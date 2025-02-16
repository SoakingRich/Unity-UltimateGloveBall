using System;
using System.Collections;
using System.Collections.Generic;
using Blockami.Scripts;
using UltimateGloveBall.Arena.Services;
using Unity.Netcode;
using UnityEngine;

public class SnapZone : MonoBehaviour
{
  public  DrawingGrid OwningGrid;
  public bool HasCurrentlySpawnedCube;
  public bool OnCooldown = false;
 [SerializeField] public MeshRenderer HighlightCube;
  public Vector3 Coords;
  public GameObject SnapzoneDot;
  
  
  
  
  
  
    private void Awake()
    {
        OwningGrid = UtilityLibrary.FindObjectInDirectParents<DrawingGrid>(this.transform);
           // transform.parent.GetComponent<DrawingGrid>();
           
           HighlightCube.enabled = false;   
    }
    
    
    
    private void OnTriggerStay(Collider other)
    {
        DoTriggerStay(other);
    }

    public void DoTriggerStay(Collider other)
    {
        if (BlockamiData.Instance.HideSnapDots) return;

        if (!HasCurrentlySpawnedCube)
        {
            //   Debug.Log("snapzone triggerStay colliding with " + other.name);

            if (other.gameObject.CompareTag("Player"))
            {
               
                if (OwningGrid.GetComponent<NetworkObject>().IsOwner)
                {
                  
                    var TPE = other.GetComponent<TriggerPinchEvents>();
                    if (TPE != null)
                    {

                        HighlightCube.enabled = true;          // show highlight cube
                        OwningGrid.PointerUI?.Move(this);
                        
                      
                        if (TPE.m_IsCurrentlyPressed)
                        {
                            if (!TPE.DoAnyInteractorsHaveInteractables())       // dont try and spawn cubes if were holding a missile
                            {

                                if (!OnCooldown) // why are there cooldowns?
                                {
                                    OnCooldown = true;
                                    TrySpawnCube(TPE.IsRight);
                                }
                            }
                            else
                            {
                                Debug.Log("Dont spawn because pinch has interactable");
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
      
            if (other.gameObject.CompareTag("Player"))
            {
                if (OwningGrid.GetComponent<NetworkObject>().IsOwner)
                {
                    var TPE = other.GetComponent<TriggerPinchEvents>();
                    if (TPE != null)
                    {
                        HighlightCube.enabled = false;                // hide highlight cube
                        OnCooldown = false;
                    }

                }
            }
        
    }


    
    
    
    public void TrySpawnCube(bool isRight)
   {
       HasCurrentlySpawnedCube = true;
        SpawnManager.Instance.SpawnPlayerCubeServer(transform.position,OwningGrid.NetworkObject.OwnerClientId, isRight);
     
    }
}
