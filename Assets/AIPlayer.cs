using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blockami.Scripts;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class AIPlayer : NetworkBehaviour
{


    [Header("Settings")] public float SpawnAICubeDelay = 0.2f;
    public float timeEnoughToShoot = 1.0f;
    public int DrawLengthMax = 10;
    public float StartShotRate = 2.0f;
    public bool OnlyUseFirstLayer = true;
    public NetworkVariable<int> AIPlayerNum = new NetworkVariable<int>();
    public int AIPlayerNumber
    {
        get => AIPlayerNum.Value;
        set => AIPlayerNum.Value = value;
    }
  // public int AIPlayerNum;

    [Header("State")] 
    public bool AIPlayerIsActive;
    public int CurrentColorID;
    public bool ClearedToShoot = true;
    public bool DrawSingleRunning;
    public bool DrawNextRunning;
    public bool ShootingIsPaused;
    public int CubesDrawn;
    public bool ColorMatchFound = false;
    public bool timerOn;

    [Header("Internal")] 
    public BlockamiData BlockamiData;
    public DrawingGrid OwningDrawingGrid;
    public AIAvatarBlockami AIAvatar;
    public NetworkVariable<ulong> CurrentPlayerShot = new NetworkVariable<ulong>();
    public PlayerShotObject CurrentPlayerShotObject
    {
        get
        {
            ulong shotUlong = CurrentPlayerShot.Value;
            var netObj = NetworkManager.SpawnManager?.SpawnedObjects.GetValueOrDefault(shotUlong);  
            return netObj?.GetComponent<PlayerShotObject>();
        }
    }
    public NetworkVariable<bool> NetCurrentDrawingHandIsRight = new NetworkVariable<bool>();
    public NetworkVariable<Vector3> NetLastGoodDrawLocation = new NetworkVariable<Vector3>();
    public List<SnapZone> mySnapzones;
    public List<SnapZone> SelectedSnapzones = new List<SnapZone>(); // never used
    public List<PlayerCubeScript> currentlyDrawnChildCubes;
    SnapZone[,,] snapzoneMap;
    public SnapZone goodSnapzone;
    public SnapZone recentlyUsedSnapzone;
    public int recentlyUsedColorID;
    public float ShootTimer;
    RaycastHit hit;
    

    public UnityEvent AIShootEvent;

    
    
    
    
    
    
    

    private void Awake()
    {
        BlockamiData = Resources.Load<BlockamiData>("BlockamiData");
        
        Application.runInBackground = true;
    }

    
    
    private void OnEnable()
    {
        NetLastGoodDrawLocation.OnValueChanged += OnLastGoodDrawLocationChanged;
        AIAvatar = GetComponentInChildren<AIAvatarBlockami>();
    }
    
    
    



    
    
    
    private void OnDisable()
    {
        NetLastGoodDrawLocation.OnValueChanged -= OnLastGoodDrawLocationChanged; 
    }

    
    
    
    
    
    private void OnLastGoodDrawLocationChanged(Vector3 previousvalue, Vector3 newvalue)
    {
        if (NetCurrentDrawingHandIsRight.Value)
        {
            AIAvatar.FromNetRightHandPos = newvalue;
            AIAvatar.FromNetLeftHandPos = Vector3.zero;
        }
        else
        {
            AIAvatar.FromNetLeftHandPos = newvalue;
            AIAvatar.FromNetRightHandPos = Vector3.zero;
        }

        AIAvatar.FromNetCurrentDrawingHandIsRight = NetCurrentDrawingHandIsRight.Value;
    }
    
    
    
    
    
    void Start() // start
    {
        if ((NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost)) return;

        currentlyDrawnChildCubes = new List<PlayerCubeScript>();
        mySnapzones = OwningDrawingGrid.AllSnapZones; // get all snapzones from owning drawing grid
        AIPlayerNum.Value = OwningDrawingGrid.DrawingGridIndex;
        
        //  myAvatar = GetComponentInChildren<AIAvatarScript>();

        //ShootingIsPaused = true;                                         // start paused


        snapzoneMap =
            new SnapZone[6, 5,
                3]; // Correct grid size       - // create 3 layer array of SnapZones  from parsing AllSnapzones

        foreach (SnapZone item in mySnapzones)
        {
            int x = Mathf.RoundToInt(item.Coords.x); // rounding if necessary
            int y = Mathf.RoundToInt(item.Coords.y);
            int z = Mathf.RoundToInt(item.Coords.z);

            if (z != 0 && OnlyUseFirstLayer) continue; // dont bother with 2nd and 3rd layer for now

            // Ensure the coordinates are within bounds of the grid
            if (x >= 0 && x < 6 && y >= 0 && y < 5 && z >= 0 && z < 3)
            {
                snapzoneMap[x, y, z] = item; // Correctly map the SnapZone
            }
        }
        
        CancelInvoke("NewShot");
        InvokeRepeating("NewShot",0.0f,StartShotRate); // start NewShot every 2 seconds
 
    }





    void  NewShot() // do NewShot continously
    {
    StartCoroutine("DrawSingleRandom");
    }




    public SnapZone GetRandomSnapZone() // GetRandomSnapZone
    {
        var s = OnlyUseFirstLayer
            ? snapzoneMap[UnityEngine.Random.Range(0, 6), UnityEngine.Random.Range(0, 5), 0]    // max is exclusive for some dumb reason
            : mySnapzones[UnityEngine.Random.Range(0, mySnapzones.Count)];

        if (s == null)
        {
            Debug.Log("Random Snapzone was null");
            return null;
        }

        return s;
    }







    public IEnumerator DrawSingleRandom() // start every 2 seconds
    {

    
        
        if (AIPlayerIsActive == false || ShootingIsPaused) yield break; // if Ai Is not active or is paused stop

        
        if (CurrentPlayerShotObject)
        {
            ShootShot();
            //CurrentPlayerShot = new NetworkVariable<ulong>();
            yield break;
        }
    




        DrawSingleRunning = true;
        timerOn = true; // bool to accumulate time in Update
        currentlyDrawnChildCubes = new List<PlayerCubeScript>();
        
        while (!ColorMatchFound) // start while loop searching for a first origin
            // while (true) // start while loop searching for a first origin
        {
            
            
            SceneCubeNetworking sCs = null;

            SnapZone s = GetRandomSnapZone();
            if (s == null) yield return new WaitForEndOfFrame();


            sCs = FindSceneCubeHit(s);                                      // find a scene cube infront of the testing Snapzone

            if (sCs != null)
            {
                goodSnapzone = s;                                               //declares this snapzone spawnable by notion of it will hit something

                if (TestForColorMatch(sCs))                                                  //	TestForColorMatch will set be true if its good
                {
                   // NetCurrentDrawingHandIsRight.Value = Vector3.Dot(goodSnapzone.transform.position - transform.position, transform.right) > 0;
                   NetCurrentDrawingHandIsRight.Value = Random.value > 0.5f;   // randomly do left or right


                    recentlyUsedSnapzone = goodSnapzone;
                    recentlyUsedColorID = CurrentColorID;
                 //   SpawnAIPlayerCube();                                                    // trigger a PlayerCubeSpawn after a delay    // delayed 0.2 seconds
                 SetCubeToMoveTo(recentlyUsedSnapzone.gameObject);
                 Invoke("SpawnAIPlayerCube", SpawnAICubeDelay);                                                                      // trigger a PlayerCubeSpawn after a delay    // delayed 0.2 seconds
                    StartCoroutine("DrawNextSingle");                                                  // start routine for further cube finding                // delays 0.3 before starting
                   // DrawNextSingle();
                    ColorMatchFound = true;                                                               
                    DrawSingleRunning = false;
                    yield break;
                }
            }


            ColorMatchFound = false;
            CurrentColorID =
                UnityEngine.Random.Range(0, 6);             // Change AI color for next find attempt,   only when no color match for this random snapzone, which is often
            
            // if (CurrentColorID == 6) CurrentColorID = 12;
            CancelInvoke("NewShot");
            InvokeRepeating("NewShot",0.01f,StartShotRate);
            yield break;
            
        }


        DrawSingleRunning = false;
    }



    public IEnumerator DrawNextSingle()
    {
        DrawNextRunning = true;
        
        yield return new WaitForSeconds(0.3f);              // always wait at least 0.3 before finding new cube draw
        
        if (recentlyUsedSnapzone == null){ Debug.Log("recently used snapzone was null, not saved from first draw function"); yield break; }
        
        SnapZone test = null;

        for (int i = 0; i < 4; i++)
        {
            Vector3 coords = recentlyUsedSnapzone.Coords;

            switch (i)
            {
                case 0: // Right (increase x)
                    coords.x += 1;
                    break;
                case 1: // Left (decrease x)
                    coords.x -= 1;
                    break;
                case 2: // Up (increase y)
                    coords.y += 1;
                    break;
                case 3: // Down (decrease y)
                    coords.y -= 1;
                    break;
            }


            // Clamp the coords within bounds of the snapzoneMap
            coords.x = Mathf.Clamp((int)coords.x, 0, snapzoneMap.GetLength(0) - 1);
            coords.y = Mathf.Clamp((int)coords.y, 0, snapzoneMap.GetLength(1) - 1);
            coords.z = Mathf.Clamp((int)coords.z, 0, snapzoneMap.GetLength(2) - 1);

            // Now check if the snapzone at the clamped coordinates is not null
            if (snapzoneMap[(int)coords.x, (int)coords.y, (int)coords.z] != null)
            {
                test = snapzoneMap[(int)coords.x, (int)coords.y, (int)coords.z];
                if (test == recentlyUsedSnapzone) continue;

            }
            else
            {
                continue;
            }

            if (test.HasCurrentlySpawnedCube == true)
            {
                Debug.Log("alreadyhasSceneCube"); // continuing testing directions
                continue; // snapzone has already spawned}
            }


            /// TEST SNAPZONE ACQUIRED
            
            SceneCubeNetworking sCs = null;
            sCs = FindSceneCubeHit(test);

            if (sCs == null)
            {
//                Debug.Log("no scene cube found by cast ray"); // continuing testing directions
                continue;
            }
            else
            {
                if (TestForColorMatch(sCs))
                {
                    recentlyUsedSnapzone = test; // update last Origin
                    recentlyUsedColorID = CurrentColorID;

                    SetCubeToMoveTo(recentlyUsedSnapzone.gameObject);
                    SpawnAIPlayerCube();
                    //    Invoke("SpawnAIPlayerCube",SpawnAICubeDelay);                 // spawn after 0.2,  and find next after 0.3
                    //  Debug.Log("Succesfully draw 2nd Cube");
                    
                    StartCoroutine("DrawNextSingle"); // if a match was found, do this coroutine again with new origin        // break this coroutine
                    yield break;

                }
                else
                {
                    // Debug.Log("tested a spot but didnt find color match, continuing test others");                           // continuing testing directions
                    continue;
                }
            }
            // END OF FOR LOOP                   
        }

//        Debug.Log("Tested all 4 adjecents and found no hits and matches"); //Tested all 4 adjecents and found no hits and matches

        yield return
            new WaitUntil(() =>ClearedToShoot ==  true); // we end up here when theres no further matches, wait til cleared to shoot via Update() and fire

        ShootShot();
    }

    private void ShootShot()
    {
        foreach (SnapZone item in mySnapzones)
        {
            item.HasCurrentlySpawnedCube = false;
        }
        ColorMatchFound = false;
        AIShootEvent.Invoke();
        ColorMatchFound = false;
        ShootTimer = 0f;
        CubesDrawn = 0;
        DrawNextRunning = false;
        if (CurrentPlayerShotObject) CurrentPlayerShotObject.FireShotServerRpc();         // take the shot
        CurrentPlayerShot = new NetworkVariable<ulong>();
        
    }


    public SceneCubeNetworking FindSceneCubeHit(SnapZone testingSnapZone)           // find a scene cube infront of the testing Snapzone
    {
        if (AIPlayerIsActive)
        {
            int sceneCubeLayer = LayerMask.NameToLayer("Hitable");
            int sceneCubeMask = 1 << sceneCubeLayer;
            
         if (Physics.Raycast(testingSnapZone.transform.position, -testingSnapZone.transform.forward, out hit, 70f, sceneCubeMask))
            {
             //   Debug.DrawRay(testingSnapZone.transform.position, -testingSnapZone.transform.forward * hit.distance, BlockamiData.m_ColorTypes[CurrentColorID].color,0.7f,false);

                SceneCubeNetworking s = hit.collider.gameObject.GetComponent<SceneCubeNetworking>();
                if (s == null)
                {
                  //  Debug.Log("LOOK, hit something but not scenecube, it was " + hit.collider.gameObject.name);
                    s = hit.collider.gameObject.transform.parent.gameObject.GetComponent<SceneCubeNetworking>();
                    if (s == null)
                    {
                        Debug.Log("LOOK, hit something but not scenecube, it was " + hit.collider.gameObject.name);
                        return null;
                    }
                }


                if (s.IsHealthCube)
                {
                    // Debug.Log("Ai Ray hit HEALTHCUBE with Color "  + s.ColorID);

                }
                else
                {
                    //  Debug.Log("Ai Ray hit SCENEcube with Color " + s.ColorID);
                }
                
                return s;
            }
            else
            {
           //     Debug.DrawRay(testingSnapZone.transform.position, -testingSnapZone.transform.forward * 10.0f, BlockamiData.m_ColorTypes[CurrentColorID].color,1.0f,false);

                // Debug.Log("AI Ray Hit nothing");

                return null;

            }
        }
        return null;



    }

    public bool TestForColorMatch(SceneCubeNetworking s)
    {
        if (s.ColorID == CurrentColorID)
        {
            ColorMatchFound = true; // set tracker bool to true  // doesnt this break the while loop???
            return true;
        }

        else if (s.ColorID != CurrentColorID)
        {
            return false;
        }
		
        return false;   
    }


    public void SetCubeToMoveTo(GameObject cube)
    {
        NetLastGoodDrawLocation.Value = cube.transform.position;
    }
    
	public void SpawnAIPlayerCube()
    {
        if (ShootingIsPaused) return;
       // if (!IsServer) return;

        ulong HostClientID = NetworkManager.Singleton.LocalClientId;
        Vector3 pos = recentlyUsedSnapzone.transform.position;
        recentlyUsedSnapzone.HasCurrentlySpawnedCube = true;
     
        SpawnManager.Instance.SpawnPlayerCubeServerRpc(pos,HostClientID,false, AIPlayerNumber);
        
        // if (!PhotonNetwork.isMasterClient && !PhotonNetwork.isNonMasterClientInRoom) return;
        //
        // if (PhotonNetwork.isMasterClient || PhotonNetwork.isNonMasterClientInRoom)
        // {
        //     recentlyUsedSnapzone.photonView.RPC("SpawnChildCubeAI", PhotonTargets.MasterClient, AIPlayerNumber);
        // }
        //
        CubesDrawn++;

    }

    public void PauseShootingForSeconds(float seconds, bool TurnOffAfterPausing)     // why turn off after pausing??
    {
        ShootingIsPaused = true;
        
        // myAvatar.ikActive = false;
        // myAvatar.OnAnimatorIK();

        if (TurnOffAfterPausing)
        {
            Invoke("TurnOffInstead", seconds + 1f);
        }
        else
        {
            Invoke("UnpauseShooting", seconds);
        }
    }

    public void UnpauseShooting()
    {
       //
       //  myAvatar.ShouldLerpIKOn = true;
       // // myAvatar.ikActive = true;
       //  myAvatar.OnAnimatorIK();
        ShootingIsPaused = false;
    }

    public void TurnOffInstead()
    {
        //   Debug.Log("SHOTTING UNPAUSED");
        AIPlayerIsActive = false;
        // // myAvatar.ikActive = true;
        // myAvatar.ShouldLerpIKOn = true;
        // ShootingIsPaused = true;
        // myAvatar.OnAnimatorIK();
    }




    void Update()
    {

        if (timerOn) { ShootTimer += Time.deltaTime; }


        if (ShootTimer > timeEnoughToShoot)
        {
            ClearedToShoot =
                true; // cleared to shoot according to ShootRate  // whenever cleared to shoot is true,  and looping Cubefinding algo will shoot its shot
        }
        else
        {
            ClearedToShoot = false;
        }



        if (CurrentColorID >= 6 && CurrentColorID < 10) // dont know what this did
        {
            CurrentColorID = 0;
        }

        // if (AIisActive)                             // upkeep vars
        // {
        //     myRegion.HasBotActive = true;
        //     myAvatar.gameObject.SetActive(true);
        //
        // }
        // else
        // {
        //     myRegion.HasBotActive = false;
        //     myAvatar.gameObject.SetActive(false);
        // }

    }

    
}
