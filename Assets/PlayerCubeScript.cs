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

    // [Header("Settings")]
    [SerializeField] private bool LetIncorrectPlayerCubesBounceBack => BlockamiData.LetIncorrectPlayerCubesBounceBack;
    
    [Header("State")]
    bool m_CubeisDead = false;
    public PlayerShotObject OwningPlayerShot;
    public DrawingGrid OwningGrid;

    
    private Vector3 DirToMove
    {
        get => net_DirToMov.Value; // Get the value from the NetworkVariable
        set => net_DirToMov.Value = value; // Set the value to the NetworkVariable
    }
    [Header("Internal")]
    public NetworkVariable<Vector3> net_DirToMov;
    [SerializeField] public BlockamiData BlockamiData;   
    public event System.Action<PlayerCubeScript> PCDied;
    private Material m_CubeMaterial;
    private static readonly int s_color = Shader.PropertyToID("_Color");
    private Renderer Rend;
    private SceneCubeNetworking ScsToDestroy;
    private PlayerCubeScript PcsToDestroy;
    private Vector3 OriginalScale;
    public Rigidbody rb;
    public float LastReverseTime = -999.0f;
    
    
    
    
    
    
    

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
      
        Rend.enabled = true;
        transform.localScale = OriginalScale;
        m_CubeisDead = false;
        var col = BlockamiData.GetColorFromColorID(ColorID);
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
        if (!OwningGrid) return;
        
        if (ShouldMove.Value)
        {
            if (NetworkObject.IsSpawned)
            {
                
                transform.position += DirToMove * BlockamiData.PlayerCubeMoveSpeed * Time.deltaTime;
                transform.localScale *= (1 - BlockamiData.PlayerCubeShrinkRate);

              //   rb.MovePosition(rb.position + DirToMove * BlockamiData.PlayerCubeMoveSpeed * Time.deltaTime);
              // //rb.position += DirToMove * BlockamiData.PlayerCubeMoveSpeed * Time.deltaTime;
              // rb.transform.localScale *= (1 - BlockamiData.PlayerCubeShrinkRate);
            }
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



            if (!isFail && PlayerCubeMatchesSceneCubeColorID(this, scs)) // SUCCESS SHOT
            {
                Rend.enabled = false;
                DestroySceneCubeAfterDelay(scs);
                KillPlayerCubeServerRpc();
            }
            else // FAIL SHOT
            {
                var audioController = AudioController.Instance;
                audioController.PlaySound("fail");

                if (LetIncorrectPlayerCubesBounceBack)
                {
                    ReverseDirection();
                }

                if (true) // use ErrorCubes
                {
                    scs.TrySetErrorCube(true);
                }


                foreach (var i in OwningPlayerShot.AllPcs.Value.ToList())
                {
                    var netPc = NetworkManager.SpawnManager?.SpawnedObjects.GetValueOrDefault(i);
                    var pc = netPc ? netPc.gameObject.GetComponent<PlayerCubeScript>() : null;
                    if (pc != null)
                    {


                        if (LetIncorrectPlayerCubesBounceBack && pc == this)
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
        

    

    public void ReverseDirection()
    {
        if (Time.time - LastReverseTime > 2.0f)
        {
            DirToMove *= -1;
            LastReverseTime = Time.time;
        }
    }


    bool PlayerCubeMatchesSceneCubeColorID(PlayerCubeScript pc, SceneCubeNetworking scs)
    {
        if (scs.IsErrorCube) return false;
        
        if (scs.ColorID == pc._NetPCData.Value.ColorID)
        {
            return true;
        }
        
        if (scs.ColorID == 10)
        {
            return true;
        }

        return false;
    }

    void DestroySceneCubeAfterDelay(SceneCubeNetworking scs)
    {

        if (IsServer || IsHost)
        {
            if (scs.IsHealthCube)       // dont destroy health cubes, just do the consequence
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
