using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Blockami.Scripts;
using Meta.Utilities;
using Oculus.Avatar2;
using UltimateGloveBall.Arena.Services;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using Vector3 = UnityEngine.Vector3;

public class PlayerCubeScript : NetworkBehaviour
{
    public NetworkVariable<bool> ShouldMove;
    public NetworkVariable<ulong> OwningPlayerID;
    public NetworkVariable<PlayerCubeData> _NetPCData;
    public int ColorID => _NetPCData.Value.ColorID;
    
    [Header("State")]
    bool m_CubeisDead = false;
    public PlayerShotObject OwningPlayerShot;
    public DrawingGrid OwningGrid;
    
    [Header("Internal")]
    [SerializeField] public BlockamiData BlockamiData;   
    public event System.Action<PlayerCubeScript> PCDied;
    private Material m_CubeMaterial;
    private static readonly int s_color = Shader.PropertyToID("_Color");
    private Renderer Rend;
    private SceneCubeNetworking ScsToDestroy;
    private Vector3 OriginalScale;
    
    
    
    

    private void Awake()
    {
        OriginalScale = gameObject.transform.localScale;
        m_CubeMaterial = GetComponent<MeshRenderer>().material;
        _NetPCData.OnValueChanged += OnPCDataChanged;
        ShouldMove.OnValueChanged += OnShouldMoveChanged;
        Rend = GetComponent<Renderer>();
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
        ShouldMove.Value = true;
    }


    void Update()
    {
        if (!OwningGrid) return;
        
        if (ShouldMove.Value)
        {
            if (NetworkObject.IsSpawned)
            {
                transform.position += OwningGrid.MoveDirection * BlockamiData.PlayerCubeMoveSpeed * Time.deltaTime;
                transform.localScale *= (1 - BlockamiData.PlayerCubeShrinkRate);
            }
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if (OwningPlayerShot == null)
        {
            Debug.Log("OwningPlayerShot is null");
        }
        
        var scs = other.GetComponent<SceneCubeNetworking>();
        if (!scs) return;
        
        Rend.enabled = false;
        
        if (ShouldPlayerCubeDestroySceneCube(this,scs))
        {
            DestroySceneCubeAfterDelay(scs);
            KillPlayerCubeServerRpc();
        }
        else
        {
            foreach (var i in OwningPlayerShot.AllPcs.Value.ToList())
            {
                var netPc = NetworkManager.SpawnManager?.SpawnedObjects.GetValueOrDefault(i);
                var pc = netPc ? netPc.gameObject.GetComponent<PlayerCubeScript>() : null;
                pc?.KillPlayerCubeServerRpc();
            }

        }
        
        
    }

    bool ShouldPlayerCubeDestroySceneCube(PlayerCubeScript pc, SceneCubeNetworking scs)
    {
        if (scs.ColorID == pc._NetPCData.Value.ColorID)
        {
            return true;
        }

        return false;
    }

    void DestroySceneCubeAfterDelay(SceneCubeNetworking scs)
    {
        if (IsServer || IsHost)
        {
            ScsToDestroy = scs;
            Invoke("DestroySceneCube", 0.1f);
        }
    }

    void DestroySceneCube()
    {
        if (ScsToDestroy == null || !ScsToDestroy.IsSpawned) return;
        
        ScsToDestroy.KillSceneCubeServerRpc();
        
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
