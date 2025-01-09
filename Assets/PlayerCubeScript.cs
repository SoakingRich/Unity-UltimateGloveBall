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
    [SerializeField] public BlockamiData BlockamiData;   
    
    public NetworkVariable<PlayerCubeData> PCData;
    
    public event System.Action<PlayerCubeScript> PCDied;
    bool m_CubeisDead = false;
    
    private Material m_CubeMaterial;
    private static readonly int s_color = Shader.PropertyToID("_Color");

    public PlayerShotObject OwningPlayerShot;
    public NetworkVariable<bool> ShouldMove;
    public NetworkVariable<ulong> OwningPlayerID;
    
    public DrawingGrid OwningGrid;
    
    private Renderer Rend;
    private SceneCubeNetworking ScsToDestroy;

    private NetworkObject m_NetworkObject;
    private Vector3 OriginalScale;

    private void Awake()
    {
        OriginalScale = gameObject.transform.localScale;
        m_CubeMaterial = GetComponent<MeshRenderer>().material;
        PCData.OnValueChanged += OnPCDataChanged;
        ShouldMove.OnValueChanged += OnShouldMoveChanged;
        Rend = GetComponent<Renderer>();
        m_NetworkObject = GetComponent<NetworkObject>();
    }

    
    public void Initialize( )         // on rep change of     PCData.OnValueChanged
    {
        Rend.enabled = true;
       transform.localScale = OriginalScale;
        m_CubeisDead = false;
        m_CubeMaterial.SetColor(s_color, PCData.Value.MyColorType.color);
       // LocalPlayerEntities.Instance.GetPlayerObjects(PCData.Value.OwningPlayerId).PlayerController.grid
       var grids = FindObjectsOfType<DrawingGrid>();
       foreach (var grid in grids)
       {
           if (grid.OwnerClientId  == PCData.Value.OwningPlayerId)
           {
               OwningGrid = grid;
           }
       }

       if (PCData.Value.AIPlayerNum > -1)
       {
           AIPlayer ai = FindObjectsOfType<AIPlayer>()
               .FirstOrDefault(player => player.AIPlayerNum.Value == PCData.Value.AIPlayerNum);
           if (ai)
           {
               OwningGrid = ai.OwningDrawingGrid;
           }
       }


    }

   

    private void OnPCDataChanged(PlayerCubeData previousvalue, PlayerCubeData newvalue)
    {
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
        if (ShouldMove.Value)
        {
            if (m_NetworkObject.IsSpawned)
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
        
        if (scs)
        {
            Rend.enabled = false;
            
            if (scs.SCData.Value.MyColorType == PCData.Value.MyColorType)
            {
                DestroySceneCubeAfterDelay(scs);
                KillPlayerCubeServerRpc();
            }
            else
            {
                foreach (var i in OwningPlayerShot.AllPcs.Value)
                {
                    var netPc = NetworkManager.SpawnManager?.SpawnedObjects.GetValueOrDefault(i);
                    var pc = netPc ? netPc.gameObject.GetComponent<PlayerCubeScript>() : null;
                    pc?.KillPlayerCubeServerRpc();
                }
            }
        }
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
        if (ScsToDestroy)
        {

            ScsToDestroy.KillSceneCubeServerRpc();
        }
    }
    
    [ServerRpc]
    public void KillPlayerCubeServerRpc()
    {
        ShouldMove.Value = false;
        m_CubeisDead = true;
        PCDied?.Invoke(this);          // spawn manager returns to pool
    }

   
    private void UpdateVisuals(bool isDead)
    {
        // foreach (var ball in m_ballRenderers)                                                        // ball may have multiple ballRenderers such as the TripleBall
        //     ball.sharedMaterial = isDead ? m_deadMaterial : m_defaultMaterial;
    }

}
