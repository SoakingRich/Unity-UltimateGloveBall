// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System.Collections;
using System.ComponentModel;
using System.Threading.Tasks;
using Meta.Multiplayer.Core;
using Meta.Utilities;
using Oculus.Platform;
using UnityEngine;

namespace UltimateGloveBall.App
{
    /// <summary>
    /// This is the Entry Point of the whole Ultimate Glove Ball application.
    /// Initializes the core of the application on Start, loads controllers and handlers.
    /// This singleton also exposes controllers and handlers to be used through the application.
    /// Initializes the Oculus Platform, fetch the player state on login and handles join intent.
    /// </summary>
    public class UGBApplication : Singleton<UGBApplication>
    {
        public NetworkLayer NetworkLayer;
        public VoipController Voip;
        [SerializeField] private NetworkSession m_sessionPrefab;

        private LaunchType m_launchType;

        private LocalPlayerState LocalPlayerState => LocalPlayerState.Instance;

        public NavigationController NavigationController { get; private set; }
        public PlayerPresenceHandler PlayerPresenceHandler { get; private set; }
        public NetworkStateHandler NetworkStateHandler { get; private set; }

        protected override void InternalAwake()
        {
            DontDestroyOnLoad(this);
        }

        private void OnDestroy()
        {
            NetworkStateHandler?.Dispose();
        }

        private void Start()
        {
            if (UnityEngine.Application.isEditor)   // editor only
            {
                if (NetworkSettings.Autostart)
                {
                    LocalPlayerState.SetApplicationID(             
                        // this is the only call to SetApplicationID anywhere
                        // we set ApplicationID to either Device UID or a RoomName denoted in network settings
                            // this lets us join a room someone else is hosting in the editor if needed
                        NetworkSettings.UseDeviceRoom ? SystemInfo.deviceUniqueIdentifier : NetworkSettings.RoomName);
                }
            }

            _ = StartCoroutine(Init());
        }

        private IEnumerator Init()
        {
            _ = InitializeOculusModules();        // this is an Async Task

            // Initialize Player Presence
            Debug.Log("constructing PlayerPresenceHandler");
            PlayerPresenceHandler = new PlayerPresenceHandler();
            yield return PlayerPresenceHandler.Init();

#if !UNITY_EDITOR && !UNITY_STANDALONE_WIN         // LocalPlayerState is Init is InitializeOculusModules
             Debug.Log("waiting for LocalPlayerState to not be null");    
            yield return new WaitUntil(() => !string.IsNullOrWhiteSpace(LocalPlayerState.Username));
#else
            m_launchType = LaunchType.Normal;      // seems like Quest is always first given LaunchType Normal ?? cant see anywhere this is set to anything different 
#endif
            _ = BlockUserManager.Instance.Initialize();
            NavigationController =
                new NavigationController(this, NetworkLayer, LocalPlayerState, PlayerPresenceHandler);
            NetworkStateHandler = new NetworkStateHandler(this, NetworkLayer, NavigationController, Voip,
                LocalPlayerState, PlayerPresenceHandler, InstantiateSession);
            // Get the products and the purchases of the current logged in user
            // get all icons products
            IAPManager.Instance.FetchProducts(UserIconManager.Instance.AllSkus, ProductCategories.ICONS);     // fetchProducts has two signatures, one to retrieve all SKUs and one to specificy only select category types
            // get cat consumable
            IAPManager.Instance.FetchProducts(new[] { ProductCategories.CAT }, ProductCategories.CONSUMABLES);
            IAPManager.Instance.FetchPurchases();

            if (m_launchType == LaunchType.Normal)            // launch type is a enum defined by Oculus Platform
            {
                if (LocalPlayerState.HasCustomAppId)        // app id is really just GameInstanceID rather than actual appID
                {
                    StartCoroutine(PlayerPresenceHandler.GenerateNewGroupPresence(               // if HasCustomAppId is true   (only when using editor???), our group presense is created as Arena-<applicationID>  where applicationID has been set custom
                        "Arena",
                        $"{LocalPlayerState.ApplicationID}"));
                }
                else
                {
                    StartCoroutine(
                        PlayerPresenceHandler.GenerateNewGroupPresence(
                            "MainMenu")
                    );
                }
            }

          //  wating for GroupPresence state to be null or something ??
            yield return new WaitUntil(() => PlayerPresenceHandler.GroupPresenceState is { Destination: { } });

            NetworkLayer.Init(
                PlayerPresenceHandler.GroupPresenceState.LobbySessionID,
                PlayerPresenceHandler.GetRegionFromDestination(PlayerPresenceHandler.GroupPresenceState.Destination));
        }

        private async Task InitializeOculusModules()    // this is an async task that can happen on other threads
        {
            try
            {
                var coreInit = await Core.AsyncInitialize().Gen();    // oculus provides an async initialization method for Oculus platform  ( log in as oculus user / entitlement etc. )
                if (coreInit.IsError)
                {
                    LogError("Failed to initialize Oculus Platform SDK", coreInit.GetError());
                    return;
                }

                Debug.Log("Oculus Platform SDK initialized successfully");

                var isUserEntitled = await Entitlements.IsUserEntitledToApplication().Gen();
                if (isUserEntitled.IsError)
                {
                    LogError("You are not entitled to use this app", isUserEntitled.GetError());
                    return;
                }

                m_launchType = ApplicationLifecycle.GetLaunchDetails().LaunchType;   // either    Unknown, Normal, Invite, Coordinated or Deeplink

                GroupPresence.SetJoinIntentReceivedNotificationCallback(OnJoinIntentReceived);     // this is an event you will get when OculusPlatform tells your app, the user wants to Join a group presence??
                GroupPresence.SetInvitationsSentNotificationCallback(OnInvitationsSent);   // lets you bind a method that will happen when you send an invitation

                var getLoggedInuser = await Users.GetLoggedInUser().Gen();
                if (getLoggedInuser.IsError)
                {
                    LogError("Cannot get user info", getLoggedInuser.GetError());
                    return;
                }

                // Workaround.
                // At the moment, Platform.Users.GetLoggedInUser() seems to only be returning the user ID.
                // Display name is blank.
                // Platform.Users.Get(ulong userID) returns the display name.
                var getUser = await Users.Get(getLoggedInuser.Data.ID).Gen();
                LocalPlayerState.Init(getUser.Data.DisplayName, getUser.Data.ID);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void OnJoinIntentReceived(Message<Oculus.Platform.Models.GroupPresenceJoinIntent> message)
        {
            // OnJoinIntentReceived is called when Player has selected to join a match via a Destination deeplink (thus we recieve a destination and all its data)
            // Client will recieve this shortly after launching the app and logging in to Oculus Platform
            // they would also recieve it, if theyve already been playing for a while, but went to the oculus menu and chose to join someone
            
            Debug.Log("------JOIN INTENT RECEIVED------");
            Debug.Log("Destination:       " + message.Data.DestinationApiName);
            Debug.Log("Lobby Session ID:  " + message.Data.LobbySessionId);
            Debug.Log("Match Session ID:  " + message.Data.MatchSessionId);
            Debug.Log("Deep Link Message: " + message.Data.DeeplinkMessage);
            Debug.Log("--------------------------------");

            var messageLobbySessionId = message.Data.LobbySessionId;

            // no Group Presence yet:
            // app is being launched by this join intent, either
            // through an in-app direct invite, or through a deeplink
            if (PlayerPresenceHandler.GroupPresenceState == null)
            {
                var lobbySessionID = message.Data.DestinationApiName.StartsWith("Arena") && !string.IsNullOrEmpty(messageLobbySessionId)   // lobbySessionID set to either    Arena + applicationID
                    ? messageLobbySessionId
                    : "Arena-" + LocalPlayerState.ApplicationID;

                _ = StartCoroutine(PlayerPresenceHandler.GenerateNewGroupPresence(
                    message.Data.DestinationApiName,
                    lobbySessionID));
            }
            // game was already running, meaning the user already has a Group Presence, and
            // is already either hosting or a client of another host.
            else
            {
                NavigationController.SwitchRoomFromInvite(
                    message.Data.DestinationApiName, messageLobbySessionId, false, false);
            }
        }

        private void OnInvitationsSent(Message<Oculus.Platform.Models.LaunchInvitePanelFlowResult> message)
        {
            Debug.Log("-------INVITED USERS LIST-------");
            Debug.Log("Size: " + message.Data.InvitedUsers.Count);
            foreach (var user in message.Data.InvitedUsers)
            {
                Debug.Log("Username: " + user.DisplayName);
                Debug.Log("User ID:  " + user.ID);
            }

            Debug.Log("--------------------------------");
        }

        private void LogError(string message, Oculus.Platform.Models.Error error)
        {
            Debug.LogError(message);
            Debug.LogError("ERROR MESSAGE:   " + error.Message);
            Debug.LogError("ERROR CODE:      " + error.Code);
            Debug.LogError("ERROR HTTP CODE: " + error.HttpCode);
        }

        private NetworkSession InstantiateSession()
        {
            return Instantiate(m_sessionPrefab);
        }
    }
}