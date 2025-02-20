using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ClientTester : NetworkBehaviour
{
    public  void OnNetworkSpawn()
    {
        Debug.Log($"ClientTester + ClientId  {NetworkManager.Singleton.LocalClient.ClientId}");
        Debug.Log($"ClientTester + LocalClientId  {NetworkManager.Singleton.LocalClientId}");
        Debug.Log($"ClientTester + ConnectedHostname  {NetworkManager.Singleton.ConnectedHostname}");
       // Debug.Log($"ConnectedHostname  {NetworkManager.Singleton.}");
    }
    
    
}
