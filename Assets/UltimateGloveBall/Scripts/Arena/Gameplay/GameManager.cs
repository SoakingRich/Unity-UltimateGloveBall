// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Meta.Multiplayer.Core;
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
        
        public static GameManager _instance { get; private set; }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject); // Prevent duplicate singletons
                return;
            }

            _instance = this;
         //   DontDestroyOnLoad(gameObject); // Persist across scenes
        }
    
        
        
        
        private const double GAME_START_COUNTDOWN_TIME_SEC = 4;
        private const double GAME_DURATION_SEC = 180;
        public enum GamePhase
        {
            PreGame,
            CountDown,
            InGame,
            PostGame,
        }

        private struct GameStateSave           // TimeRemaining is a struct for some reason??    when theres a host migration, this is used to keep the game going
                                              
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

        private readonly List<IGamePhaseListener> m_phaseListeners = new();                  // an array of generic objects which are all IGamePhaseListeners

        private NetworkVariable<GamePhase> m_currentGamePhase = new(GamePhase.PreGame);
        private NetworkVariable<double> m_gameStartTime = new();

        private NetworkVariable<double> m_gameEndTime = new();

        private readonly Dictionary<ulong, NetworkedTeam.Team> m_playersTeamSelection = new();             // dictionary of teams

        private NetworkVariable<TeamColor> m_teamAColor = new(TeamColor.Profile1TeamA);
        private NetworkVariable<TeamColor> m_teamBColor = new(TeamColor.Profile1TeamB);
        private bool m_teamColorIsSet = false;

        private GameStateSave m_gameStateSave;

        private int m_previousSecondsLeft = int.MaxValue;
        

        public GamePhase CurrentPhase => m_currentGamePhase.Value;                       //   instead of fetching m_currentGamePhase.Value constantly, we can just use this CurrentPhase accessor
        public TeamColor TeamAColor => m_teamAColor.Value;
        public TeamColor TeamBColor => m_teamBColor.Value;

        
        
        [Header("BLOCKAMI")]
        public bool AutoStartGameManager = true;
        public AIPlayer[] m_allAIPlayers;
        public Action OnEmojiCubeHitFloor;

        
        
        
        
        
        
        
        
        

        private void OnEnable()
        {
            m_allAIPlayers  = FindObjectsOfType<AIPlayer>();
            
            
            m_currentGamePhase.OnValueChanged += OnPhaseChanged;      // rep notify for replicated variables
            m_gameStartTime.OnValueChanged += OnStartTimeChanged;
            
         
            var HCTransforms = FindObjectsOfType<HealthCubeTransform>();
            foreach (var hct in HCTransforms)
            {
                hct.OnHealthCubeDied += OnHealthCubeDied;
            }



            UGBApplication.Instance.NetworkLayer.OnHostLeftAndStartingMigration += OnHostMigrationStarted;

            
        }

   


        private void OnDisable()
        {
            m_currentGamePhase.OnValueChanged -= OnPhaseChanged;
            m_gameStartTime.OnValueChanged -= OnStartTimeChanged;
  
            UGBApplication.Instance.NetworkLayer.OnHostLeftAndStartingMigration -= OnHostMigrationStarted;
       //    UGBApplication.Instance.NetworkLayer.StartHostCallback -= StartGameGameManager;
        }


        void Start()
        {
#if UNITY_EDITOR
            if (AutoStartGameManager)
            {
               Invoke("StartGame", 2.0f); 
            }
#endif
        }

       

        private void OnStartTimeChanged(double previousvalue, double newvalue)                 // when start button gets pressed
        {
            if (m_currentGamePhase.Value == GamePhase.CountDown)
            {
                Debug.LogWarning($"OnStartTimeChanged: {newvalue}");
                m_countdownView.Show(newvalue);                     // start the pre-game countdown
            }
        }
        
        
        
        private void OnPhaseChanged(GamePhase previousvalue, GamePhase newvalue)                // when GamePhase.value has been set to a new value
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

            var playerCanMove = newvalue is GamePhase.InGame or GamePhase.PreGame;   // what the fuck is this, why not:       // playerCanMove = newvalue == GamePhase.InGame || newvalue == GamePhase.PreGame;

            PlayerInputController.Instance.MovementEnabled = playerCanMove;                       // allow clients sliding movement if InGame or PreGame,  otherwise no
            // only applies for in game players
            if (LocalPlayerEntities.Instance?.Avatar != null)
            {
                m_inviteFriendButtonContainer.SetActive(newvalue == GamePhase.PreGame);             // if PreGame,  activate InviteFriends that will open roster menu
            }
            m_previousSecondsLeft = int.MaxValue;
            NotifyPhaseListener(newvalue);                                              // notify all phase change listeners such as... MusicManager, Scoreboard
        }
        
        
        
        public override void OnNetworkSpawn()             // set current state for GameManager, including for late joiners   // i think this will run Everytime a client triggers OnNetworkSpawn to happen ??
            
        {
            _ = StartCoroutine(Impl());

            IEnumerator Impl()
            {
                yield return new WaitUntil(() => LocalPlayerEntities.Instance?.GetPlayerObjects(NetworkManager.Singleton.LocalClientId)?.Avatar != null);     // wait until LocalPlayerEntities has been initialized
            }

            var currentPhase = m_currentGamePhase.Value;
            
            
            if (IsServer)         // everything here is server only
            {
                if (!m_teamColorIsSet)
                {
                    TeamColorProfiles.Instance.GetRandomProfile(out var colorA, out var colorB);      // colorA and colorB vars now exist in scope of function despite not being declared
                    m_teamAColor.Value = colorA;
                    m_teamBColor.Value = colorB;
                    m_teamColorIsSet = true;                        // set the TeamColors that TeamColoringComponents will use/update themselves with
                }
                
                
                OnColorUpdatedClientRPC(m_teamAColor.Value, m_teamBColor.Value);         // send rpcs color updates to all objects

                if (m_currentGamePhase.Value is GamePhase.PreGame)
                {
                    m_startGameButtonContainer.SetActive(true);
                    
                    var playerloc = LocalPlayerEntities.Instance?.GetPlayerObjects(NetworkManager.Singleton.LocalClientId)?.Avatar?.transform;

                    if (playerloc == null)
                    {
                        Debug.LogError("error - Null detected in the chain: " +
                                       $"{(LocalPlayerEntities.Instance == null ? "LocalPlayerEntities.Instance is null" : "")} " +
                                       $"{(LocalPlayerEntities.Instance?.GetPlayerObjects(NetworkManager.Singleton.LocalClientId) == null ? "GetPlayerObjects returned null" : "")} " +
                                       $"{(LocalPlayerEntities.Instance?.GetPlayerObjects(NetworkManager.Singleton.LocalClientId)?.Avatar == null ? "Avatar is null" : "")}");
                        return;
                    }
                    
                    playerloc.position += playerloc.rotation * Vector3.forward * 2;
                    m_startGameButtonContainer.transform.position = playerloc.position;
                   // m_startGameButtonContainer.transform.rotation = playerloc.rotation.;     // rotation is handled by FacePlayerOnZUI.cs
                }
                else if (m_currentGamePhase.Value is GamePhase.PostGame)
                {
                    m_restartGameButtonContainer.SetActive(true);            // restart button is present in PostGame podium
                }

                m_obstacleManager.SetTeamColor(TeamAColor, TeamBColor);

                                                                            // If we come back from a host migration we need to handle the different states
                if (currentPhase == GamePhase.CountDown)
                {
                    StartCountdown();                          // if we joined during countdown, just start the countdown (late?)
                }
                else if (currentPhase == GamePhase.InGame)          // GameManager might get NetworkedSpawned during InGame phase,  if we are the server, there must have been a migration
                {
                    HandleInGameHostMigration(m_gameStateSave.TimeRemaining);
                }
            }
            
            ////////CLIENTS AS WELL
            

            if (m_currentGamePhase.Value == GamePhase.PreGame)      // let all clients use InvitePanel
            {
                m_inviteFriendButtonContainer.SetActive(true);
            }

            OnPhaseChanged(currentPhase, currentPhase);          // manually run the RepNotify of currentPhase
            NotifyPhaseListener(m_currentGamePhase.Value);                                  // notify all Phase Listeners
            m_teamColorIsSet = true;
        }

        public void RegisterPhaseListener(IGamePhaseListener listener)
        {
            m_phaseListeners.Add(listener);
            listener.OnPhaseChanged(m_currentGamePhase.Value);
            listener.OnTeamColorUpdated(TeamAColor, TeamBColor);             // on first Registering Phase Listenership, run initial funcs that happen on NotifyPhaseListener
        }

        public void UnregisterPhaseListener(IGamePhaseListener listener)
        {
            _ = m_phaseListeners.Remove(listener);
        }
        
        private void NotifyPhaseListener(GamePhase newphase)
        {
            foreach (var listener in m_phaseListeners)
            {
                listener.OnPhaseChanged(newphase);
            }
        }
        
        
        

        public void UpdatePlayerTeam(ulong clientId, NetworkedTeam.Team team)    // update dictionary of PlayerIDs to Team values
        {
            m_playersTeamSelection[clientId] = team;            
        }
        
        public NetworkedTeam.Team GetTeamWithLeastPlayers()         // return a Team, which is an enum declared in NetworkedTeam
        {
            // Initialize counters for each team
            var teamCounts = new Dictionary<NetworkedTeam.Team, int>
            {
                { NetworkedTeam.Team.TeamA, 0 },
                { NetworkedTeam.Team.TeamB, 0 },
                { NetworkedTeam.Team.TeamC, 0 },
                { NetworkedTeam.Team.TeamD, 0 }
            };

            // Count players for each team
            foreach (var team in m_playersTeamSelection.Values)
            {
                if (teamCounts.ContainsKey(team))
                {
                    teamCounts[team]++;
                }
            }

            // Find the team with the least players
            NetworkedTeam.Team leastPopulatedTeam = teamCounts.OrderBy(kvp => kvp.Value).First().Key;

            if (leastPopulatedTeam == NetworkedTeam.Team.TeamB)
            {
                Debug.Log("is choosing team B");
            }
                
            return leastPopulatedTeam;
        }

        

        
        [ContextMenu("StartGame")]
        public void StartGame()             // called by unityEvent on the StartGameButton,   only Host/Server can start game 
        {
            if (LocalPlayerEntities.Instance.Avatar == null)
            {
                Invoke("StartGame",0.5f);
                Debug.Log("trying to start game without local avatar will cause teleport to fail");
                return;
            }
            
            if (m_currentGamePhase.Value is GamePhase.PreGame or GamePhase.PostGame)          // only if we're in a compatible GamePhase
            {
                m_gameState.Score.Reset();
                _ = StartCoroutine(DeactivateStartButton());

                                                                        
                if (m_currentGamePhase.Value is GamePhase.PreGame)            // if in PreGame, in oppose to PostGame
                {
                    CheckPlayersSides();                 // assign teams based on which side of the arena people are standing.   If people join afterwards, theyll have teams auto assigned and given spawnlocations
                    LockPlayersTeams();              // dont allow team switching from now
                }

                StartCountdown();                                                                           // START COUNTDOWN                                                  
                if(SpawningManagerBase.Instance) ((ArenaPlayerSpawningManager)SpawningManagerBase.Instance).ResetInGameSpawnPoints();         // set all game spawn points as unclaimed
                RespawnAllPlayers();    
               
            }
            else
            {
                // game is already InPlay
            }
            
            StartBlockamiGame();
        }


        public void StartBlockamiGame()
        {
            
            SpawnManager.Instance.Invoke("ResumeSpawning", SpawnManager.Instance.m_AllScs.Any() ? 5.0f : 0.1f);
            foreach (var ai in m_allAIPlayers)
            {
                ai.PauseShootingForSeconds(SpawnManager.Instance.m_AllScs.Any() ? 5.0f : 0.1f,false);
            }
            
            SpawnManager.Instance.PauseSpawning();
            SpawnManager.Instance.ClearAllCubes();
            SpawnManager.Instance.TriggerFrenzyTime();

            SpawnManager.Instance.ResetAllPlayerHealthCubes();







        }
        
        
        private void StartCountdown()
        {
            m_gameStartTime.Value = NetworkManager.Singleton.ServerTime.Time + GAME_START_COUNTDOWN_TIME_SEC;
            m_currentGamePhase.Value = GamePhase.CountDown;
            m_countdownView.Show(m_gameStartTime.Value, SwitchToInGame);                             // SwitchToInGame func is being passed in, as a delegate for when CountDownView.Show() is complete 
        }

        
        
        public void SwitchToInGame()                                                            // go to InGame
        {
            m_currentGamePhase.Value = GamePhase.InGame;
            m_gameEndTime.Value = NetworkManager.Singleton.ServerTime.Time + GAME_DURATION_SEC;       // set an explicit Server time to be m_gameEndTime, and replicate it
            m_ballSpawner.SpawnInitialBalls();
        }
        
        
        
        private void Update()
        {
            if (m_currentGamePhase.Value == GamePhase.InGame)                    // all clients run this update, using replicated values
            {
                var timeLeft = m_gameEndTime.Value - NetworkManager.Singleton.ServerTime.Time;     // update time to PhaseListeners
                UpdateTimeInPhaseListener(Math.Max(0, timeLeft));

                if (timeLeft < 11)
                {
                    var seconds = Math.Max(0, (int)Math.Floor(timeLeft));
                    if (m_previousSecondsLeft != seconds)
                    {
                        TriggerEndGameCountdownBeep(seconds);      // start countdown beep
                    }

                    m_previousSecondsLeft = seconds;

                    if (IsServer)
                    {
                        if (timeLeft < 0)
                        {
                            GoToPostGame();               // only server can GoToPostGame
                        }
                    }
                }
            }
            else if (m_currentGamePhase.Value == GamePhase.PreGame)       // check player sides on Tick if Pregame  ( this was checked previously on StartGame, but here its constant)
            {
                if (NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsServer)
                {
                    CheckPlayersSides();
                }
            }
        }

        
        private void GoToPostGame()
        {
            m_ballSpawner.DeSpawnAllBalls();
            m_currentGamePhase.Value = GamePhase.PostGame;
            m_restartGameButtonContainer.SetActive(true);
            ((ArenaPlayerSpawningManager)SpawningManagerBase.Instance).ResetPostGameSpawnPoints();
            RespawnAllPlayers();                      // respawn all players, now that its postgame,  go to podium
        }


        
        private void OnHostMigrationStarted()
        {
            if (m_currentGamePhase.Value == GamePhase.InGame)     // if a host migration is starting, update TimeRemaining in the GameStateSave
            {
                m_gameStateSave.TimeRemaining = m_gameEndTime.Value - NetworkManager.Singleton.ServerTime.Time;
            }
        }
        
        
        
        private void HandleInGameHostMigration(double timeRemaining)     // if a new host is started during InGame, InitalBalls need to be spawned for some reason??
        {
            m_currentGamePhase.Value = GamePhase.InGame;
            m_gameEndTime.Value = NetworkManager.Singleton.ServerTime.Time + timeRemaining;
            m_ballSpawner.SpawnInitialBalls();         
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
                                    // We need to finish processing the pointers before deactivating UI   ????
            yield return new WaitForEndOfFrame();
            m_startGameButtonContainer.SetActive(false);
            m_restartGameButtonContainer.SetActive(false);
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

        private void OnGUI()            // put a clickable button on screen to start game,  only server can see this on PC
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

        
        
        private void UpdateTimeInPhaseListener(double timeLeft)
        {
            foreach (var listener in m_phaseListeners)
            {
                listener.OnPhaseTimeUpdate(timeLeft);
            }
        }

        private void NotifyTeamColorListener(TeamColor teamColorA, TeamColor teamColorB)         // notify RPC for TeamColor update listeners, that dont have replicated color member variables themselves to bind repnotify to
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
                var playerObjects = LocalPlayerEntities.Instance?.GetPlayerObjects(clientId);
                if (playerObjects==null) return;
                
                var avatar = playerObjects.Avatar;
                if (avatar != null)
                {
                    // dont change player teams for some reason
                    
                    // var side = avatar.transform.position.z < 0                // assign sides based on Position,   are Player Teams flexible depending on what side Players have moved to??
                    //     ? NetworkedTeam.Team.TeamA
                    //     : NetworkedTeam.Team.TeamB;
                    //
                    // var color = side == NetworkedTeam.Team.TeamA ? TeamAColor : TeamBColor;
                    //
                    // foreach (var colorComp in playerObjects.ColoringComponents)   // class PlayerGameObjects keeps a list of ColoringComponent scripts to store team color
                    // {
                    //     colorComp.TeamColor = color;
                    // }
                    //
                    // m_playersTeamSelection[clientId] = side;
                }
            }
        }

        private void RespawnAllPlayers()   // only server does this
        {
            if (!LocalPlayerEntities.Instance) return;
            
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
        

        [ClientRpc]
        public void OnRespawnClientRpc(Vector3 position, Quaternion rotation, GamePhase phase, ClientRpcParams rpcParams)        // literally just teleport a player somewhere and call it respawn
        {
            if (phase is GamePhase.PostGame or GamePhase.CountDown)       // ensure again, no movement in PostGame or CountDown
            {
                PlayerInputController.Instance.MovementEnabled = false;
            }
            PlayerMovement.Instance.TeleportTo(position, rotation);
            LocalPlayerEntities.Instance.LeftGloveHand.ResetGlove();       // return gloves to uncharged state
            LocalPlayerEntities.Instance.RightGloveHand.ResetGlove();
        }
        
        
        
        
        private void OnHealthCubeDied(HealthCubeTransform obj)
        {
            var allhct = obj.OwningDrawingGrid.AllHealthCubeTransforms;
            var filteredHct = allhct.Where(htc => htc.OwningHealthCube != null).ToList();
            if (filteredHct.Count == 0)
            {
                Debug.Log("All Heath cubes destroyed for " + obj.OwningDrawingGrid.name);
                SpawnManager.Instance.ClearAllCubes();
                Invoke("GoToPostGame", 5.0f);
            }
            else
            {
                SpawnManager.Instance.TriggerFrenzyTime();
            }
            
            foreach (var hct in filteredHct)
            {
                Debug.Log($"HealthCubeTransform: {hct.name} is still active.");
            }
        }
        
        
        
        
        public void   EmojiCubeHitFloor()
        { 
            OnEmojiCubeHitFloor?.Invoke();
        }
    }
    
   
    
    
    
    
}