using System;
using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using Unity.Netcode;
using UnityEngine;

public class GrabOnNetworkSpawn : NetworkBehaviour
{
 
    
    
    
    
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        TryInitialGrab();
    }
    
  
    
    

    private void TryInitialGrab()
    {
        float finaldistance;
        HandGrabInteractor hgi = FindClosestHandGrabInteractorToTarget(transform, out finaldistance);

        if (!(finaldistance < 0.5f))
        {
            Debug.Log("not close enough for GrabOnNetworkSpawn");
        }
        else
        {
            HandGrabInteractable hgInteractable = GetComponent<HandGrabInteractable>();

            if (!(hgi && hgInteractable)) return;

            hgi.ForceSelect(hgInteractable, true);
        }

    }
    
    
    
    
    
    
    private HandGrabInteractor FindClosestHandGrabInteractorToTarget(Transform target, out float dist)
    {
        HandGrabInteractor[] handGrabInteractors = FindObjectsOfType<HandGrabInteractor>();  
        HandGrabInteractor closestInteractor = null;
        
        
        float minDistance = Mathf.Infinity;  
        foreach (HandGrabInteractor hgi in handGrabInteractors)
        {
            float distance = Vector3.Distance(target.position, hgi.transform.position);  
            if (distance < minDistance)  
            {
                minDistance = distance;
                closestInteractor = hgi;
            }
        }

        dist = minDistance; 
        return closestInteractor; 
    }
}
