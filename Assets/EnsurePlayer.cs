using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UltimateGloveBall.Arena.Gameplay;
using UltimateGloveBall.Arena.Services;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

public class EnsurePlayer : MonoBehaviour
{
    [SerializeField] public Object playerPrefab;
    [SerializeField] public Object NetworkManagerPrefab;
    [SerializeField] public Object LocalPlayerEntitiesPrefab;


    private void Awake()
    {
        if (NetworkManager.Singleton == null)
        {
            GameObject player = Instantiate(NetworkManagerPrefab) as GameObject;
        }
        
        if(LocalPlayerEntities.Instance == null)
        {
            GameObject player = Instantiate(LocalPlayerEntitiesPrefab) as GameObject;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        if (Camera.main == null)
        {
            // Instantiate the player prefab
            GameObject player = Instantiate(playerPrefab) as GameObject;

            // Make sure the player possesses the camera on the player prefab
            Camera playerCamera = player.GetComponentInChildren<Camera>();
            if (playerCamera != null)
            {
                // Set the camera to be the main camera
                playerCamera.tag = "MainCamera";
                Debug.Log("MR LOG - force spawning a player");
                
                
                
                GameManager gm = FindObjectOfType<GameManager>();
                var playerGrids = FindObjectsOfType<DrawingGrid>().Where(s => s.DrawingGridIndex == 0).ToArray();
                var playerGrid = playerGrids[0];
                
                Vector3 pos = playerGrid.transform.position + playerGrid.transform.rotation * Vector3.forward * -1.0f;
                pos.y = 0;
                
                ClientRpcParams clientRpcParams = new ClientRpcParams();
                gm.OnRespawnClientRpc(pos, playerGrid.transform.rotation, gm.CurrentPhase, clientRpcParams);
            }
        }
        
        

      
    }

   
}
