using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnsurePlayer : MonoBehaviour
{
    [SerializeField] public Object playerPrefab;

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
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
