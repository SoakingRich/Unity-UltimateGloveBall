// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System;
using System.Collections.Generic;
using System.Linq;
using UltimateGloveBall.Arena.Gameplay;
using UltimateGloveBall.Arena.Player;
using UltimateGloveBall.Arena.Spectator;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace UltimateGloveBall.Arena.Services
{
    /// <summary>
    /// The Spawning Manager handles player connecting to an arena. It handles spawning players in the right teams and
    /// the right position. When a player requests to be spawned in the game, this controller will assess what player
    /// or spectator prefab to generate, their position, team and team color.
    /// </summary>
    public class ArenaPlayerSpawningManager : SpawningManagerBase
    {
        [SerializeField] private NetworkObject m_playerPrefab;

        [SerializeField] private NetworkObject m_gloveArmaturePrefab;
        [SerializeField] private NetworkObject m_gloveHandPrefab;

        [SerializeField] private NetworkObject m_spectatorPrefab;

        [SerializeField] private GameManager m_gameManager;

        [SerializeField] private Transform[] m_teamASpawnPoints = Array.Empty<Transform>();
        [SerializeField] private Transform[] m_teamBSpawnPoints = Array.Empty<Transform>();
        [SerializeField] private Transform[] m_teamCSpawnPoints = Array.Empty<Transform>();
        [SerializeField] private Transform[] m_teamDSpawnPoints = Array.Empty<Transform>();

        [SerializeField] private SpawnPointReservingService m_spectatorASpawnPoints;
        [SerializeField] private SpawnPointReservingService m_spectatorBSpawnPoints;

        [SerializeField] private SpawnPointReservingService m_winnerSpawnPoints;
        [SerializeField] private SpawnPointReservingService m_loserSpawnPoints;
        private bool m_tieAlternateToWin = true;

        // spawn point randomizer
        private Queue<int> m_teamARandomSpawnOrder = new();
        private Queue<int> m_teamBRandomSpawnOrder = new();
        private Queue<int> m_teamCRandomSpawnOrder = new();
        private Queue<int> m_teamDRandomSpawnOrder = new();

        private readonly List<int> m_tempListForSpawnPoints = new();








        public override NetworkObject
            SpawnPlayer(ulong clientId, string playerId, bool isSpectator,
                Vector3 playerPos) // main spawn player,  from OnHostStarted or OnClientConnected
        {
            if (isSpectator) return SpawnSpectator(clientId, playerId, playerPos);
            
            ArenaSessionManager.Instance.SetupPlayerData(clientId, playerId,              // let ArenaSessionManager add a new ArenaPlayerData to its dictionary 
                new ArenaPlayerData(clientId,                                       // using ClientID (from network object), PlayerID (from OculusID),   it will decide if its a reconnection
                    playerId));                    
            
            var playerData = ArenaSessionManager.Instance.GetPlayerData(playerId).Value;      // find the newly created ArenaPlayerData


            GetSpawnData(ref playerData, playerPos, out var position, out var rotation,      // get spawn data for the player
                out var team, // setup which team for the spawning player
                out var color, out var spawnTeam); // setup spawning data based on gamePhase
                                                    

            var player = Instantiate(m_playerPrefab, position, rotation);               // spawn the AvatarEntity
            player.SpawnAsPlayerObject(
                clientId);              // after spawning, Netcode designate it as a PlayerObject    
            
            player.GetComponent<NetworkedTeam>().MyTeam = team;

            var leftArmatureNet =
                Instantiate(m_gloveArmaturePrefab, Vector3.down,
                    Quaternion.identity);                                                       // spawn gloves/hands for player   LEFT
            var leftArmature = leftArmatureNet.GetComponent<GloveArmatureNetworking>();
            leftArmature.Side = Glove.GloveSide.Left;
            leftArmatureNet.GetComponent<TeamColoringNetComponent>().TeamColor =
                color; // anything that has TeamColoringNetComponents need color updated on RepNotify of changed var
            var leftHandNet = Instantiate(m_gloveHandPrefab, Vector3.down, Quaternion.identity);
            var leftHand = leftHandNet.GetComponent<GloveNetworking>();
            leftHand.Side = Glove.GloveSide.Left;
            leftHandNet.GetComponent<TeamColoringNetComponent>().TeamColor = color;

            var rightArmatureNet =
                Instantiate(m_gloveArmaturePrefab, Vector3.down, Quaternion.identity); // spawn right hand/glove
            var rightArmature = rightArmatureNet.GetComponent<GloveArmatureNetworking>();
            rightArmature.Side = Glove.GloveSide.Right;
            rightArmatureNet.GetComponent<TeamColoringNetComponent>().TeamColor = color;
            var rightHandNet = Instantiate(m_gloveHandPrefab, Vector3.down, Quaternion.identity);
            var rightHand = rightHandNet.GetComponent<GloveNetworking>();
            rightHand.Side = Glove.GloveSide.Right;
            rightHandNet.GetComponent<TeamColoringNetComponent>().TeamColor = color;


            rightArmatureNet
                .SpawnWithOwnership(
                    clientId); // instead of telling these NetworkObject hand/armatures to networkspawn, we do SpawnWithOwnerShip
            rightHandNet.SpawnWithOwnership(clientId);
            leftArmatureNet.SpawnWithOwnership(clientId);
            leftHandNet.SpawnWithOwnership(clientId); // give the client player ownership of gloves/hands

            player.GetComponent<PlayerControllerNetwork>().ArmatureLeft =
                leftArmature; //populate vars on  PlayerControllerNetwork
            player.GetComponent<PlayerControllerNetwork>().ArmatureRight = rightArmature;
            player.GetComponent<PlayerControllerNetwork>().GloveLeft = leftHand;
            player.GetComponent<PlayerControllerNetwork>().GloveRight = rightHand;

            playerData.SelectedTeam = team;
            m_gameManager.UpdatePlayerTeam(clientId,
                spawnTeam); // update the player team in the game manager - dictionary of ClientID to teams
            
            ArenaSessionManager.Instance.SetPlayerData(clientId,
                playerData);                        // update the playerdata in the ArenaSession,   now we know the team and spawn point index (etc??)

            AssignDrawingGrid(player);

            return player;
        }

        
        
        
        
        
        public void AssignDrawingGrid(NetworkObject player) // assign the player to a DrawingGrid          // 'player' is actually a PlayerControllerNetwork gO
        {
            var PlayerData = ArenaSessionManager.Instance.GetPlayerData(player.OwnerClientId);     
            int teamIdx = (int)PlayerData.Value.SelectedTeam - 1;     // subtract 1 from team enum to get index for DrawingGrids
            
            DrawingGrid playerGrid = null;
            DrawingGrid[] allDrawingGrids = FindObjectsOfType<DrawingGrid>();
            playerGrid = allDrawingGrids.FirstOrDefault(grid => grid.DrawingGridIndex == teamIdx);     // find the grid by index
            
            if (playerGrid == null)
            {
                Debug.LogError("No DrawingGrid with correct team index found.");
                return;
            }

            playerGrid.OwningPlayer.Value = player.OwnerClientId; 
            playerGrid.NetworkObject.ChangeOwnership(player.OwnerClientId);
            
            // "player" is already a PlayerController, i could just set the OwnedDrawingGrid there instead of the following  ????
            if (LocalPlayerEntities.Instance.GetPlayerObjects(player.OwnerClientId).PlayerController)   // try to find the PlayerControllerNetwork for the spawned player to set their OwnedDrawingGrid
            {
                LocalPlayerEntities.Instance.GetPlayerObjects(player.OwnerClientId).PlayerController
                    .OwnedDrawingGrid = playerGrid;
            }
            else
            {
                Debug.LogWarning($"no playercontrollernetwork for player???");
            }

            Debug.Log($"Assigned DrawingGrid at {playerGrid.transform.position} to player.");
            
            var clientRpcParams = new ClientRpcParams          // create ClientRPC Params so we can specify Targets for rpc as we only want to send Respawn RPC to one client only
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { player.OwnerClientId } }
            };
            
            GameManager gm = FindObjectOfType<GameManager>();
            Vector3 pos = playerGrid.transform.position + playerGrid.transform.rotation * Vector3.forward * -1.0f;
            pos.y = 0;
            gm.OnRespawnClientRpc(pos, playerGrid.transform.rotation, gm.CurrentPhase, clientRpcParams);
            
                
            
                
            
        }


        
        
        
        
        private void GetSpawnData(ref ArenaPlayerData playerData, Vector3 currentPos,
            out Vector3 position, // setup where the joining player should spawn given the current game phase
            out Quaternion rotation, out NetworkedTeam.Team team, out TeamColor teamColor,
            out NetworkedTeam.Team spawnTeam)
        {

            var currentPhase = m_gameManager.CurrentPhase;

            team = currentPhase switch // choose a team based on game current phase
            {
                GameManager.GamePhase.InGame or GameManager.GamePhase.CountDown => GetTeam(playerData,
                    currentPos), // if in game already, this might be a reconnection, playerdata might already have a team, use it
                // Position is relevant, because if player is reconnecting they'll be somewhere that isnt world zero ???
           

                GameManager.GamePhase.PostGame => GetTeam(playerData,
                    currentPos), // if post game, same scenario, might be reconnection during post game, use it

                GameManager.GamePhase.PreGame => NetworkedTeam.Team
                    .NoTeam, // if pregame, treat as no team yet, go with least populated later

                _ => NetworkedTeam.Team.NoTeam, // default case never happens ??
            };

            spawnTeam = team;
            if (spawnTeam == NetworkedTeam.Team.NoTeam) // if after those checks theres no team, get one
            {
                spawnTeam = GetTeam(playerData, currentPos);
            }

            team = spawnTeam;
            
            GetSpawnPositionForTeam(currentPhase, spawnTeam, ref playerData, out position,
                out rotation); // get spawn position now that player has a team

            teamColor = GetTeamColor(spawnTeam);
        }





        private NetworkedTeam.Team
            GetTeam(ArenaPlayerData playerData,
                Vector3 currentPos) // return the team thats already set in playerData or else get a new one
        {
            if (playerData.SelectedTeam != NetworkedTeam.Team.NoTeam)
            {
                return playerData.SelectedTeam; // if playerData already has a team, use it
            }

            NetworkedTeam.Team team;

            //  team = m_gameManager.GetTeamWithLeastPlayers();
            //  return team;

            if (currentPos == Vector3.zero) // if we are a new player joining
            {
                team = m_gameManager.GetTeamWithLeastPlayers(); // Get Team With Least Players
                return team;
            }
            else // if we are standing somewhere in the arena
            {
                if (m_gameManager.CurrentPhase ==
                    GameManager.GamePhase
                        .PostGame) // HANDLE POST GAME - if we are in the post game and player already has a position, join the losing side??
                {
                    var winningTeam = GameState.Instance.Score.GetWinningTeam();

                    if (winningTeam == NetworkedTeam.Team.NoTeam)
                    {
                        winningTeam = NetworkedTeam.Team.TeamA;
                    } //  some sort of bug fix??,   if winning team is none (no one has won yet, but were still in PostGame somehow, consider TeamA the winning team??)  

                    var losingTeam =
                        winningTeam == NetworkedTeam.Team.TeamA // losing team equals the opposite of winning team
                            ? NetworkedTeam.Team.TeamB
                            : NetworkedTeam.Team.TeamA;

                    team =
                        Mathf.Abs(currentPos.z -
                                  m_winnerSpawnPoints.transform.position
                                      .z) >= // still in post game, assign team based on if we are closer to WinnerSpawnPoints vs Loserspawnpoints, we would have been teleported to one or the other
                        Mathf.Abs(currentPos.z - m_loserSpawnPoints.transform.position.z)
                            ? winningTeam
                            : losingTeam;
                }
                else // GamePhase is PreGame or InGame        
                {
                    team = currentPos.z < 0
                        ? NetworkedTeam.Team.TeamA
                        : NetworkedTeam.Team.TeamB; // assign teams based on what side of Zero we're standing
                }
            }

            return team;
        }





        private void GetSpawnPositionForTeam(GameManager.GamePhase gamePhase,
            NetworkedTeam.Team team, // get a spawn position, dequeued randomized spawn point on team side, or on podium
            ref ArenaPlayerData playerData,
            out Vector3 position, out Quaternion rotation)
        {


            if (gamePhase ==
                GameManager.GamePhase
                    .PostGame) ////////     // HANDLE POST GAME    // handle spawning into a postgame podium
            {
                var winningTeam = GameState.Instance.Score.GetWinningTeam();
                var useWin = winningTeam == team;
                if (winningTeam == NetworkedTeam.Team.NoTeam)
                {
                    useWin = m_tieAlternateToWin;
                    m_tieAlternateToWin = !m_tieAlternateToWin;
                }

                Transform trans = null;
                if (useWin)
                {
                    trans = m_winnerSpawnPoints.ReserveRandomSpawnPoint(out var index);
                    playerData.PostGameWinnerSide = true;
                    playerData.SpawnPointIndex = index;
                }

                if (trans == null)
                {
                    trans = m_loserSpawnPoints.ReserveRandomSpawnPoint(out var index);
                    playerData.PostGameWinnerSide = false;
                    playerData.SpawnPointIndex = index;
                }

                position = trans.position;
                rotation = trans.rotation;

            }

            // if (team == NetworkedTeam.Team.TeamA)       ////// HANDLE NON POST GAME
                // {
                //     if (m_teamARandomSpawnOrder.Count <= 0)                                                 // read a list of randomized indexes for spawn points
                //     {
                //         RandomizeSpawnPoints(m_teamASpawnPoints.Length, ref m_teamARandomSpawnOrder);
                //     }
                //
                //     var point = m_teamASpawnPoints[m_teamARandomSpawnOrder.Dequeue()];                  // remove the index from the queue and use it
                //     position = point.position;
                //     rotation = point.rotation;
                // }
                // else
                // {
                //     if (m_teamBRandomSpawnOrder.Count <= 0)
                //     {
                //         RandomizeSpawnPoints(m_teamBSpawnPoints.Length, ref m_teamBRandomSpawnOrder);
                //     }
                //
                //     var point = m_teamBSpawnPoints[m_teamBRandomSpawnOrder.Dequeue()];
                //     position = point.position;
                //     rotation = point.rotation;

                // Handle Post-Game podium spawning
                // Handle spawning during other phases

                Transform spawnPoint = GetRandomSpawnPointForTeam(team, out var spawnIndex);
                playerData.SpawnPointIndex = spawnIndex;

                position = spawnPoint.position;
                rotation = spawnPoint.rotation;
            }
        
    


    
    private Transform GetRandomSpawnPointForTeam(NetworkedTeam.Team team, out int spawnIndex)
        {
            // Mapping team enums to their respective spawn point lists and random queues
            Dictionary<NetworkedTeam.Team, (Transform[] spawnPoints, Queue<int> randomOrder)> teamData = new()
            {
                { NetworkedTeam.Team.TeamA, (m_teamASpawnPoints, m_teamARandomSpawnOrder) },
                { NetworkedTeam.Team.TeamB, (m_teamBSpawnPoints, m_teamBRandomSpawnOrder) },
                { NetworkedTeam.Team.TeamC, (m_teamCSpawnPoints, m_teamCRandomSpawnOrder) },
                { NetworkedTeam.Team.TeamD, (m_teamDSpawnPoints, m_teamDRandomSpawnOrder) }
            };

            if (!teamData.TryGetValue(team, out var data))
                throw new ArgumentException($"Invalid team specified: {team}");

            var (spawnPoints, randomOrder) = data;

            if (randomOrder.Count <= 0)
            {
                RandomizeSpawnPoints(spawnPoints.Length, ref randomOrder);
            }

            spawnIndex = randomOrder.Dequeue();
            return spawnPoints[spawnIndex];
        }
        
        private void RandomizeSpawnPoints(int length, ref Queue<int> randomQueue)
        {
            m_tempListForSpawnPoints.Clear();
            for (var i = 0; i < length; ++i)
            {
                m_tempListForSpawnPoints.Add(i);
            }

            var n = length;
            while (n > 1)
            {
                n--;
                var k = Random.Range(0, n);
                var value = m_tempListForSpawnPoints[k];
                m_tempListForSpawnPoints[k] = m_tempListForSpawnPoints[n];
                m_tempListForSpawnPoints[n] = value;
            }

            randomQueue.Clear();
            for (var i = 0; i < length; ++i)
            {
                randomQueue.Enqueue(m_tempListForSpawnPoints[i]);
            }

            m_tempListForSpawnPoints.Clear();
        }


        protected override void OnClientDisconnected(ulong clientId)       // this is a NetworkBehavior so it gets OnClientDisconnected callbacks
        {
            var playerData = ArenaSessionManager.Instance.GetPlayerData(clientId);
            if (playerData.HasValue)
            {
                var data = playerData.Value;
                data.IsConnected = false;                   // set is connected to false
                
                
                if (m_gameManager.CurrentPhase == GameManager.GamePhase.PostGame)
                {
                    if (data.SpawnPointIndex > 0)
                    {
                        if (data.PostGameWinnerSide)
                        {
                            m_winnerSpawnPoints.ReleaseSpawnPoint(data.SpawnPointIndex);                // release podium spawn points if client disconnects,  others might join
                        }
                        else
                        {
                            m_loserSpawnPoints.ReleaseSpawnPoint(data.SpawnPointIndex);
                        }
                    }
                }

                ArenaSessionManager.Instance.SetPlayerData(clientId, data);               // set data as player IsConnected false
            }
        }


        private TeamColor GetTeamColor(NetworkedTeam.Team team)            // ask the game manager what colors teams get
        {
            var useTeamA = team == NetworkedTeam.Team.TeamA;
            var color = useTeamA ? m_gameManager.TeamAColor : m_gameManager.TeamBColor;
            return color;
        }
        
        
        
        public override void GetRespawnPoint(ulong clientId, NetworkedTeam.Team team,                       // i dont need respawning
            out Vector3 position, out Quaternion rotation)
        {
            var playerData = ArenaSessionManager.Instance.GetPlayerData(clientId).Value;
            GetSpawnPositionForTeam(m_gameManager.CurrentPhase, team, ref playerData, out position, out rotation);         // get a new spawn position given PlayerData already exists, player already has a position
            ArenaSessionManager.Instance.SetPlayerData(clientId, playerData);
        }

        
        
        public void ResetPostGameSpawnPoints()
        {
            m_winnerSpawnPoints.Reset();           // unclaim all the spawn points in reserving service
            m_loserSpawnPoints.Reset();
        }

        public void ResetInGameSpawnPoints()
        {
            RandomizeSpawnPoints(m_teamASpawnPoints.Length, ref m_teamARandomSpawnOrder);             // randomize the main team spawn points
            RandomizeSpawnPoints(m_teamBSpawnPoints.Length, ref m_teamBRandomSpawnOrder);
            RandomizeSpawnPoints(m_teamCSpawnPoints.Length, ref m_teamCRandomSpawnOrder);
            RandomizeSpawnPoints(m_teamDSpawnPoints.Length, ref m_teamDRandomSpawnOrder);
        }
        
        
        
        
        
        private NetworkObject SpawnSpectator(ulong clientId, string playerId, Vector3 playerPos)           // spawn a player without Gloves
        {
            ArenaSessionManager.Instance.SetupPlayerData(clientId, playerId,
                new ArenaPlayerData(clientId, playerId, true));       // setup data in ArenaSessionManager for the spectator
            
            var playerData = ArenaSessionManager.Instance.GetPlayerData(playerId).Value;      // get the newly made data struct
            
            Transform spawnPoint;
            
            if (playerData.SelectedTeam == NetworkedTeam.Team.NoTeam)
            {
                bool useA;
                var findClosestSpawnPoint = false;
                if (playerPos == Vector3.zero)
                {
                    useA = Random.Range(0, 2) == 0;
                }
                else
                {
                    useA = playerPos.z < 0;
                    findClosestSpawnPoint = true;
                }

                var spawnPoints = useA ? m_spectatorASpawnPoints : m_spectatorBSpawnPoints;
                spawnPoint = findClosestSpawnPoint
                    ? spawnPoints.ReserveClosestSpawnPoint(playerPos, out var spawnIndex)
                    : spawnPoints.ReserveRandomSpawnPoint(out spawnIndex);

                if (spawnPoint == null)
                {
                    useA = !useA;
                    spawnPoints = useA ? m_spectatorASpawnPoints : m_spectatorBSpawnPoints;
                    spawnPoint = spawnPoints.ReserveRandomSpawnPoint(out spawnIndex);
                }

                playerData.SelectedTeam = useA ? NetworkedTeam.Team.TeamA : NetworkedTeam.Team.TeamB;
                playerData.SpawnPointIndex = spawnIndex;
            }
            else
            {
                var spawnPoints = playerData.SelectedTeam == NetworkedTeam.Team.TeamA
                    ? m_spectatorASpawnPoints
                    : m_spectatorBSpawnPoints;
                if (playerData.SpawnPointIndex < 0)
                {
                    spawnPoint = spawnPoints.ReserveRandomSpawnPoint(out var spawnIndex);
                    playerData.SpawnPointIndex = spawnIndex;
                }
                else
                {
                    spawnPoint = spawnPoints.GetSpawnPoint(playerData.SpawnPointIndex, true);
                }
            }

            var position = spawnPoint.position;
            var rotation = spawnPoint.rotation;
            var spectator = Instantiate(m_spectatorPrefab, position, rotation);
            spectator.GetComponent<SpectatorNetwork>().TeamSideColor =
                playerData.SelectedTeam == NetworkedTeam.Team.TeamA
                    ? m_gameManager.TeamAColor
                    : m_gameManager.TeamBColor;
            spectator.SpawnAsPlayerObject(clientId);
            ArenaSessionManager.Instance.SetPlayerData(clientId, playerData);
            return spectator;
            
        }

        public Transform SwitchSpectatorSide(ulong clientId, SpectatorNetwork spectator)
        {
            var playerData = ArenaSessionManager.Instance.GetPlayerData(clientId).Value;
            if (!playerData.IsSpectator)
            {
                return null;
            }

            var spawnPoints = playerData.SelectedTeam == NetworkedTeam.Team.TeamA
                ? m_spectatorASpawnPoints
                : m_spectatorBSpawnPoints;
            spawnPoints.ReleaseSpawnPoint(playerData.SpawnPointIndex);

            // switch teams
            playerData.SelectedTeam = playerData.SelectedTeam == NetworkedTeam.Team.TeamA
                ? NetworkedTeam.Team.TeamB
                : NetworkedTeam.Team.TeamA;
            spectator.TeamSideColor = playerData.SelectedTeam == NetworkedTeam.Team.TeamA
                ? m_gameManager.TeamAColor
                : m_gameManager.TeamBColor;
            spawnPoints = playerData.SelectedTeam == NetworkedTeam.Team.TeamA
                ? m_spectatorASpawnPoints
                : m_spectatorBSpawnPoints;
            var newLocation = spawnPoints.ReserveRandomSpawnPoint(out var spawnIndex);
            playerData.SpawnPointIndex = spawnIndex;
            ArenaSessionManager.Instance.SetPlayerData(clientId, playerData);
            return newLocation;
        }
    }
    
    
}
