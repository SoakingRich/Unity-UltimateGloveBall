using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DrawingGrid : NetworkBehaviour
{
    public List<SnapZone> AllSnapZones = new List<SnapZone>();
    public NetworkVariable<ulong> OwningPlayer;
    
    public Vector3 MoveDirection = Vector3.forward;

    public bool OnlyShowFirstLayerSnapZones = true;

    
    
    
    private void Awake()
    {
        AllSnapZones = new List<SnapZone>(GetComponentsInChildren<SnapZone>());
        
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
