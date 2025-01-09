using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blockami.Scripts;
using Meta.Utilities;
using UltimateGloveBall.Arena.Player;
using UltimateGloveBall.Arena.Services;
using UltimateGloveBall.Networking.Pooling;
using UnityEngine.Serialization;

public class SpawnManager : NetworkBehaviour    // used to be a NetworkBehavior but doesnt need it??
{
    // Singleton instance
    public static SpawnManager Instance { get; private set; }
    
    
    [Header("Editor")]
    [SerializeField] public bool BeginSpawnOnStart = true; 
    
    [FormerlySerializedAs("spawnRateSettings")]
    [Header("Spawn Settings")]
    [SerializeField] public BlockamiData BlockamiData;                      // For tweaking special cube spawn rates
    [SerializeField] public float defaultSpawnRate = 1f;
    [SerializeField] public float frenzySpawnRate = 0.2f;
    [SerializeField] public int maxSceneCubes = 50;
    [SerializeField] private NetworkObjectPool m_cubePool;

    [Header("State")]
    [SerializeField] public bool isPaused = false;
    [SerializeField] private bool isSpawning = false;
    [SerializeField] private int currentSceneCubeCount = 0;
    [SerializeField] private float currentSpawnRate;
    //[SerializeField] private Object sceneCubePrefab;
    

    [Header("World")] 
    [SerializeField] private SpawnZone[] allSpawnZones;

    [Header("Interal")] 
    private readonly List < SceneCubeNetworking> m_AllScs  = new();
    private readonly List < PlayerCubeScript> m_AllPcs  = new();
    private NetworkObject sceneCubePrefab;
    public NetworkObject PlayerShotPrefab;
    
    
    //public static SpawnManager Instance { get; private set; }
    
    
    
    
    
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple SpawnManager instances detected. Destroying the duplicate.");
            Destroy(gameObject); // Destroy the duplicate instance
            return;
        }
        Instance = this;

    }
    private void OnDestroy()                    // Optional: Clean up when this instance is destroyed
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    
    
    
    private void Start()
    {
        InitFromBlockamiData();
        
        allSpawnZones = FindObjectsOfType<SpawnZone>();
        
        currentSpawnRate = defaultSpawnRate;
        
        if(BeginSpawnOnStart) StartSpawning();
    }

                      
    
    private void InitFromBlockamiData()                         // Initialize variables from ScriptableObject
    {
        if (BlockamiData == null) { Debug.LogError("SpawnRateSettings ScriptableObject is not assigned!"); return; }

     
        defaultSpawnRate = BlockamiData.DefaultSpawnRate;        
        frenzySpawnRate = BlockamiData.FrenzySpawnRate;
        maxSceneCubes = BlockamiData.MaxCubes;
    }
    

public void StartSpawning()
{
  
    
    if (!isPaused && !isSpawning) 
    {
        if (NetworkManager.Singleton == null || IsServer)
        {

            isSpawning = true;
            StartCoroutine(SpawnSceneCubes());
        }
    }
}




private IEnumerator SpawnSceneCubes()        // repeat state
{
    while (currentSceneCubeCount < maxSceneCubes)
    {
        if (isPaused) yield break;

        float spawnRate = isFrenzyTime() ? frenzySpawnRate : defaultSpawnRate;
        
        yield return new WaitForSeconds(1f / spawnRate);

      
        SpawnCubeServer();          // Spawn a random cube

        currentSceneCubeCount++;
    }
}

                    
                    private void SpawnCubeServer()
                    {
                    //    if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient) return;    clients shouldnt spawn scene cubes
                        
                        if (currentSceneCubeCount >= maxSceneCubes) return;
                        
                            SceneCubeData newCubeData = new SceneCubeData
                            {
                               
                                MyColorType = BlockamiData.GetRandomColor(),            // Set attributes based on your logic, such as random colors, types, pickups, etc.
                                ContainsPickup = Random.value > 0.5f                 //  CubeType = (CubeType)Random.Range(0, 3), // For example
                            };
                            
                            var spawnPosition = GetRandomSpawnPosition();        // Spawn SceneCube at a random spawn zone in the sky
                            
                            // Get object from pool before spawning
                            var selectedPrefab = BlockamiData.GetRandomCube(); 
                            var networkObject = m_cubePool.GetNetworkObject(selectedPrefab.gameObject, spawnPosition, Quaternion.identity);
                            if (!networkObject.IsSpawned)       // Spawning the ball is only required the first time it is fetched from the pool.
                                networkObject.Spawn(); 
                            
                            var go = networkObject.gameObject;
                            SceneCubeNetworking scs = go.GetComponent<SceneCubeNetworking>();
                            scs.SCData.Value = newCubeData;
                            scs.Initialize();
                            m_AllScs.Add(scs);
                            scs.SCDied += OnSceneCubeDied;
                            

                            
                            UpdateSceneCubeData(newCubeData);                 // Update the data struct for tracking   ????????
                        
                    }
    
    private void UpdateSceneCubeData(SceneCubeData data)                     // Handle the logic of updating the struct with new cube data
    {
     
    }
    
    
    
    private void OnSceneCubeDied(SceneCubeNetworking destroyedCube)
    {
        DeSpawnCube(destroyedCube);
    }
    
    public void DeSpawnCube(SceneCubeNetworking Sc)      
    {
        Sc.SCDied -= OnSceneCubeDied;
        currentSceneCubeCount--;
        _ = m_AllScs.Remove(Sc);
        if (Sc.NetworkObject.IsSpawned) Sc.NetworkObject.Despawn();
        
    }
    
    public void ClearAllCubes()
    {
        if (!IsServer) return;

        foreach (var cube in m_AllScs)
        {
            DeSpawnCube(cube);                      // return ball calls NetworkManager.Despawn   and we have a pool manager which intercepts that and returns to a pool
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        SpawnZone randomSpawnZone = allSpawnZones[Random.Range(0, allSpawnZones.Length)];
        return randomSpawnZone.transform.position;
    }

    private bool isFrenzyTime()
    {
        return false;                     // Add your logic for when frenzy time is triggered (e.g., score-based or timer-based)
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




    /// <summary>
    /// /////////////////////////////// Player Cube Spawning //////////////////////////////////////////
    /// </summary>
    /// <param name="Position"></param>
    /// <param name="clientId"></param>

    [ServerRpc(RequireOwnership = false)]
    public void SpawnPlayerCubeServerRpc(Vector3 Position, ulong clientId, bool IsRight, int AIPlayerNum = -1)
    {

        // Determine if the player is AI or Local
        bool isAI = AIPlayerNum > -1;
        PlayerControllerNetwork localController =
            isAI ? null : LocalPlayerEntities.Instance.GetPlayerObjects(clientId).PlayerController;
        AIPlayer aiController = isAI
            ? FindObjectsOfType<AIPlayer>().FirstOrDefault(player => player.AIPlayerNumber == AIPlayerNum)
            : null;

        if (isAI && aiController == null) return; // If AI player not found, exit

        // Set the cube's data based on player type (AI or Local)
        PlayerCubeData newCubeData = new PlayerCubeData
        {
            MyColorType = isAI
                ? BlockamiData.m_ColorTypes[aiController.CurrentColorID]
                : LocalPlayerEntities.Instance?.GetPlayerObjects(clientId)?.PlayerController?.m_ColorType?.Value ??
                  BlockamiData.m_ColorTypes[Random.Range(0, BlockamiData.m_ColorTypes.Count)],
            AIPlayerNum  = AIPlayerNum, OwningPlayerId = clientId
        };

        // Get or spawn the PlayerCube
        var selectedPrefab = BlockamiData.PlayerCubePrefab;
        var networkObject = m_cubePool.GetNetworkObject(selectedPrefab.gameObject, Position, Quaternion.identity);

        if (!networkObject.IsSpawned) networkObject.Spawn();

        var go = networkObject.gameObject;
        var pcs = go.GetComponent<PlayerCubeScript>();
        pcs.PCData.Value = newCubeData;
        pcs.Initialize();
        m_AllPcs.Add(pcs);
        pcs.PCDied += OnPlayerCubeDied;

        // Determine the current shot object
        var shotUlong = isAI ? aiController.CurrentPlayerShot.Value : localController.CurrentPlayerShot.Value;
        var netObj = NetworkManager.SpawnManager?.SpawnedObjects.GetValueOrDefault(shotUlong);
        var shot = netObj?.GetComponent<PlayerShotObject>();

        bool needsShotSpawned = false;
        if (shot)
        {
            needsShotSpawned = shot.AllPcs.Value.Count == 0 || shot.MyColorType != newCubeData.MyColorType;
        }
        else
        {
            needsShotSpawned = true;
        }

        // Handle shot spawning if needed
        if (needsShotSpawned)
        {
            var pshot = NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(PlayerShotPrefab, clientId, false,
                false, false, Position, Quaternion.identity);
            shot = pshot ? pshot.GetComponent<PlayerShotObject>() : null;

            if (shot)
            {
                // Update the shot reference for AI or local player
                if (isAI)
                    aiController.CurrentPlayerShot.Value = pshot.NetworkObjectId;
                else
                    localController.CurrentPlayerShot.Value = pshot.NetworkObjectId;

                shot.MyColorType = newCubeData.MyColorType;
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

        // if (pcs.OwningPlayerShot == null)
        // {
        //     Debug.LogError("Shot is null");
        //
        // }


}
    
    
    private void OnPlayerCubeDied(PlayerCubeScript destroyedCube)
    {
        DeSpawnPlayerCube(destroyedCube);
    }
    
    public void DeSpawnPlayerCube(PlayerCubeScript PC)      
    {
        PC.PCDied -= OnPlayerCubeDied;
        currentSceneCubeCount--;
        _ = m_AllPcs.Remove(PC);
        if (PC.NetworkObject.IsSpawned) PC.NetworkObject.Despawn();
        
    }
}
