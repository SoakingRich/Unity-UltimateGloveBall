using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class KillZone : MonoBehaviour
{

    public int playerCubeLayer;

    
    
    
   
    void Start()
    {
        playerCubeLayer = LayerMask.NameToLayer("PlayerCube");
    }

   
    private void OnTriggerEnter(Collider other)
    {
        
        if (!NetworkManager.Singleton.IsServer && NetworkManager.Singleton.IsConnectedClient ) return;
        
        if (other.gameObject.layer == playerCubeLayer || other.CompareTag("PlayerCube"))
        {

            var pc = other.GetComponent<PlayerCubeScript>();
            if (pc)
            {
                pc.KillPlayerCubeServerRpc();
            }

        }

        var netObj = other.gameObject.GetComponent<NetworkObject>();
        if (netObj)
        {
            netObj.Despawn(); 
        }
       
    }


}