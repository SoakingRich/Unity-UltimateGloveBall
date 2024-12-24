// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System;
using System.Collections;
using System.Collections.Generic;
using Oculus.Platform;
using UltimateGloveBall.App;
using UltimateGloveBall.Arena.Balls;
using UltimateGloveBall.Arena.Environment;
using UltimateGloveBall.Arena.Player;
using UltimateGloveBall.Arena.Services;
using Unity.Netcode;
using UnityEngine;
#if !(UNITY_EDITOR || UNITY_STANDALONE_WIN)
using Oculus.Platform;
#endif

namespace UltimateGloveBall.Arena.Gameplay
{
    /// <summary>
    /// Manages the state of the game. This handles the different phases of the game (Pre-game, Countdown, Game and
    /// Post-Game). It handles keeping track of what teams players are on during the pregame phase, seting up
    /// the scene according to the pahse and randomly selecting a color profile for the game.
    /// </summary>
    public class GameManager : NetworkBehaviour
    {
        private const double GAME_START_COUNTDOWN_TIME_SEC = 4;
        private const double GAME_DURATION_SEC = 180;
        public enum GamePhase
        {
            PreGame,
            CountDown,
            InGame,
            PostGame,
        }

        private struct GameStateSave           // timeRemaining is encapsulated in a struct, because maybe the game gets paused and resumed etc.?? thus we may often want to load and save a state of ' x time is remaining ' 
                                               // would make sense if there were more stats getting saved though
        {
            public double TimeRemaining;
        }

        [SerializeField] private GameState m_gameState;
        [SerializeField] private GameObject m_startGameButtonContainer;
        [SerializeField] private GameObject m_restartGameButtonContainer;
        [SerializeField] private GameObject m_inviteFriendButtonContainer;
        [SerializeField] private BallSpawner m_ballSpawner;

        [SerializeField] private CountdownView m_countdownView;

        [SerializeField] private ObstacleManager m_obstacleManager;

        [SerializeField] private GameObject m_postGameView;

        [SerializeField] private AudioSource m_courtAudioSource;
        [SerializeField] private AudioClip m_lowCountdownBeep;
        [SerializeField] private AudioClip m_highCountdownBeep;

        private readonly List<IGamePhaseListener> m_phaseListeners = new();

        private NetworkVariable<GamePhase> m_currentGamePhase = new(GamePhase.PreGame);
        private NetworkVariable<double> m_gameStartTime = new();

        private NetworkVariable<double> m_gameEndTime = new();

        private readonly Dictionary<ulong, NetworkedTeam.Team> m_playersTeamSelection = new();

        private NetworkVariable<TeamColor> m_teamAColor = new(TeamColor.Profile1TeamA);
        private NetworkVariable<TeamColor> m_teamBColor = new(TeamColor.Profile1TeamB);
        private bool m_teamColorIsSet = false;

        private GameStateSave m_gameStateSave;

        private int m_previousSecondsLeft = int.MaxValue;

        public GamePhase CurrentPhase => m_currentGamePhase.Value;
        public TeamColor TeamAColor => m_teamAColor.Value;
        public TeamColor TeamBColor => m_teamBColor.Value;

        private void OnEnable()
        {
            m_currentGamePhase.OnValueChanged += OnPhaseChanged;      // rep notify for replicated variables
            m_gameStartTime.OnValueChanged += OnStartTimeChanged;
            UGBApplication.Instance.NetworkLayer.OnHostLeftAndStartingMigration += OnHostMigrationStarted;
        }

        private void OnStartTimeChanged(double previousvalue, double newvalue)
        {
            if (m_currentGamePhase.Value == GamePhase.CountDown)
            {
                Debug.LogWarning($"OnStartTimeChanged: {newvalue}");
                m_countdownView.Show(newvalue);                     // start the pre-game countdown
            }
        }

        private void OnHostMigrationStarted()
        {
            if (m_currentGamePhase.Value == GamePhase.InGame)
            {
                m_gameStateSave.TimeRemaining = m_gameEndTime.Value - NetworkManager.Singleton.ServerTime.Time;
            }
        }

        private void OnDisable()
        {
            m_currentGamePhase.OnValueChanged -= OnPhaseChanged;
            m_gameStartTime.OnValueChanged -= OnStartTimeChanged;
            UGBApplication.Instance.NetworkLayer.OnHostLeftAndStartingMigration -= OnHostMigrationStarted;
        }

        public void RegisterPhaseListener(IGamePhaseListener listener)
        {
            m_phaseListeners.Add(listener);
            listener.OnPhaseChanged(m_currentGamePhase.Value);
            listener.OnTeamColorUpdated(TeamAColor, TeamBColor);             // on first Registering Phase Listenership, run initial funcs
        }

        public void UnregisterPhaseListener(IGamePhaseListener listener)
        {
            _ = m_phaseListeners.Remove(listener);
        }

        public void UpdatePlayerTeam(ulong clientId, NetworkedTeam.Team team)
        {
            m_playersTeamSelection[clientId] = team;             // dictionary of PlayerIDs to Team values
        }

        public NetworkedTeam.Team GetTeamWithLeastPlayers()         // return a Team, which is an enum declared in NetworkedTeam
        {
            var countA = 0;
            var countB = 0;
            foreach (var team in m_playersTeamSelection.Values)       // dictionary of PlayerIDs to Team values
            {
                if (team == NetworkedTeam.Team.TeamA)
                {
                    countA++;
                }
                else if (team == NetworkedTeam.Team.TeamB)
                {
                    countB++;
                }
            }

            return countA <= countB ? NetworkedTeam.Team.TeamA : NetworkedTeam.Team.TeamB;
        }

        public override void OnNetworkSpawn()             // i dont know why this wouldnt be spawned from the start anyway, maybe GameManager gets 'respawned' ??
            // I guess logic is put here so late joiners still get logic?? but why shouldnt that just happen OnStart()
        {
            var currentPhase = m_currentGamePhase.Value;
            if (IsServer)
            {
                if (!m_teamColorIsSet)
                {
                    TeamColorProfiles.Instance.GetRandomProfile(out var colorA, out var colorB);      // colorA and colorB vars now exist in scope of function
                    m_teamAColor.Value = colorA;
                    m_teamBColor.Value = colorB;
                    m_teamColorIsSet = true;
                }
                OnColorUpdatedClientRPC(m_teamAColor.Value, m_teamBColor.Value);         // with random colors now set, Let all relevant scripts know via client rpcs

                if (m_currentGamePhase.Value is GamePhase.PreGame)
                {
                    m_startGameButtonContainer.SetActive(true);
                }
                else if (m_currentGamePhase.Value is GamePhase.PostGame)
                {
                    m_restartGameButtonContainer.SetActive(true);
                }

                m_obstacleManager.SetTeamColor(TeamAColor, TeamBColor);

                // If we comeback from a host migration we need to handle the different states
                if (currentPhase == GamePhase.CountDown)
                {
                    StartCountdown();
                }
                else if (currentPhase == GamePhase.InGame)          // GameManager might get re-NetworkedSpawned when a host migration happens??!!    If host gets changed, all In-Scene objects get OnNetworkSpawn called again??
                {
                    HandleInGameHostMigration(m_gameStateSave.TimeRemaining);
                }
            }

            if (m_currentGamePhase.Value == GamePhase.PreGame)      // let all clients use InvitePanel
            {
                m_inviteFriendButtonContainer.SetActive(true);
            }

            OnPhaseChanged(currentPhase, currentPhase);          // call initial IGamePhase interface functions on all listeners
            NotifyPhaseListener(m_currentGamePhase.Value);
            m_teamColorIsSet = true;
        }

        private void OnPhaseChanged(GamePhase previousvalue, GamePhase newvalue)
        {
            if (newvalue == GamePhase.CountDown)
            {
                m_countdownView.Show(m_gameStartTime.Value);
            }

            if (newvalue == GamePhase.PostGame)
            {
                m_postGameView.SetActive(true);
            }
            else
            {
                m_postGameView.SetActive(false);
            }

            var playerCanMove = newvalue is GamePhase.InGame or GamePhase.PreGame;   // what the fuck is this, why not:
           // playerCanMove = newvalue == GamePhase.InGame || newvalue == GamePhase.PreGame;

            PlayerInputController.Instance.MovementEnabled = playerCanMove;        // disallow local client sliding movement?? when InGame or PreGame
            // only applies for in game players
            if (LocalPlayerEntities.Instance.Avatar != null)
            {
                m_inviteFriendButtonContainer.SetActive(newvalue == GamePhase.PreGame);
            }
            m_previousSecondsLeft = int.MaxValue;
            NotifyPhaseListener(newvalue);              // notify all listeners
        }

        public void StartGame()     // called on unityEvent bound on this object in editor from UI Button press,   only Host/Server can start game 
        {
            if (m_currentGamePhase.Value is GamePhase.PreGame or GamePhase.PostGame)          // check we're in a compatible GamePhase
            {
                m_gameState.Score.Reset();
                _ = StartCoroutine(DeactivateStartButton());

                // only check side on initial start of game
                if (m_currentGamePhase.Value is GamePhase.PreGame)
                {
                    CheckPlayersSides();
                    LockPlayersTeams();
                }

                StartCountdown();
                ((ArenaPlayerSpawningManager)SpawningManagerBase.Instance).ResetInGameSpawnPoints();
                RespawnAllPlayers();
            }
        }

        [ClientRpc]
        private void OnColorUpdatedClientRPC(TeamColor teamColorA, TeamColor teamColorB)      // called onNetworkSpawn
        {
            NotifyTeamColorListener(teamColorA, teamColorB);
            m_teamColorIsSet = true;
        }

        public void InviteFriend()
        {
            // don't open invite panel if in another phase than pregame
            if (CurrentPhase != GamePhase.PreGame)
            {
                return;
            }
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            Debug.Log("Invite Friends clicked");
#else
            GroupPresence.LaunchInvitePanel(new InviteOptions());
#endif
           // GroupPresence.LaunchInvitePanel(new InviteOptions());
        }

        private IEnumerator DeactivateStartButton()
        {
            // We need to finish processing the pointers before deactivating UI
            yield return new WaitForEndOfFrame();
            m_startGameButtonContainer.SetActive(false);
            m_restartGameButtonContainer.SetActive(false);
        }

        private void StartCountdown()
        {
            m_gameStartTime.Value = NetworkManager.Singleton.ServerTime.Time + GAME_START_COUNTDOWN_TIME_SEC;
            m_currentGamePhase.Value = GamePhase.CountDown;
            m_countdownView.Show(m_gameStartTime.Value, SwitchToInGame);    // SwitchToInGame action is called on complete 
        }

        public void SwitchToInGame()          // go to InGame after countdown is over
        {
            m_currentGamePhase.Value = GamePhase.InGame;
            m_gameEndTime.Value = NetworkManager.Singleton.ServerTime.Time + GAME_DURATION_SEC;       // set an explicit Server time to be m_gameEndTime, and replicate it
            m_ballSpawner.SpawnInitialBalls();
        }

        private void GoToPostGame()
        {
            m_ballSpawner.DeSpawnAllBalls();
            m_currentGamePhase.Value = GamePhase.PostGame;
            m_restartGameButtonContainer.SetActive(true);
            ((ArenaPlayerSpawningManager)SpawningManagerBase.Instance).ResetPostGameSpawnPoints();
            RespawnAllPlayers();
        }

        private void Update()
        {
            if (m_currentGamePhase.Value == GamePhase.InGame)
            {
                var timeLeft = m_gameEndTime.Value - NetworkManager.Singleton.ServerTime.Time;     // all clients can access the server time and replicated var m_gameEndTime
                UpdateTimeInPhaseListener(Math.Max(0, timeLeft));

                if (timeLeft < 11)
                {
                    var seconds = Math.Max(0, (int)Math.Floor(timeLeft));
                    if (m_previousSecondsLeft != seconds)
                    {
                        TriggerEndGameCountdownBeep(seconds);      // handle countdown beep
                    }

                    m_previousSecondsLeft = seconds;

                    if (IsServer)
                    {
                        if (timeLeft < 0)
                        {
                            GoToPostGame();
                        }
                    }
                }
            }
            else if (m_currentGamePhase.Value == GamePhase.PreGame)
            {
                if (NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsServer)
                {
                    CheckPlayersSides();
                }
            }
        }

        private void TriggerEndGameCountdownBeep(int seconds)
        {
            if (seconds == 0)
            {
                m_courtAudioSource.PlayOneShot(m_highCountdownBeep);
            }
            else
            {
                m_courtAudioSource.PlayOneShot(m_lowCountdownBeep);
            }
        }

        private void OnGUI()
        {
            if (IsServer)
            {
                if (m_currentGamePhase.Value is GamePhase.PreGame or GamePhase.PostGame)
                {
                    if (GUILayout.Button("StartGame"))
                    {
                        StartGame();
                    }
                }
            }
        }

        private void NotifyPhaseListener(GamePhase newphase)
        {
            foreach (var listener in m_phaseListeners)
            {
                listener.OnPhaseChanged(newphase);
            }
        }
        private void UpdateTimeInPhaseListener(double timeLeft)
        {
            foreach (var listener in m_phaseListeners)
            {
                listener.OnPhaseTimeUpdate(timeLeft);
            }
        }

        private void NotifyTeamColorListener(TeamColor teamColorA, TeamColor teamColorB)          // actually sending TeamColor changes to GamePhase listeners but go off.
        {
            foreach (var listener in m_phaseListeners)
            {
                listener.OnTeamColorUpdated(teamColorA, teamColorB);
            }
        }

        private void LockPlayersTeams()     // not really any locking going on
        {
            foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if (m_playersTeamSelection.TryGetValue(clientId, out var team))
                {
                    var avatar = LocalPlayerEntities.Instance.GetPlayerObjects(clientId).Avatar;
                    if (avatar != null)
                    {
                        avatar.GetComponent<NetworkedTeam>().MyTeam = team;

                        var playerData = ArenaSessionManager.Instance.GetPlayerData(clientId).Value;     // update value of  playerData.SelectedTeam member in ArenaSessionManager singleton,  Server only ??
                        playerData.SelectedTeam = team;
                        ArenaSessionManager.Instance.SetPlayerData(clientId, playerData);
                    }
                }
            }
        }

        private void CheckPlayersSides()
        {
            var clientCount = NetworkManager.Singleton.ConnectedClientsIds.Count;
            if (m_playersTeamSelection.Count != clientCount)
            {
                m_playersTeamSelection.Clear();
            }

            for (var i = 0; i < clientCount; ++i)
            {
                var clientId = NetworkManager.Singleton.ConnectedClientsIds[i];
                var playerObjects = LocalPlayerEntities.Instance.GetPlayerObjects(clientId);
                var avatar = playerObjects.Avatar;
                if (avatar != null)
                {
                    var side = avatar.transform.position.z < 0                // assign sides based on Position,   are Player Teams flexible depending on what side Players have moved to??
                        ? NetworkedTeam.Team.TeamA
                        : NetworkedTeam.Team.TeamB;

                    var color = side == NetworkedTeam.Team.TeamA ? TeamAColor : TeamBColor;

                    foreach (var colorComp in playerObjects.ColoringComponents)   // class PlayerGameObjects keeps a list of ColoringComponent scripts to store team color
                    {
                        colorComp.TeamColor = color;
                    }

                    m_playersTeamSelection[clientId] = side;
                }
            }
        }

        private void RespawnAllPlayers()   // only server surely
        {
            foreach (var clientId in LocalPlayerEntities.Instance.PlayerIds)
            {
                var allPlayerObjects = LocalPlayerEntities.Instance.GetPlayerObjects(clientId);   // allPlayerObjects is a script of class PlayerGameObjects
                if (allPlayerObjects.Avatar)
                {
                    SpawningManagerBase.Instance.GetRespawnPoint(             // Get a respawn point for a particular PlayerAvatarEntity, considering its Team 
                        clientId,
                        allPlayerObjects.Avatar.GetComponent<NetworkedTeam>().MyTeam, out var position,
                        out var rotation);
                    // only send to specific client
                    var clientRpcParams = new ClientRpcParams          // create ClientRPC Params so we can specify Targets for rpc as we only want to send Respawn RPC to one client only
                    {
                        Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } }
                    };
                    OnRespawnClientRpc(position, rotation, m_currentGamePhase.Value, clientRpcParams);
                }
            }
        }

        private void HandleInGameHostMigration(double timeRemaining)   // game remains playing when hostmigration happens when gamePhase is still going
        {
            m_currentGamePhase.Value = GamePhase.InGame;
            m_gameEndTime.Value = NetworkManager.Singleton.ServerTime.Time + timeRemaining;
            m_ballSpawner.SpawnInitialBalls();          // for some reason we have to reset all balls though,  makes sense, since the owning client of many of them has just left??
        }

        [ClientRpc]
        private void OnRespawnClientRpc(Vector3 position, Quaternion rotation, GamePhase phase, ClientRpcParams rpcParams)        // respawn a single client  ( teleport them somewhere )
        {
            if (phase is GamePhase.PostGame or GamePhase.CountDown)
            {
                PlayerInputController.Instance.MovementEnabled = false;
            }
            PlayerMovement.Instance.TeleportTo(position, rotation);
            LocalPlayerEntities.Instance.LeftGloveHand.ResetGlove();
            LocalPlayerEntities.Instance.RightGloveHand.ResetGlove();
        }
    }
}