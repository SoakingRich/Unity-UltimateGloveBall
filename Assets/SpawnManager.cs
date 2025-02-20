using System;
using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blockami.Scripts;
using Meta.Multiplayer.Core;
using Meta.Utilities;
using UltimateGloveBall.App;
using UltimateGloveBall.Arena.Gameplay;
using UltimateGloveBall.Arena.Player;
using UltimateGloveBall.Arena.Services;
using UltimateGloveBall.Arena.VFX;
using UltimateGloveBall.Networking.Pooling;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
using static UtilityLibrary;

public class SpawnManager : NetworkBehaviour, IGamePhaseListener   
{
    private static SpawnManager _instance;

    public static SpawnManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing instance in the scene
                _instance = FindObjectOfType<SpawnManager>();

                if (_instance == null)
                {
             
                }
            }
            return _instance;
        }
    }



    #region Variables

    

    
    [Header("Editor")]
    [SerializeField] public bool BeginSpawnOnStart = true; 
    
    
    [Header("Spawn Settings")]
    [SerializeField] public int maxSceneCubes = 50;
    [SerializeField] public NetworkObjectPool m_cubePool;

    [Header("State")]

    [SerializeField] public bool isSpawning = false;
    [SerializeField] public bool isFrenzyTime = false;
    [SerializeField] public int currentSceneCubeCount = 0;
    [SerializeField] public float currentSpawnRate;
    //[SerializeField] private Object sceneCubePrefab;
    public Action OnTimeThresholdPassed;
    private bool TimeThresholdCalled = false;
    
   // [Header("Actions")] public Action OnSCSDied;
    [Header("Actions")] 
    public Action<ulong> OnSCSDied;
    public Action OnDownCubeLayerDestroy;
    public Action<SceneCubeNetworking> OnHealthCubeHit;
    
    [Header("World")] 
    [SerializeField] private List<SpawnZone> allSpawnZones = new List<SpawnZone>();

    [Header("Internal")] 
    public readonly List < SceneCubeNetworking> m_AllScs  = new();
    public readonly List < PlayerCubeScript> m_AllPcs  = new();
    public NetworkObject sceneCubePrefab;
    public NetworkObject PlayerShotPrefab;
    public NetworkObject HealthCubePrefab;
    [SerializeField] private NetworkObject RainbowCubePrefab;
    [SerializeField] private NetworkObject DownCubePrefab;
    [SerializeField] private NetworkObject HealthCubePickupPrefab;
    [SerializeField] private NetworkObject MissilePickupPrefab;
    [SerializeField] private NetworkObject NumberCubePrefab;
    private Coroutine SpawnTimerHandle;
    private Coroutine frenzyCoroutine;
    public GameManager m_gameManager => GameManager.Instance;
    public GameManager.GamePhase _gamePhase => m_gameManager.CurrentPhase;
    public float FrenzyOverideDuration = 0.0f;
    private System.Random rng = new System.Random();
    
    
    
    #endregion
    
    
    
    
    
    private void Awake()
    {
            // Ensure only one instance exists
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject); // Keep it across scene loads if necessary
        
        allSpawnZones = FindObjectsOfType<SpawnZone>().ToList();
        currentSpawnRate = BlockamiData.Instance.DefaultSpawnRate;

        // NetworkLayer NetLayer = FindObjectOfType<NetworkLayer>();
        // NetLayer.OnClientConnectedCallback += OnClientConnectedCallback;
    }

    // private void OnClientConnectedCallback(ulong obj)
    // {
    //   
    // }

    

    private void OnDestroy()
    {
        m_gameManager.UnregisterPhaseListener(this);
    }

    
    
    private void Start()
    {
        m_gameManager.RegisterPhaseListener(this);
        if(BeginSpawnOnStart) StartSpawning();
        
        InvokeRepeating("SlowUpdate1Second", 1.0f, 2.5f);

   //  Debug.Log("Blockami Log - " + NetworkObject.IsSpawned);   
  //   NetworkObject.Spawn();
    }
    
    
    public void SlowUpdate1Second()
    {
        int count = 0;
        
        foreach (var scs in m_AllScs)
        {
            if (scs.transform.position.y > 1.2f)
            {
                count++;

                if (count > 2)
                {
                    Debug.Log("Try Overflow sound");
                    var audioController = AudioController.Instance;
                    audioController.PlaySound("overflow");
                    break;
                }
            }
        }
        
    }

    
    
    [ContextMenu("StartSpawning")]
public void StartSpawning()
{
    if (SpawnTimerHandle != null)
    {
        StopCoroutine(SpawnTimerHandle);
    }
    
 
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClientId == 0)
        {

            isSpawning = true;
            SpawnTimerHandle = StartCoroutine(SpawnSceneCubes());
            
        }
    
}

    public void StopSpawning()
    {
        if (SpawnTimerHandle != null)
        {
            StopCoroutine(SpawnTimerHandle);
        }

        isSpawning = false;
    }

    
    

private IEnumerator SpawnSceneCubes()
{
    yield return new WaitForSeconds(2.0f);
    
    while (true)
    {
        if (currentSceneCubeCount > BlockamiData.Instance.MaxCubes)
        {
            yield return null;
        }

        float spawnRate = isFrenzyTime ? BlockamiData.Instance.FrenzySpawnRate : BlockamiData.Instance.DefaultSpawnRate;

        // Wait before spawning to control the spawn rate
        yield return new WaitForSeconds(1f / spawnRate);

        if (m_cubePool.IsSpawned)
        {



            try
            {
                SpawnCubeServer(); // Spawn a random cube
            }
            catch (Exception ex)
            {
                Debug.LogError($"SpawnSceneCubes encountered an error: {ex}");
                // Optional: Delay before retrying to avoid excessive logging
            }
        }
        else
        {
            Debug.LogError("cube pool not network spawned");
            m_cubePool.NetworkObject.Spawn();
        }

    }
    
    Debug.Log("SpawnManager Has Stopped Spawning");
}


                    
private void SpawnCubeServer(Transform OverrideTransform = null, int OverrideColorID = -1, bool IsHealthCube = false)
                    {


                        if (!IsHealthCube && currentSceneCubeCount >= maxSceneCubes)
                        {
                            Debug.LogWarning("HITTING MAX SCENE CUBES");
                            return;
                            
                        }

                        if (IsWithEditor() && NetworkManager.Singleton.ConnectedClients.Count > 1 &&
                            currentSceneCubeCount > 10)   // hard code less cubes for testing
                        {
                            SceneCubeNetworking last;
                            last = m_AllScs[0];
                            last.KillSceneCubeServerRpc();
                            return;
                        }

                        if (allSpawnZones.Count <= 0) return;
                        
                        ////////////////////////////// Get Position  
                        var spawnPosition = GetRandomSpawnPosition();        // Spawn SceneCube at a random spawn zone in the sky
                        if (OverrideTransform != null)
                        {
                            spawnPosition = OverrideTransform.position;     // health cubes specify location explicitly
                        }

                        NetworkObject overrideprefab = null;
                        
                        //CHANCES OF
                        
                        ////////////////////////////// Get Color ID
                            int colorID;
                            colorID = BlockamiData.Instance.GetRandomColorID();
                            if(OverrideColorID > -1) colorID = OverrideColorID;

                         bool HasHealthPickup = false;
                         bool IsRainbow = false;
                         bool IsDownCube = false;
                         bool HasMissilePickup = false;
                         bool IsNumberCube = false;

                         float dist;
                         
                         if (!IsHealthCube)
                         {
                             float rand = Random.Range(0.0f, 1.0f);

                             if (rand > 0.98f)
                             {
                                 HasHealthPickup = true;  // Highest rarity
                             }
                             else if (rand > 0.93f)
                             {
                                 IsRainbow = true;
                                 colorID = 10;
                             }
                             else if (rand > 0.88f) 
                             {
                                 if (CheckForDownCubeSpawn(spawnPosition, out dist))
                                 {
                                     IsDownCube = true;
                                     colorID = 11;
                                 }
                             }
                             else if (rand > 0.84f)
                             {
                                 HasMissilePickup = true;
                             }
                             // else if (rand > 0.80f)
                             // {
                             //     colorID = 12;  // HeavyCube       PlayerCubeMatchesSceneCubeColorID
                             // }
                            else if (rand > 0.79f)
                          //   else if (rand > 0.00f)
                             {
                                 IsNumberCube = true;
                             }

                             // CheckForDownCubeSpawn(spawnPosition, out dist);
                             // if (dist < 1.0)
                             // {
                             //     Debug.Log("stack is too high, dist is " + dist);
                             //     AudioController.Instance.PlaySound("fail");
                             // }
                         }




                         ////////////////////////////// Get Prefab accounting for Exotic Type
                            SceneCubeData scd = BlockamiData.Instance.GetSceneCubeDataFromID(colorID);            // ATM the only reason for SceneCubeData to exist is for clients to retrieve Color from ID
                           var selectedPrefab = scd.SceneCubePrefab;           // retrieve a prefab associated with colorIds SceneCubeData type
                            
                            if (IsHealthCube) selectedPrefab = HealthCubePrefab;   // healthcube prefab is not determined By ID
                            if (HasHealthPickup) selectedPrefab = HealthCubePickupPrefab;  // healthcubepickup prefab is not determined By ID
                            if(IsRainbow) selectedPrefab = RainbowCubePrefab;
                            if(IsDownCube) selectedPrefab = DownCubePrefab;
                            if(HasMissilePickup) selectedPrefab = MissilePickupPrefab;
                            if (IsNumberCube) selectedPrefab = NumberCubePrefab;
                            

                            
                            if(selectedPrefab == null)
                            {
                                Debug.LogError("No prefab found for colorID: " + colorID);
                                Debug.Break();
                                return;
                            }
                            
                            ////////////////////////////// SPAWN
                            
                            var networkObject = m_cubePool.GetNetworkObject(selectedPrefab.gameObject, spawnPosition, Quaternion.identity);
                            if (!networkObject)
                            {
                                Debug.LogError("no network object for prefab in spawn manager ");
                            }
                            
                            if (!networkObject.IsSpawned)       // Spawning is only required the first time it is fetched from the pool.
                                networkObject.Spawn(); 
                            
                            
                            ////////////////////////////// INITIALIZE 
                            var go = networkObject.gameObject;
                            SceneCubeNetworking scs = go.GetComponent<SceneCubeNetworking>();
                            scs.NetIsHealthCube.Value = IsHealthCube;
                            scs.NetColorID.Value = colorID;                          // assign the ID so clients can initialize a color ID, if cube uses it
                            
                            // for exotic cubes, theyre traits will be evident to clients already via what prefab was used to spawn
                            // exotic cubes with unique ColorIDs can still initialize themselves from that alone
                            scs.Initialize();

                            if (!IsHealthCube)
                            {
                                currentSceneCubeCount++;
                                m_AllScs.Add(scs);
                            }

                            scs.SCDied += OnSceneCubeDied;
                        
             
                        
                    }

private static bool CheckForDownCubeSpawn(Vector3 spawnPosition, out float dist)
                    {
                        Ray ray;
                        Vector3 rayOrigin = spawnPosition;
                        Vector3 rayDirection = Vector3.down;
                        float rayDistance = 100.0f;
                        dist = 9999.0f;
                
                        ray = new Ray(rayOrigin, rayDirection * rayDistance);
                        int SceneCubeLayerMask = 1 << LayerMask.NameToLayer("Hitable");

                        RaycastHit hitInfo;

                        if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, SceneCubeLayerMask))
                        {
                            GameObject hitObject = hitInfo.collider.gameObject;
                            var scs = hitObject.GetComponent<SceneCubeNetworking>();
                            dist = hitInfo.distance;
                            

                           return scs != null;      // ray hit scene cube, or something else
                        }
                        
                        return false;   // ray hit nothing
                    }
                    

                    
                    
                    
                    
                    
                    

private void OnSceneCubeDied(SceneCubeNetworking destroyedCube)     // KillSceneCubeServerRpc
    {
        DeSpawnCube(destroyedCube);
        OnSCSDied?.Invoke(destroyedCube.NetworkObjectId);
    }
    
    // public void OnHealthCubeHit(SceneCubeNetworking destroyedCube)
    // {
    //     //TriggerFrenzyTime();
    //     destroyedCube.m_healthCubeTransform.OnHealthCubeHit?.Invoke( destroyedCube.m_healthCubeTransform);
    //
    // }
    
    public void DeSpawnCube(SceneCubeNetworking Sc)      
    {
        
        PlayHitVFXClientRpc(Sc.transform.position,Sc.transform.forward);
        
        Sc.SCDied -= OnSceneCubeDied;
        currentSceneCubeCount--;
        _ = m_AllScs.Remove(Sc);
        if (Sc.NetworkObject.IsSpawned) Sc.NetworkObject.Despawn();
        
    }

 
    
    
    
    
    public void ClearAllCubes()   // Destroy All Scene Cubes ,  DestroyAllSceneCubes
    {
        if (!IsServer) return;
        
        foreach (var cube in m_AllScs.ToList())  // return ball calls NetworkManager.Despawn   and we have a pool manager which intercepts that and returns to a pool
        {
            DeSpawnCube(cube);
        }
        
       
    }
    
    
   
  
    
    
    

    
    
    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    public void PlayHitVFXClientRpc(Vector3 position, Vector3 rotation)
    {
        VFXManager.Instance.PlayHitVFX(position,rotation);
    }

    
    

    public void TriggerFrenzyTime(bool Enable)
    {
        isFrenzyTime = Enable;

        if (frenzyCoroutine != null)
            StopCoroutine(frenzyCoroutine);
    
        frenzyCoroutine = StartCoroutine(FrenzyTimeRoutine());
    }

    private IEnumerator FrenzyTimeRoutine()
    {
        float DurationToUse = BlockamiData.Instance.FrenzyTimeDuration;
        if (FrenzyOverideDuration != 0.0f)
        {
            DurationToUse = FrenzyOverideDuration;
            FrenzyOverideDuration = 0.0f;
        }
        yield return new WaitForSeconds(DurationToUse);
        TriggerFrenzyTime(false);
    }
    

    
    
    
    
    
    private Vector3 GetRandomSpawnPosition()
    {
        var allPCs = FindObjectsOfType<PlayerCubeScript>();
        allSpawnZones = allSpawnZones.OrderBy(x => rng.Next()).ToList();
        
        allSpawnZones.Sort((a, b) => 
        {
            bool aAligned = IsAlignedWithAnyPC(a.transform.position, allPCs);
            bool bAligned = IsAlignedWithAnyPC(b.transform.position, allPCs);

            if (aAligned == bAligned) return 0; // No change in order
            return aAligned ? 1 : -1; // Move aligned ones to the end
        });
        
       // SpawnZone randomSpawnZone = allSpawnZones[Random.Range(0, allSpawnZones.Count)];
        SpawnZone randomSpawnZone = allSpawnZones[0];
        return randomSpawnZone.transform.position;
    }

    
    
    
    
    bool IsAlignedWithAnyPC(Vector3 position, PlayerCubeScript[] allPCs)
    {
        foreach (var pc in allPCs)
        {
            Vector3 pcPos = pc.transform.position;
            if (Mathf.Approximately(position.x, pcPos.x) || Mathf.Approximately(position.z, pcPos.z))
            {
                return true; // Aligned in cardinal direction
            }
        }
        return false;
    }
 
    
    
    public void RequestSpawnItemServer(string prefabName, Vector3 Position, Quaternion rotation, ulong clientId)
    {
        RequestSpawnItemServerRpc(prefabName, Position, rotation, clientId);
    }



    [ServerRpc(RequireOwnership = false)]
    public void RequestSpawnItemServerRpc(string prefabName, Vector3 Position, Quaternion rotation, ulong clientId)
    {
    
        if (!IsServer) return;
        
        var NetPrefab = NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs.FirstOrDefault(x => x.Prefab.name == prefabName);
        if (NetPrefab == null)
        {
            Debug.LogError("NetworkObject not found for prefab: " + prefabName);
            return;
        }

      
            GameObject spawnedItem = Instantiate(NetPrefab.Prefab, Position, rotation);
            NetworkObject networkObject = spawnedItem.GetComponent<NetworkObject>();
            
            if (networkObject != null)
            {
                networkObject.SpawnWithOwnership(clientId,true);  // Make sure the object is spawned and networked
            }
            else
            {
                Debug.LogError("Prefab does not contain a NetworkObject.");
            }
        
       
    }



    /// <summary>
    /// /////////////////////////////// Player Cube Spawning //////////////////////////////////////////
    /// </summary>
    /// <param name="Position"></param>
    /// <param name="clientId"></param>
    ///

    public void SpawnPlayerCubeServer(Vector3 Position, ulong clientId, bool IsRight, int AIPlayerNum = -1)   // this is so the client can call this here, instead of everything else needing to be a NetworkBehavior
    {
        SpawnPlayerCubeServerRpc(Position, clientId, IsRight, AIPlayerNum);
    }

    /// 

    [ServerRpc(RequireOwnership = false)]
    public void SpawnPlayerCubeServerRpc(Vector3 Position, ulong clientId, bool IsRight, int AIPlayerNum = -1)
    {
        DebugServerForClientID("SpawnPlayerCubeServerRpc for id " + clientId, clientId);
       
        bool isAI = AIPlayerNum > -1;                         // Determine if the player is AI or Local
        PlayerControllerNetwork localController =              // determine PlayerControllerNetwork
            isAI ? null : LocalPlayerEntities.Instance.GetPlayerObjects(clientId).PlayerController;
        
        AIPlayer aiController = isAI                            // Get the AIPlayer script
            ? FindObjectsOfType<AIPlayer>().FirstOrDefault(player => player.AIPlayerNumber == AIPlayerNum)
            : null;

        if (isAI && aiController == null) return;                       // If AI player not found, exit

        
        /////////////////////// Get ColorID for new Cube
        int NewColorID;        
        if (isAI)
        {
            NewColorID = aiController.CurrentColorID;
        }
        else
        {
            NewColorID = LocalPlayerEntities.Instance.GetPlayerObjects(clientId).PlayerController.ColorID;

        }
        
       
        /////////////////////// Get PlayerCubeData   for colorID, clientID, AIPlayerNum
        PlayerCubeData newCubeData = new PlayerCubeData
        {
            
            ColorID = NewColorID, AIPlayerNum  = AIPlayerNum, OwningPlayerId = clientId
        };

        var grid = isAI ? aiController.OwningDrawingGrid : LocalPlayerEntities.Instance.GetPlayerObjects(clientId).PlayerController.OwnedDrawingGrid;
        
        Quaternion rot = grid == null ? Quaternion.identity : grid.transform.rotation;

        
        /////////////////////// Get Prefab
        var selectedPrefab = BlockamiData.Instance.PlayerCubePrefab;
        var networkObject = m_cubePool.GetNetworkObject(selectedPrefab.gameObject, Position, rot);

        if (!networkObject.IsSpawned) networkObject.Spawn();

        /////////////////////// Intialize
        var go = networkObject.gameObject;
        var pcs = go.GetComponent<PlayerCubeScript>();
        pcs.IsAI.Value = isAI;
        pcs._NetPCData.Value = newCubeData;
        pcs.Initialize();
        m_AllPcs.Add(pcs);
        pcs.PCDied += OnPlayerCubeDied;

        /////////////////////// Try Get PlayerShot for this new cube
        var shotUlong = isAI ? aiController.CurrentPlayerShot.Value : localController.CurrentPlayerShot.Value;
        var netObj = NetworkManager.SpawnManager?.SpawnedObjects.GetValueOrDefault(shotUlong);
        var shot = netObj?.GetComponent<PlayerShotObject>();

        bool needsShotSpawned = false;
        if (shot)
        {
            needsShotSpawned = shot.AllPcs.Value.Count == 0 || shot.ColorID != newCubeData.ColorID;
        }
        else
        {
            needsShotSpawned = true;
        }

        
        if (needsShotSpawned)         // spawn a new shot
        {
            var pshot = NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(PlayerShotPrefab, clientId, false,
                false, false, Position, Quaternion.identity);
            shot = pshot ? pshot.GetComponent<PlayerShotObject>() : null;

            if (shot)
            {
               shot.SetLifeTime(10.0f);
                
                if (isAI)
                    aiController.CurrentPlayerShot.Value = pshot.NetworkObjectId;            // AIPlayer saves ulong of the Shot spawned for them
                else
                    localController.CurrentPlayerShot.Value = pshot.NetworkObjectId;         // PlayerControllerNetwork saves ulong of the Shot spawned for them

                shot.NetColorID.Value = newCubeData.ColorID;
                shot.AllPcs.Value.Add(pcs.NetworkObjectId);
                shot.IsRight.Value = IsRight;
                shot.IsSuccess.Value = false;
                shot.IsFailure.Value = false;
                shot.TotalScore.Value = 0.0f;

                pcs.OwningPlayerShot = shot; // Assign shot to player cube
            }
            else
            {
                Debug.LogError("Failed to spawn PlayerShotObject");
            }
        }
        else
        {
            pcs.OwningPlayerShot = shot;    // Assign shot to player cube
            shot?.AllPcs.Value.Add(pcs.NetworkObjectId); // If shot is valid, add cube to shot's list
        }

        
}
    
    
    private void OnPlayerCubeDied(PlayerCubeScript destroyedCube)
    {
        DeSpawnPlayerCube(destroyedCube);
    }
    
    public void DeSpawnPlayerCube(PlayerCubeScript PC)      
    {
        PC.PCDied -= OnPlayerCubeDied;
        
        _ = m_AllPcs.Remove(PC);
        if (PC.NetworkObject.IsSpawned) PC.NetworkObject.Despawn();
        
    }

    
    
    
    
    
   
    [ContextMenu("ResetAllHealthCubes")]
    public void ResetAllHealthCubes()
    {
        
        var cubesToDestroy = new List<SceneCubeNetworking>();
        
        List<DrawingGrid> AllDrawingGrids = FindObjectsOfType<DrawingGrid>().ToList();

        foreach (var dg in AllDrawingGrids)
        {
         
            HealthCubeTransform[] hcts = dg.GetComponentsInChildren<HealthCubeTransform>();

        
            for (int idx = 0; idx < hcts.Length; idx++)
            {
                var hct = hcts[idx]; 
                hct.HealthCubeIndex = idx;

                if (hct.HasHealthCube)
                {
                 //   hct.OwningHealthCube.KillSceneCubeServerRpc();
                 cubesToDestroy.Add(hct.OwningHealthCube);
                }
               
                int colorID = idx;
                
                //colorID = Mathf.CeilToInt(((float)idx)/2.0f);    // make color IDs between 0 and 2
               // colorID = Mathf.CeilToInt((  (float)idx)+1.0f/2.0f)  ;    // make color IDs between 0 and 2
                colorID = idx / 2;
                
                Transform spawnTransform = hct.transform;
                SpawnCubeServer(spawnTransform, colorID, true);
                
                hct._HealthPillar.ResetPillar();
                   
                
            }
        }
        
       // TriggerFrenzyTime(true);
        
        // Destroy after iteration
        foreach (var cube in cubesToDestroy)
        {
            cube.KillSceneCubeServerRpc();
        }
    
    }

    
    
    
    
    
    
    
    
    


    #region  Misc

    
    
    public void OnPhaseChanged(GameManager.GamePhase phase)
    {
        switch (phase)
        {
            case GameManager.GamePhase.PostGame: 
                StopSpawning();
                break;
            case GameManager.GamePhase.InGame:
                TimeThresholdCalled = false;
                StartSpawning();
                break;
        }
    }



    public void OnPhaseTimeUpdate(double timeLeft)
    {
        // nothing
    }
    
    public void OnPhaseTimeCounter(double timeCounter)
    {
        if (timeCounter >= 5.0f & !TimeThresholdCalled)
        {
            OnTimeThresholdPassed?.Invoke();
            TimeThresholdCalled = true;
        }
    }
   
    
    
    
    public void OnTeamColorUpdated(TeamColor teamColorA, TeamColor teamColorB)
    {
        // nothing
    }
    #endregion
    

}
