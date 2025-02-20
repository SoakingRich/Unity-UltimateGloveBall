using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Blockami.Scripts;
using Meta.Utilities;
using Oculus.Avatar2;
using UltimateGloveBall.App;
using UltimateGloveBall.Arena.Services;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using Quaternion = System.Numerics.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class PlayerCubeScript : NetworkBehaviour
{
    public NetworkVariable<bool> ShouldMove;
    public NetworkVariable<bool> IsAI;
    public NetworkVariable<ulong> OwningPlayerID;
    public NetworkVariable<PlayerCubeData> _NetPCData;
    public int ColorID => _NetPCData.Value.ColorID;
    public bool isFail = false;
    public bool isSuccess = false;
    public Material RainbowMaterial;

    // [Header("Settings")]
    [SerializeField] private bool LetIncorrectPlayerCubesBounceBack => BlockamiData.Instance.LetIncorrectPlayerCubesBounceBack;
    
    [Header("State")]
    bool m_CubeisDead = false;
    public PlayerShotObject OwningPlayerShot;
    public DrawingGrid OwningGrid;
    public bool IsReverseDirection;

    
    private Vector3 DirToMove
    {
        get => net_DirToMov.Value; // Get the value from the NetworkVariable
        set => net_DirToMov.Value = value; // Set the value to the NetworkVariable
    }
    [Header("Internal")]
    public NetworkVariable<Vector3> net_DirToMov;
    public event System.Action<PlayerCubeScript> PCDied;
    private Material m_CubeMaterial;
    private static readonly int s_color = Shader.PropertyToID("_Color");
    private Renderer Rend;
    private SceneCubeNetworking ScsToDestroy;
    private PlayerCubeScript PcsToDestroy;
    private Vector3 OriginalScale;
    public Rigidbody rb;
    public float LastReverseTime = -999.0f;
    public bool IsRainbow = false;



    public bool DebugBounce = false;
    
    
    

    private void Awake()
    {
        OriginalScale = gameObject.transform.localScale;
        m_CubeMaterial = GetComponent<MeshRenderer>().material;
        _NetPCData.OnValueChanged += OnPCDataChanged;
        ShouldMove.OnValueChanged += OnShouldMoveChanged;
        Rend = GetComponent<Renderer>();
        rb = GetComponent<Rigidbody>();
    }


    public void ResetPlayerCube()
    {
        _NetPCData.Value = PlayerCubeData.Default;                // i think this is unnecessary, despawned cubes must have their vars wiped already ??
    }
    
    public void Initialize( )         // on rep change of     PCData.OnValueChanged
    {
        isFail = false;
        Rend.enabled = true;
        transform.localScale = OriginalScale;
        m_CubeisDead = false;
        var col = BlockamiData.Instance.GetColorFromColorID(ColorID);
        m_CubeMaterial.SetColor(s_color, col);
        
       // LocalPlayerEntities.Instance.GetPlayerObjects(PCData.Value.OwningPlayerId).PlayerController.grid
       
       var grids = FindObjectsOfType<DrawingGrid>();
       foreach (var grid in grids)
       {
           if (grid.OwnerClientId  == _NetPCData.Value.OwningPlayerId)
           {
               OwningGrid = grid;
               DirToMove = OwningGrid.MoveDirection;
               break;
           }
       }
       

       if (_NetPCData.Value.AIPlayerNum > -1)
       {
           AIPlayer ai = FindObjectsOfType<AIPlayer>()
               .FirstOrDefault(player => player.AIPlayerNum.Value == _NetPCData.Value.AIPlayerNum);
           if (ai)
           {
               OwningGrid = ai.OwningDrawingGrid;
               DirToMove = OwningGrid.MoveDirection;
           }
       }

       if (!OwningGrid)
       {
           Debug.Log("this is this " + _NetPCData.Value.OwningPlayerId);
       }

    }

   

    private void OnPCDataChanged(PlayerCubeData previousvalue, PlayerCubeData newvalue)
    {
        if (newvalue == PlayerCubeData.Default) return;
        Initialize();
    }
    
    

 


    
    private void OnShouldMoveChanged(bool previousvalue, bool newvalue)
    {
        if (m_CubeisDead) return;
        
      if (previousvalue != newvalue)
      {
          if (newvalue)
          {
             
          }
          else
          {
              // stop moving for some reason
          }
      }
    }
    
    
    [ServerRpc]
    public void LaunchCubeServerRPC()
    {
        DirToMove = OwningGrid.MoveDirection;
        ShouldMove.Value = true;
    }


    //void FixedUpdate()
    void Update()
    {
        if (IsReverseDirection) DebugBounce = true;

      
            Debug.Log("player cube dirToMove is " + DirToMove);
        
        
        
        if (!OwningGrid) return;
        
        if (ShouldMove.Value)
        {
            if (NetworkObject.IsSpawned)
            {
                
                transform.position += DirToMove * BlockamiData.Instance.PlayerCubeMoveSpeed * Time.deltaTime;
                transform.localScale *= (1 - BlockamiData.Instance.PlayerCubeShrinkRate);

              //   rb.MovePosition(rb.position + DirToMove * BlockamiData.PlayerCubeMoveSpeed * Time.deltaTime);
              // //rb.position += DirToMove * BlockamiData.PlayerCubeMoveSpeed * Time.deltaTime;
              // rb.transform.localScale *= (1 - BlockamiData.PlayerCubeShrinkRate);
            }
        }
    }

    public bool CanPlayershotDestroyNumberCube(PlayerCubeScript pc, SceneCubeNetworking scs)
    {
        //return true;

        var NumberCubeBehavior = scs.GetComponent<NumberCubeBehavior>();
        if (!NumberCubeBehavior) return true;

        // number cubes must resolve after all other cubes in player shot, or do immediete tracing
        
        var shot = pc.OwningPlayerShot;

        int NumOfSuccess = 1;    // because this itself was a success ?
        
       foreach (var playercube in shot.allPlayerCubeScripts)
       {
           if (playercube == this) continue;    // skip a test for this cube itself??
           if (playercube.isSuccess)
           {
               NumOfSuccess++;
               continue;
               
               // playercube might have had a success collision before this one, so line trace would fail, but it should count as success
           }
           
           if (playercube.isFail)
           {
               Debug.Log("NumberCube failed test because Sibling cube was IsFail");
               return false;
           }
           Ray ray;
           Vector3 rayDirection = pc.DirToMove;
           float rayDistance = 100.0f; 
           ray = new Ray(playercube.transform.position, rayDirection * rayDistance);
           RaycastHit hitInfo;
           int SceneCubeLayerMask = 1 << LayerMask.NameToLayer("Hitable");
           
           if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, SceneCubeLayerMask))
           {
               GameObject hitObject = hitInfo.collider.gameObject;
               var foundscs = hitObject.GetComponent<SceneCubeNetworking>();
               if (!foundscs)
               {
                   Debug.Log("CanPlayershotDestroyNumberCube - Hit something on the SceneCube layer!" + hitObject.name);
                   continue;
               }
               
            if (!PlayerCubeMatchesSceneCubeColorID(playercube,foundscs,out var wasError))
            {
                Debug.Log("Blockami Log - a sibling cube was not correctly color matching for number cube");
                return false;
            }

            NumOfSuccess++;

           }
       }

       if (NumOfSuccess >= NumberCubeBehavior.NumberRequirement)
       {
           return true;
       }
       else
       {
           return false;
       }
    }

    private void OnTriggerEnter(Collider other)
    {
        // if (OwningPlayerShot == null)
        // {
        //     Debug.Log("OwningPlayerShot is null");
        // }

        var scs = other.GetComponent<SceneCubeNetworking>();
        if (scs)
        {
        
            if (PlayerCubeMatchesSceneCubeColorID(this, scs, out var OutWasError) && !isFail && CanPlayershotDestroyNumberCube(this,scs)) // SUCCESS SHOT
            {
                Rend.enabled = false;
                isSuccess = true;
                DestroySceneCubeAfterDelay(scs);
                KillPlayerCubeServerRpc();
            }
            else // FAIL SHOT
            {
                var audioController = AudioController.Instance;
                audioController.PlaySound("fail");

                bool ShouldBounce = LetIncorrectPlayerCubesBounceBack;
                if (OutWasError && !IsRainbow) ShouldBounce = true;
                
                if (ShouldBounce)
                {
                    TryReverseDirection();
                }
                
                scs.TrySetErrorCube(true);
              
                foreach (var i in OwningPlayerShot.AllPcs.Value.ToList())
                {
                    var netPc = NetworkManager.SpawnManager?.SpawnedObjects.GetValueOrDefault(i);
                    var pc = netPc ? netPc.gameObject.GetComponent<PlayerCubeScript>() : null;
                    if (pc != null)
                    {


                        if (ShouldBounce && pc == this)
                        {
                            // avoid killing the player cube that instigated a fail, let it bounce back
                        }
                        else
                        {
                            pc.isFail = true;
                            pc.DestroyPlayerCubeAfterDelay(pc);

                      //      pc?.KillPlayerCubeServerRpc();
                        }
                    }
                }




            }
        }

        var pcs = other.GetComponent<PlayerCubeScript>();
        if (pcs)
        {
            if (pcs.ColorID == ColorID || ColorID == 10)
            {
                pcs.KillPlayerCubeServerRpc(); // let PlayerCubes destroy other playercubes of matching colors, but not ruin 'Shots'
                KillPlayerCubeServerRpc();
                SpawnManager.Instance.PlayHitVFXClientRpc(transform.position, transform.forward);
            }
        }
    }


    public void DestroyPlayerCubeAfterDelay(PlayerCubeScript pcs)
        {
            if (IsReverseDirection)
            {
                Debug.Log("Avoiding DestroyPlayerCubeAfterDelay because cube is reverse direction ");
                return;
            }
            
            
            if (IsServer || IsHost)
            {
                pcs.Rend.enabled = false;      // make it immedietely invisible
                PcsToDestroy = pcs;
                Invoke("DestroyPlayerCubeByPlayerCube", 0.1f);
            }
        }

        void DestroyPlayerCubeByPlayerCube()
        {
            if (PcsToDestroy == null || !PcsToDestroy.IsSpawned) return;

          //  ulong id = IsAI.Value ? default : OwnerClientId;
            PcsToDestroy.KillPlayerCubeServerRpc();
        
        }

        public void SetRainbow(bool enable)
        {
            IsRainbow = true;
            Rend.material = RainbowMaterial;
            PlayerCubeData newdata =
                new PlayerCubeData(10, _NetPCData.Value.OwningPlayerId, _NetPCData.Value.AIPlayerNum);
            _NetPCData.Value = newdata;
        }
    

    public void TryReverseDirection()
    {
        if (Time.time - LastReverseTime > 0.5f)
        {
           
            
            Debug.Log("player cube setting DirToMove to opposite");
           // DirToMove.Scale(-Vector3.one);
           DirToMove = DirToMove * -1.0f;
            LastReverseTime = Time.time;
            
            if (IsReverseDirection)
            {
                if (_NetPCData.Value.OwningPlayerId == NetworkManager.Singleton.LocalClientId)
                {
                    SetRainbow(true);     // makes no fucking sense why this has to be here instead of before DirToMove flip, wtf!!??
                }
            }
            
            IsReverseDirection = !IsReverseDirection;
        }
        else
        {
            Debug.Log("player cube not bouncing because too soon between toggles");
        }
    }


    bool PlayerCubeMatchesSceneCubeColorID(PlayerCubeScript pc, SceneCubeNetworking scs, out bool WasError)   // SceneCubeMatchesID
    {
        WasError = false;

        if (scs.IsErrorCube)   // error cant be destroyed, even by rainbow
        {
            WasError = true;
            if (pc.IsRainbow) return true;   // ALLOW player rainbow cubes to destroy error cubes
            return false;
        }
        
        if (scs.ColorID == pc._NetPCData.Value.ColorID)
        {
            return true;
        }
        
        if (scs.ColorID == 10 || scs.ColorID == 12)   // rainbow or heavy
        {
            return true;
        }

        return false;
    }

    void DestroySceneCubeAfterDelay(SceneCubeNetworking scs)
    {

        if (IsServer || IsHost)
        {
            if (scs.IsHealthCube || scs.AvoidDestroyByPlayerCube)       // dont destroy health cubes, just do the consequence
            {
                scs.HealthCubeHit();
                return;
            }
            
            ScsToDestroy = scs;
            Invoke("DestroySceneCubeByPlayerCube", 0.1f);
        }
    }

    void DestroySceneCubeByPlayerCube()
    {
        if (ScsToDestroy == null || !ScsToDestroy.IsSpawned) return;

        ulong id = IsAI.Value ? default : OwnerClientId;
        ScsToDestroy.KillSceneCubeServerRpc(id);
        
    }
    
    [ServerRpc]
    public void KillPlayerCubeServerRpc()
    {
        ShouldMove.Value = false;
        m_CubeisDead = true;
        PCDied?.Invoke(this);          // spawn manager returns to pool
        ResetPlayerCube();
    }
    
    

   
    private void UpdateVisuals(bool isDead)
    {
        // foreach (var ball in m_ballRenderers)                                                        // ball may have multiple ballRenderers such as the TripleBall
        //     ball.sharedMaterial = isDead ? m_deadMaterial : m_defaultMaterial;
    }

}
