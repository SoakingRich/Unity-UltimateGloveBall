using System;
using System.Collections;
using System.Collections.Generic;
using UltimateGloveBall.Arena.Services;
using Unity.Netcode;
using UnityEngine;

public class SnapZone : MonoBehaviour
{
  public  DrawingGrid OwningGrid;

  public bool HasCurrentlySpawnedCube;
  public bool OnCooldown = false;

  public Vector3 Coords;
  
  
    private void Awake()
    {
        OwningGrid = transform.parent.GetComponent<DrawingGrid>();
    }

    void Start()
    {
       
    }

   
    void Update()
    {
        
    }

    private void OnTriggerStay(Collider other)
    {
        if (!HasCurrentlySpawnedCube)
        {
            Debug.Log("snapzone triggerStay colliding with " + other.name);

            // Check if the other object has the "Player" tag
            if (other.gameObject.CompareTag("Player"))
            {
                // Check if the player object has ownership
                if (OwningGrid.GetComponent<NetworkObject>().IsOwner)
                {
                    // Check if the object has the TriggerPinchEvents component
                    var TPE = other.GetComponent<TriggerPinchEvents>();
                    if (TPE != null)
                    {
                        // Check if the button is pressed
                        if (TPE.m_IsCurrentlyPressed)
                        {
                            // Check if we are not on cooldown
                            if (!OnCooldown)
                            {
                                OnCooldown = true;
                                TrySpawnCube(TPE.IsRight);
                            }
                        }
                        else
                        {
                            OnCooldown = false;
                        }
                    }
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (OnCooldown)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                if (OwningGrid.GetComponent<NetworkObject>().IsOwner)
                {
                    var TPE = other.GetComponent<TriggerPinchEvents>();
                    if (TPE != null)
                    {
                        OnCooldown = false;
                    }

                }
            }
        }
    }


    
    
    
    void TrySpawnCube(bool isRight)
   {
        SpawnManager.Instance.SpawnPlayerCubeServerRpc(transform.position,OwningGrid.NetworkObject.OwnerClientId, isRight);
        //OwningGrid.RequestSpawnPlayerCubeServerRpc(transform.position, transform.rotation);
    }
}
