using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PickupCubeBehavior : CubeBehavior
{
    [Header("Settings")] 
    public GameObject contentPrefab;
    public bool AddToInventory = false;
    
   

    public override void ScsOnSCDied(SceneCubeNetworking obj)
    {
        // give a health pickup
    }

    public override void OnIntialized()
    {
        // make icon spin ??
    }
    
    
    
    
    
    public virtual void ScsOnSCDiedByPlayerCube(SceneCubeNetworking obj,ulong clientID)
    {
       // add health back to player

       if (AddToInventory)
       {

           var clientRpcParams =
               new
                   ClientRpcParams // create ClientRPC Params so we can specify Targets for rpc as we only want to send Respawn RPC to one client only
                   {
                       Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientID } }
                   };


           AddToInventoryClientRpc(clientRpcParams);
       }
    }
    
    
    
    

    [ClientRpc]
    public void AddToInventoryClientRpc(ClientRpcParams rpcParams)
    {
        var HandInventory = FindObjectOfType<HandInventory>();
        HandInventory.TryAddToInventory(contentPrefab.name);
    }
   
    
    
    
}
