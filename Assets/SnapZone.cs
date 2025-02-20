using System;
using System.Collections;
using System.Collections.Generic;
using Blockami.Scripts;
using UltimateGloveBall.Arena.Services;
using Unity.Netcode;
using UnityEngine;
using static UtilityLibrary;

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
        DebugLogClient(" snapzone do trigger stay");
        
        if (BlockamiData.Instance.HideSnapDots) return;

        if (!HasCurrentlySpawnedCube)
        {
            //   Debug.Log("snapzone triggerStay colliding with " + other.name);

            if (other.gameObject.CompareTag("Player"))
            {

                if (!OwningGrid.GetComponent<NetworkObject>().IsOwner)
                {
                    DebugLogClient(" snapzoen trigger zone on Snapzone not owned locally");
                    return;
                }
                
                  
                    var TPE = other.GetComponent<TriggerPinchEvents>();
                    if (TPE != null)
                    {
                        DebugLogClient(" Snapzone 1");
                        
                        HighlightCube.enabled = true;          // show highlight cube
                        OwningGrid.PointerUI?.Move(this);
                        
                        DebugLogClient(" Snapzone 2");
                      
                        if (TPE.m_IsCurrentlyPressed)
                        {
                            DebugLogClient(" Snapzone 3");
                            
                            if (!TPE.DoAnyInteractorsHaveInteractables())       // dont try and spawn cubes if were holding a missile
                            {

                                DebugLogClient(" Snapzone 4");
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
       DebugLogClient(" TrySpawnCube");
       
       if (!HasCurrentlySpawnedCube)
       {
           HasCurrentlySpawnedCube = true;
           SpawnManager.Instance.SpawnPlayerCubeServer(transform.position, OwningGrid.NetworkObject.OwnerClientId,
               isRight);
           DebugLogClient(" TrySpawnCube 2 ");
       }
   }
}
