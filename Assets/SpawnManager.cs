using System;
using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blockami.Scripts;
using Meta.Utilities;
using UltimateGloveBall.Arena.Player;
using UltimateGloveBall.Arena.Services;
using UltimateGloveBall.Arena.VFX;
using UltimateGloveBall.Networking.Pooling;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class SpawnManager : NetworkBehaviour    // used to be a NetworkBehavior but doesnt need it??
{
    // Singleton instance
    public static SpawnManager Instance { get; private set; }
    
    
    [Header("Editor")]
    [SerializeField] public bool BeginSpawnOnStart = true; 
    
    
    [Header("Spawn Settings")]
    [SerializeField] public BlockamiData BlockamiData;                      // For tweaking special cube spawn rates
    [SerializeField] public int maxSceneCubes = 50;
    [SerializeField] public NetworkObjectPool m_cubePool;

    [Header("State")]
    [SerializeField] public bool isPaused = false;
    [SerializeField] public bool isSpawning = false;
    [SerializeField] public bool isFrenzyTime = false;
    [SerializeField] public int currentSceneCubeCount = 0;
    [SerializeField] public float currentSpawnRate;
    //[SerializeField] private Object sceneCubePrefab;

   // [Header("Actions")] public Action OnSCSDied;
    [Header("Actions")] 
    public Action<ulong> OnSCSDied;
    
    [Header("World")] 
    [SerializeField] private SpawnZone[] allSpawnZones;

    [Header("Internal")] 
    public readonly List < SceneCubeNetworking> m_AllScs  = new();
    public readonly List < PlayerCubeScript> m_AllPcs  = new();
    public NetworkObject sceneCubePrefab;
    public NetworkObject PlayerShotPrefab;
    public NetworkObject HealthCubePrefab;
    [SerializeField] private NetworkObject HealthCubePickupPrefab;
    private Coroutine SpawnTimerHandle;
    private Coroutine FrenzyTimerHandle;


    private void Awake()
    {
      
        
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple SpawnManager instances detected. Destroying the duplicate.");
            Destroy(gameObject); // Destroy the duplicate instance
            return;
        }
        Instance = this;

        
        allSpawnZones = FindObjectsOfType<SpawnZone>();
        currentSpawnRate = BlockamiData.DefaultSpawnRate;
        
    }
    
    
    
    // override void  OnDestroy()                    // Optional: Clean up when this instance is destroyed
    // {
    //   //  base.OnDestroy();
    //     
    //     if (Instance == this)
    //     {
    //         Instance = null;
    //     }
    // }
    
   
    
    private void Start()
    {
        
       
        
        if(BeginSpawnOnStart) StartSpawning();
    }

                      
 
    

public void StartSpawning()
{
    if (SpawnTimerHandle != null)
    {
        StopCoroutine(SpawnTimerHandle);
    }
    
    if (!isPaused && !isSpawning) 
    {
        if (NetworkManager.Singleton == null || IsServer)
        {

            isSpawning = true;
            SpawnTimerHandle = StartCoroutine(SpawnSceneCubes());
        }
    }
}




private IEnumerator SpawnSceneCubes()        // repeat state
{
    while (true)
    {
        if (isPaused || currentSceneCubeCount > BlockamiData.MaxCubes)
        {
            yield break;
        }

        float spawnRate = isFrenzyTime ? BlockamiData.FrenzySpawnRate : BlockamiData.DefaultSpawnRate;
        
        yield return new WaitForSeconds(1f / spawnRate);

      
        SpawnCubeServer();          // Spawn a random cube

      
    }
}

                    
                    private void SpawnCubeServer(Transform OverrideTransform = null, int OverrideColorID = -1, bool IsHealthCube = false)
                    {


                        if (!IsHealthCube && currentSceneCubeCount >= maxSceneCubes)
                        {
                            Debug.Log("HITTING MAX SCENE CUBES");
                            return;
                            
                        }
                        
                        ////////////////////////////// Get Position  
                        var spawnPosition = GetRandomSpawnPosition();        // Spawn SceneCube at a random spawn zone in the sky
                        if (OverrideTransform != null)
                        {
                            spawnPosition = OverrideTransform.position;     // health cubes specify location explicitly
                        }
                        
                        
                        
                        ////////////////////////////// Get Color ID
                            int colorID;
                            colorID = BlockamiData.GetRandomColorID();

                           // bool HasHealthPickup = Random.Range(0.0f,1.0f) > 0.92f;
                           bool HasHealthPickup = false;
                            
                          //  bool IsRainbow = Random.Range(0.0f,1.0f) > 0.92f;
                          bool IsRainbow = false;
                            if (IsRainbow)
                            {
                                colorID = 10;
                            }
                            
                           // bool IsEmoji = Random.Range(0.0f,1.0f) > 0.92f;;
                            bool IsEmoji = false;
                            if (IsEmoji)
                            {
                                colorID = 11;
                            }
                            
                            // decide if also has HealthPickup
                            // decide if is Rainbow
                            // decide if Emoji
                                // mutate ColorID if so
                                
                            if (OverrideColorID > -1)
                            {
                                colorID = OverrideColorID;        // health cubes specify their colorID explicitly
                            }
                            
                            ////////////////////////////// Get Prefab accounting for Exotic Type
                            SceneCubeData scd = BlockamiData.GetSceneCubeDataFromID(colorID);            // ATM the only reason for SceneCubeData is for clients to retrieve Color from ID
                            var selectedPrefab = scd.SceneCubePrefab;           // retrieve a prefab associated with colorIds SceneCubeData type
                            
                            if (IsHealthCube) selectedPrefab = HealthCubePrefab;   // healthcube prefab is not determined By ID
                            if (HasHealthPickup) selectedPrefab = HealthCubePickupPrefab;  // healthcubepickup prefab is not determined By ID
                            
                            
                            
                            ////////////////////////////// SPAWN
                            
                            var networkObject = m_cubePool.GetNetworkObject(selectedPrefab.gameObject, spawnPosition, Quaternion.identity);
                            if (!networkObject)
                            {
                                Debug.Log("no network object for prefab in spawn manager ");
                            }
                            
                            if (!networkObject.IsSpawned)       // Spawning the ball is only required the first time it is fetched from the pool.
                                networkObject.Spawn(); 
                            
                            
                            ////////////////////////////// INITIALIZE 
                            var go = networkObject.gameObject;
                            SceneCubeNetworking scs = go.GetComponent<SceneCubeNetworking>();
                            scs.NetIsHealthCube.Value = IsHealthCube;
                            scs.NetColorID.Value = colorID;                          // assign the ID so clients can initialize     
                            // for exotic cubes, any that have unique prefabs, can initialize variables according to prefab defaults instead of replicated vars   eg. HealthCubes
                            // exotic cubes with unique ColorIDs can still initialize themselves from that alone
                            scs.Initialize();

                            if (!IsHealthCube)
                            {
                                currentSceneCubeCount++;
                                m_AllScs.Add(scs);
                            }

                            scs.SCDied += OnSceneCubeDied;
                        
             
                        
                    }

                  

   
    
    
    
    private void OnSceneCubeDied(SceneCubeNetworking destroyedCube)
    {
        if (destroyedCube.IsHealthCube)
        {
            TriggerFrenzyTime();
        }
        DeSpawnCube(destroyedCube);
        OnSCSDied?.Invoke(destroyedCube.NetworkObjectId);
    }
    
    private void OnHealthCubeDied(SceneCubeNetworking destroyedCube)
    {
       
    }
    
    public void DeSpawnCube(SceneCubeNetworking Sc)      
    {
        
        PlayHitVFXClientRpc(Sc.transform.position,Sc.transform.forward);
        
        Sc.SCDied -= OnSceneCubeDied;
        currentSceneCubeCount--;
        _ = m_AllScs.Remove(Sc);
        if (Sc.NetworkObject.IsSpawned) Sc.NetworkObject.Despawn();
        
    }

 
    
    
    
    
    public void ClearAllCubes()
    {
        if (!IsServer) return;
        
        foreach (var cube in m_AllScs.ToList())  // return ball calls NetworkManager.Despawn   and we have a pool manager which intercepts that and returns to a pool
        {
            DeSpawnCube(cube);
        }
        
       
    }
    
    
     
    public void PauseSpawning()
    {
        isPaused = true;
        StopAllCoroutines();
        isSpawning = false;
    }
    
    
    
    public void ResumeSpawning()
    {
        isPaused = false;
        StartSpawning();
    }
    
    
    

    
    
    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    public void PlayHitVFXClientRpc(Vector3 position, Vector3 rotation)
    {
        VFXManager.Instance.PlayHitVFX(position,rotation);
    }

    
    
    
    [ContextMenu("TriggerFrenzyTime")]
    public void TriggerFrenzyTime()
    {
        isFrenzyTime = true;
        
        // retriggerable delay
        {
            CancelInvoke("TriggerFrenzyTime");
            Invoke("TriggerFrenzyTime", BlockamiData.FrenzyTimeDuration);
        }
        
    }
    
    
    
    private Vector3 GetRandomSpawnPosition()
    {
        SpawnZone randomSpawnZone = allSpawnZones[Random.Range(0, allSpawnZones.Length)];
        return randomSpawnZone.transform.position;
    }

   

  
   




    /// <summary>
    /// /////////////////////////////// Player Cube Spawning //////////////////////////////////////////
    /// </summary>
    /// <param name="Position"></param>
    /// <param name="clientId"></param>

    [ServerRpc(RequireOwnership = false)]
    public void SpawnPlayerCubeServerRpc(Vector3 Position, ulong clientId, bool IsRight, int AIPlayerNum = -1)
    {

       
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

        /////////////////////// Get Prefab
        var selectedPrefab = BlockamiData.PlayerCubePrefab;
        var networkObject = m_cubePool.GetNetworkObject(selectedPrefab.gameObject, Position, Quaternion.identity);

        if (!networkObject.IsSpawned) networkObject.Spawn();

        /////////////////////// Intialize
        var go = networkObject.gameObject;
        var pcs = go.GetComponent<PlayerCubeScript>();
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

    
    
    
    
    
    public void ResetAllPlayerHealthCubes()
    {
        
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
                    hct.OwningHealthCube.KillSceneCubeServerRpc();
                }
               
                int colorID = idx;
                Transform spawnTransform = hct.transform;
                SpawnCubeServer(spawnTransform, colorID, true);
                   
                
            }
        }
        
        TriggerFrenzyTime();
    }


    

}
