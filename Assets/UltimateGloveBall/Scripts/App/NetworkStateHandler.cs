// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System;
using System.Collections;
using Meta.Multiplayer.Core;
using UltimateGloveBall.Arena.Services;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UltimateGloveBall.App
{
    /// <summary>
    /// Handles the callbacks from the NetworkLayer.
    /// Setup the state of the application based on the connection state of the network layer.
    /// Handles Host connection, Client Connection and Lobby Connection. 
    /// </summary>
    public class NetworkStateHandler
    {
        private MonoBehaviour m_coroutineRunner;
        private NetworkLayer m_networkLayer;
        private NavigationController m_navigationController;
        private VoipController m_voip;
        private LocalPlayerState m_localPlayerState;
        private PlayerPresenceHandler m_playerPresenceHandler;
        private Func<NetworkSession> m_createSessionFunc;
        private NetworkSession m_session;

        private bool IsSpectator => m_localPlayerState.IsSpectator;

        public NetworkStateHandler(
            MonoBehaviour coroutineRunner,
            NetworkLayer networkLayer,
            NavigationController navigationController,
            VoipController voip,
            LocalPlayerState localPlayerState,
            PlayerPresenceHandler playerPresenceHandler,
            Func<NetworkSession> createSessionFunc)
        {
            m_coroutineRunner = coroutineRunner;
            m_networkLayer = networkLayer;
            m_navigationController = navigationController;
            m_voip = voip;
            m_localPlayerState = localPlayerState;
            m_playerPresenceHandler = playerPresenceHandler;
            m_createSessionFunc = createSessionFunc;

            m_networkLayer.OnClientConnectedCallback += OnClientConnected;      // netcode class NetworkLayer has callbacks/events to bind to
            m_networkLayer.OnClientDisconnectedCallback += OnClientDisconnected;
            m_networkLayer.OnMasterClientSwitchedCallback += OnMasterClientSwitched;
            m_networkLayer.StartLobbyCallback += OnLobbyStarted;
            m_networkLayer.StartHostCallback += OnHostStarted;
            m_networkLayer.StartClientCallback += OnClientStarted;
            m_networkLayer.RestoreHostCallback += OnHostRestored;
            m_networkLayer.RestoreClientCallback += OnClientRestored;
            m_networkLayer.OnRestoreFailedCallback += OnRestoreFailed;

            // Functions
            m_networkLayer.GetOnClientConnectingPayloadFunc = GetClientConnectingPayload;    // netcode class NetworkLayer has a 'interface' function we need to implement
            m_networkLayer.CanMigrateAsHostFunc = CanMigrateAsHost;
        }

     

        private Coroutine StartCoroutine(IEnumerator routine)         // use this function to startcoroutines because m_coroutineRunner is a MonoBehavior
        {
            return m_coroutineRunner.StartCoroutine(routine);        
        }
        
        
        

        private void StartVoip(Transform transform)                 // start in menu
        {
            m_voip.StartVoip(transform);
        }

        
        
        private void OnLobbyStarted()
        {
            Debug.Log("OnLobbyStarted");

            m_navigationController.LoadMainMenu();          // when player has made initial connection to photon,  Load the main menu
        }
        
        
        
        
        private void OnHostStarted()            // Started means Requesting connection
        {
            Debug.Log("OnHostStarted");      // host player is ready to enter Arena

            m_navigationController.LoadArena();              // go to arena

            _ = StartCoroutine(Impl());

            IEnumerator Impl()
            {
                yield return new WaitUntil(() => m_navigationController.IsSceneLoaded());

                SpawnSession();                        // spawn a session,    the session exists and replicates vars to all,  but only server should spawn it

                var player = SpawningManagerBase.Instance.SpawnPlayer(NetworkManager.Singleton.LocalClientId,           //spawn playerEntity for host
                    m_localPlayerState.PlayerUid, false, Vector3.zero);

                StartVoip(player.transform);
            }
        }
        
        
        
        private void SpawnSession()
        {
            m_session = m_createSessionFunc.Invoke();                         // networkStateHandler has this func defined in its constructor. It will return a NetworkSession.    its in UGBApplication  - InstantiateSession
                                                                                                    // Append Region to lobbyId to ensure unique voice room, since we use only 1 region for voice
            var lobbyId = m_playerPresenceHandler.GroupPresenceState.LobbySessionID;
            m_session.SetPhotonVoiceRoom($"{m_networkLayer.GetRegion()}-{lobbyId}");
            m_session.GetComponent<NetworkObject>().Spawn();
        }

        
        
        
        #region Network Layer Callbacks
        
        
      
        
        
        
        private void OnClientConnected(ulong clientId)                 // this will happen for all clients (including server/master) 
        {
            _ = StartCoroutine(Impl());
            var destinationAPI = m_playerPresenceHandler.GetArenaDestinationAPI(m_networkLayer.GetRegion());   // get the correct DestinationAPI by querying the region were connected to
            _ = StartCoroutine(
                m_playerPresenceHandler.GenerateNewGroupPresence(destinationAPI, m_networkLayer.CurrentRoom));     // generate a new group presence in this new place

            IEnumerator Impl()
            {
                if (NetworkManager.Singleton.IsHost)                                                          // host doesnt spawn playerEntity,  already did in OnHostStarted
                {
                    yield return new WaitUntil(() => m_session != null);                                 // wait til session is valid, that was spawned in OnHostStarted
                    m_session.DetermineFallbackHost(clientId);                                       // Sessions can have FallbackHosts - from a clientID        
                    m_session.UpdatePhotonVoiceRoomToClient(clientId);
                }
                else if (NetworkManager.Singleton.IsClient)
                {
                    m_session = Object.FindObjectOfType<NetworkSession>();         // clients retrieve the NetworkSession in the world

                    var playerPos = m_networkLayer.CurrentClientState == NetworkLayer.ClientState.RestoringClient       // PlayerPos is either LocalPlayerState position ( if client is repairing connecting)
                        ? m_localPlayerState.transform.position                // or else we want to request a spawn from a SpawnManager
                        : Vector3.zero;
                    SpawningManagerBase.Instance.RequestSpawnServerRpc(
                        clientId, m_localPlayerState.PlayerUid, IsSpectator, playerPos);
                }
            }
        }
        
        
          
        private void OnClientStarted()
        {
            var player = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();      // i guess localplayerobject becomes PlayerAvatarEntity
            StartVoip(player.transform);                                        // start voip really early
        }
        
        

        private void OnClientDisconnected(ulong clientId)
        {
            if (m_session)
            {
                m_session.RedetermineFallbackHost(clientId);           // check if our current FallbackHost left, we may need a new one
            }
        }

        private static ulong OnMasterClientSwitched()             // return the new master client id,   a new fallback id will soon be determined
        {
            return NetworkSession.FallbackHostId;
        }

       

     

        

        private void OnHostRestored()                  // i guess players can lose connections fairly easily? and reconnect
        {
            SpawnSession();                              // instantiate a new session ???

            var player = SpawningManagerBase.Instance.SpawnPlayer(NetworkManager.Singleton.LocalClientId,
                m_localPlayerState.PlayerUid, false, m_localPlayerState.transform.position);                   // spawn playerEntity for host, i guess the other may have been removed

            StartVoip(player.transform);
        }

        private void OnClientRestored()
        {
            var player = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();               // i guess Client entities are not disposed of the same way??
            StartVoip(player.transform);
        }

        private void OnRestoreFailed(int failureCode)
        {
            m_navigationController.GoToMainMenu((ArenaApprovalController.ConnectionStatus)failureCode);
        }

        
        private bool CanMigrateAsHost()
        {
            return !IsSpectator;
        }
        
        
        
        
        private string GetClientConnectingPayload()                         // serializing to json whether or not connecting client is a spectator or not, and informing ArenaApprovalController
        {
            return JsonUtility.ToJson(new ArenaApprovalController.ConnectionPayload()             
            {
                IsPlayer = !IsSpectator,
            });
        }

        
        #endregion // Network Layer Callbacks
        
        
        
        
        public void Dispose()                                  // dispose of NetworkStateHandler 
        {
            m_networkLayer.OnClientConnectedCallback -= OnClientConnected;
            m_networkLayer.OnClientDisconnectedCallback -= OnClientDisconnected;
            m_networkLayer.OnMasterClientSwitchedCallback -= OnMasterClientSwitched;
            m_networkLayer.StartLobbyCallback -= OnLobbyStarted;
            m_networkLayer.StartHostCallback -= OnHostStarted;
            m_networkLayer.StartClientCallback -= OnClientStarted;
            m_networkLayer.RestoreHostCallback -= OnHostRestored;
            m_networkLayer.RestoreClientCallback -= OnClientRestored;
            m_networkLayer.OnRestoreFailedCallback -= OnRestoreFailed;
        }
    }
}