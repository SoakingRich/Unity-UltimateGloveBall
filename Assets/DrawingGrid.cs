using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.Netcode;
using UnityEngine;

public class DrawingGrid : NetworkBehaviour
{

    [Header("Settings")] 
    public int DrawingGridIndex;
    public Vector3 MoveDirection = Vector3.forward;
    public bool OnlyShowFirstLayerSnapZones = true;
    public bool AIPlayerIsActive;
    public AIPlayer m_AIPlayer;
    
    [Header("State")]
    public NetworkVariable<ulong> OwningPlayer;
    
    [Header("Internal")]
    public List<SnapZone> AllSnapZones = new List<SnapZone>();
    public DrawPointerUI PointerUI;
    public List<HealthCubeTransform> AllHealthCubeTransforms;
    
    



    
    
    
    
    private void Awake()
    {
        if (PointerUI) PointerUI.OwningDrawingGrid = this;
        
      MoveDirection = transform.forward;
        AllSnapZones = new List<SnapZone>(GetComponentsInChildren<SnapZone>());
        AllHealthCubeTransforms = new List<HealthCubeTransform>(GetComponentsInChildren<HealthCubeTransform>());
     
        
        if (OnlyShowFirstLayerSnapZones)
        {
            foreach (var sz in AllSnapZones)
            {
                if (sz.Coords.z > 0)
                {
                    sz.gameObject.SetActive(false);
                }
            }
        }
    }

    public void Update()
    {
        m_AIPlayer.gameObject.SetActive(AIPlayerIsActive);
        m_AIPlayer.enabled = AIPlayerIsActive;
        m_AIPlayer.AIPlayerIsActive = AIPlayerIsActive;
    }

    public override void OnNetworkSpawn()
    {
        OwningPlayer.OnValueChanged += OnOwningPlayerChanged;
    }

    
    

    private void OnOwningPlayerChanged(ulong previousvalue, ulong newvalue)
    {
       
    }

    // [ServerRpc]
    // public void RequestSpawnPlayerCubeServerRpc(Vector3 position, Quaternion rotation, ServerRpcParams rpcParams = default)
    // { 
    //    var newObject = NetworkObject.InstantiateAndSpawn(NetworkManager.Singleton, OwningPlayer.Value, false, false, false, transform.position, transform.rotation);
    //     var clientId = rpcParams.Receive.SenderClientId;
    //     newObject.GetComponent<NetworkObject>().ChangeOwnership(clientId);
    // }
}
