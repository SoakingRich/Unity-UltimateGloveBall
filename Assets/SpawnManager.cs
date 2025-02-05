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
    private static SpawnManager _instance;

    public static SpawnManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing instance in the scene
                _instance = FindObjectOfType<SpawnManager>();

                // If none exists, create a new GameObject with SpawnManager attached
                if (_instance == null)
                {
                    GameObject obj = new GameObject("SpawnManager");
                    _instance = obj.AddComponent<SpawnManager>();
                }
            }
            return _instance;
        }
    }
    
    
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
        

        
        allSpawnZones = FindObjectsOfType<SpawnZone>();
        currentSpawnRate = BlockamiData.DefaultSpawnRate;
        
    }
    

    
   
    
    private void Start()
    {
        if(BeginSpawnOnStart) StartSpawning();
    }

    
    [ContextMenu("StartSpawning")]
public void StartSpawning()
{
    if (SpawnTimerHandle != null)
    {
        StopCoroutine(SpawnTimerHandle);
    }
    
    if (!isPaused && !isSpawning) 
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClientId == 0)
        {

            isSpawning = true;
            SpawnTimerHandle = StartCoroutine(SpawnSceneCubes());
        }
    }
}




private IEnumerator SpawnSceneCubes()
{
    while (true)
    {
        if (isPaused || currentSceneCubeCount > BlockamiData.MaxCubes)
        {
            yield break;
        }

        float spawnRate = isFrenzyTime ? BlockamiData.FrenzySpawnRate : BlockamiData.DefaultSpawnRate;

        // Wait before spawning to control the spawn rate
        yield return new WaitForSeconds(1f / spawnRate);

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

                  

   
    
    
    
    private void OnSceneCubeDied(SceneCubeNetworking destroyedCube)     // KillSceneCubeServerRpc
    {
        if (destroyedCube.IsHealthCube)
        {
            OnHealthCubeDied(destroyedCube);
           
        }
        DeSpawnCube(destroyedCube);
        OnSCSDied?.Invoke(destroyedCube.NetworkObjectId);
    }
    
    private void OnHealthCubeDied(SceneCubeNetworking destroyedCube)
    {
        //TriggerFrenzyTime();
    }
    
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


 
    public void RequestSpawnItemServer(string prefabName, Vector3 Position, Quaternion rotation, ulong clientId)
    {
        RequestSpawnItemServerRpc(prefabName, Position, rotation, clientId);
    }



    [ServerRpc(RequireOwnership = false)]
    public void RequestSpawnItemServerRpc(string prefabName, Vector3 Position, Quaternion rotation, ulong clientId)
    {
        // Ensure the server is the one handling the spawn
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

    public void SpawnPlayerCubeServer(Vector3 Position, ulong clientId, bool IsRight, int AIPlayerNum = -1)
    {
        SpawnPlayerCubeServerRpc(Position, clientId, IsRight, AIPlayerNum);
    }

    /// 

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

        var grid = isAI ? aiController.OwningDrawingGrid : LocalPlayerEntities.Instance.GetPlayerObjects(clientId).PlayerController.OwnedDrawingGrid;
        
        Quaternion rot = grid == null ? Quaternion.identity : grid.transform.rotation;

        
        /////////////////////// Get Prefab
        var selectedPrefab = BlockamiData.PlayerCubePrefab;
        var networkObject = m_cubePool.GetNetworkObject(selectedPrefab.gameObject, Position, rot);

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
