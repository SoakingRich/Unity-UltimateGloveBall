// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System.Collections;
using Meta.Multiplayer.Core;
using UltimateGloveBall.Arena.Services;
using UltimateGloveBall.MainMenu;
using UnityEngine;

namespace UltimateGloveBall.App
{
    /// <summary>
    /// Handles the Navigation through the application.
    /// Expose API to navigate between game state: MainMenu, Play, Spectate. Or specifically load scenes.
    /// Navigating through the different scenes based on the application state.
    /// </summary>
    public class NavigationController
    {
        private readonly SceneLoader m_sceneLoader = new();                                 // SceneLoader is a Meta-made class, not clear why its needed

        private MonoBehaviour m_coroutineRunner;
        private NetworkLayer m_networkLayer;
        private PlayerPresenceHandler m_playerPresenceHandler;
        private LocalPlayerState m_localPlayerState;

        
        
        
        
        
        
        public NavigationController(MonoBehaviour coroutineRunner, NetworkLayer networkLayer, LocalPlayerState localPlayerState, PlayerPresenceHandler playerPresenceHandler)   // constructor
        {
            m_coroutineRunner = coroutineRunner;
            m_networkLayer = networkLayer;
            m_localPlayerState = localPlayerState;
            m_playerPresenceHandler = playerPresenceHandler;
        }

        private Coroutine StartCoroutine(IEnumerator routine)
        {
            return m_coroutineRunner.StartCoroutine(routine);
        }
        
        public void GoToMainMenu(ArenaApprovalController.ConnectionStatus connectionStatus = ArenaApprovalController.ConnectionStatus.Success)        // Disconnect and Go to MainMenu // connectionStatus is defaulted to 'Success', because the menu is always available to go to, it cant be 'Full' or dissallowed by ArenaApprovalController
        {
            m_networkLayer.Leave();

            IEnumerator GoToLobbyAfterLeaving()                // define a IEnumerator and start it within the same function
            {
                if (OVRScreenFade.instance.currentAlpha < 1)
                {
                  //  OVRScreenFade.instance.FadeOut();                          fucks up passthrough ??
                }
                yield return new WaitUntil(() => m_networkLayer.CurrentClientState == NetworkLayer.ClientState.Disconnected /*&& OVRScreenFade.instance.currentAlpha >= 1*/);       // wait for disconnect and black fade
                m_networkLayer.GoToLobby();
                yield return new WaitUntil(() => m_networkLayer.CurrentClientState == NetworkLayer.ClientState.ConnectedToLobby);           // wait for connect to Lobby
                yield return new WaitUntil(() => m_sceneLoader.SceneLoaded);                                                          // wait til scene loaded
                if (OVRScreenFade.instance.currentAlpha >= 1)
                {
                   // OVRScreenFade.instance.FadeIn();
                }
                yield return GenerateNewGroupPresence("MainMenu");                                // Wait for a group presence to be generated
                var menuController = Object.FindObjectOfType<MainMenuController>();
                if (menuController)
                {
                    menuController.OnReturnToMenu(connectionStatus);                      // find the MenuController in the newly loaded level and call func on it
                }
            }

            _ = StartCoroutine(GoToLobbyAfterLeaving());
        }

       
        public void NavigateToMatch(bool isHosting)                                                             // OnQuickMatchClicked() or   OnHostMatchClicked()  ??
        {
            _ = StartCoroutine(SwitchRoomOnPhotonReady(m_playerPresenceHandler.GetArenaDestinationAPI(m_networkLayer.GetRegion()),                      // ie. Arena-Aus-20469, could be hosting, is definitely not spectating
                m_playerPresenceHandler.GroupPresenceState.LobbySessionID, isHosting));                                                             // switch room after screen fade/ group presence generate / photon exec
        }
        
        
        private IEnumerator SwitchRoomOnPhotonReady(string roomName, string lobbySessionId, bool isHosting, bool isSpectator = false)   // switch room after screen fade/ group presence generate / photon exec
        {
            //Wait until the current frame has ended to allow everything to initialize
            yield return new WaitForEndOfFrame();

            _ = StartCoroutine(SwitchRoom(roomName, lobbySessionId, isHosting, isSpectator));           // ie. switch to Arena-Aus-20469, could be hosting, could be spectating
        }
        
        
        private IEnumerator SwitchRoom(string destination, string lobbySessionID, bool isHosting, bool isSpectator)  
        {
            Debug.Log($"Switching room to {destination} as {(isHosting ? "host" : "client")}{(isSpectator ? " spectator" : "")}");

            //   OVRScreenFade.instance.FadeOut();     // this seems to fuck everything with passthrough ??

            yield return StartCoroutine(GenerateNewGroupPresence(destination, lobbySessionID));          // wait for a new group presense to be started
            //   yield return StartCoroutine(new WaitUntil(() => OVRScreenFade.instance.currentAlpha >= 1));       // wait for current alpha to be 1.0 on screen fade
            Debug.LogWarning($"Switching to room {lobbySessionID} in {destination}");
            m_localPlayerState.IsSpectator = isSpectator;
            m_networkLayer.SwitchPhotonRealtimeRoom(lobbySessionID, isHosting, m_playerPresenceHandler.GetRegionFromDestination(destination));             // networkLayer function to actually handle the photon switch
        }



        public void JoinMatch(string destinationAPI, string sessionId)
        {
            _ = StartCoroutine(SwitchRoomOnPhotonReady(destinationAPI, sessionId, false));
        }

        
        public void SwitchRoomFromInvite(string destination, string lobbySessionID, bool isHosting, bool isSpectator)
        {
            _ = StartCoroutine(SwitchRoom(destination, lobbySessionID, isHosting, isSpectator));
        }

        
        
        public void WatchRandomMatch()
        {
            _ = StartCoroutine(SwitchRoomOnPhotonReady(m_playerPresenceHandler.GetArenaDestinationAPI(m_networkLayer.GetRegion()), "", false, true));
        }

        public void WatchMatch(string destinationAPI, string sessionId)
        {
            _ = StartCoroutine(SwitchRoomOnPhotonReady(destinationAPI, sessionId, false, true));
        }

        private IEnumerator GenerateNewGroupPresence(string destination, string lobbySessionId = null)
        {
            return m_playerPresenceHandler.GenerateNewGroupPresence(destination, lobbySessionId);
        }

        public bool IsSceneLoaded()
        {
            return m_sceneLoader.SceneLoaded;
        }
        
        
        

        public void LoadMainMenu()
        {
            m_sceneLoader.LoadScene("MainMenu", false);
        }

        public void LoadArena()
        {
            m_sceneLoader.LoadScene("Arena");
        }
    }
}