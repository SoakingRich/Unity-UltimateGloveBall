using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Unity.Netcode;

public class EndlessRespawner : MonoBehaviour
{
    private GameObject selfPrefab;
    public bool DoneOnce;
    
    [SerializeField] private HandGrabInteractable m_handgrabInteractable;
    [SerializeField] private GrabInteractable m_grabInteractable;
    [SerializeField] private TouchHandGrabInteractable m_touchHandGrabInteractable;

    void Start()
    {
     
        selfPrefab = gameObject; 
        
        m_touchHandGrabInteractable = GetComponentInChildren<TouchHandGrabInteractable>();
        m_grabInteractable = GetComponentInChildren<GrabInteractable>();
        m_handgrabInteractable = GetComponentInChildren<HandGrabInteractable>();
    }

    void Update()
    {
        if (DoneOnce) return;
        
        if ((m_grabInteractable != null && m_grabInteractable.SelectingInteractors.Count > 0)
            ||  (m_handgrabInteractable != null && m_handgrabInteractable.SelectingInteractors.Count > 0)
            || m_touchHandGrabInteractable != null && m_touchHandGrabInteractable.SelectingInteractors.Count > 0
            )
        {
            // Spawn a new instance at the same position/rotation
            GameObject newPrefab = Instantiate(selfPrefab, transform.position, transform.rotation);
            NetworkObject netobj = newPrefab.GetComponent<NetworkObject>();
           if(netobj) netobj.Spawn();
            
            // Ensure it has the EndlessRespawner component
            if (newPrefab.GetComponent<EndlessRespawner>() == null)
            {
                newPrefab.AddComponent<EndlessRespawner>();
            }

            DoneOnce = true;
        }
    }
}