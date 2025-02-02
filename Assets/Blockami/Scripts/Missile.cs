using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blockami.Scripts;
using Oculus.Interaction;
using Unity.Netcode;
using UnityEngine;

public class Missile : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float speed;
    [SerializeField] public BlockamiData BlockamiData;
    [SerializeField] private Vector3 dirToMove;
    
    [Header("State")]
    public SnapZone TriggeringSnapzone;
    [SerializeField] public bool ShouldMove { get => m_netShouldMove.Value; set => m_netShouldMove.Value = value; }
    [SerializeField] public  DrawingGrid OwningDrawingGrid;
    
    [Header("NetworkGrabbable")]
    public Grabbable m_grabbable;
    public GrabInteractable m_grabInteractable;
    public TouchHandGrabInteractable m_touchHandGrabInteractable;

    [Header("Internal")]
    [SerializeField] private Rigidbody rb;
    public RigidbodyKinematicLocker m_rigidbodyKinematicLocker;
    
    
    [Header("NetworkVariables")] 
    public NetworkVariable<bool> m_netShouldMove;
    

   

    private void OnEnable()
    {
        rb = GetComponent<Rigidbody>();
        m_rigidbodyKinematicLocker = GetComponent<RigidbodyKinematicLocker>();
      
        m_grabbable = GetComponentInChildren<Grabbable>();
        
        m_touchHandGrabInteractable = GetComponentInChildren<TouchHandGrabInteractable>();
        m_grabInteractable = GetComponentInChildren<GrabInteractable>();

        m_grabInteractable.WhenStateChanged += GrabInteractableOnWhenStateChanged;         // same func for both
        m_touchHandGrabInteractable.WhenStateChanged += GrabInteractableOnWhenStateChanged;
        
        m_netShouldMove.OnValueChanged += m_netShouldMoveChanged;
    }

    private void OnDisable()
    {
        m_grabInteractable.WhenStateChanged -= GrabInteractableOnWhenStateChanged;
        m_touchHandGrabInteractable.WhenStateChanged -= GrabInteractableOnWhenStateChanged;
    }

    private void Update()
    {
        if (m_rigidbodyKinematicLocker && !m_rigidbodyKinematicLocker.IsLocked)
        {
            m_rigidbodyKinematicLocker.LockKinematic();
        }
    }


    public override void OnNetworkSpawn()
    {
        var id = NetworkObject.OwnerClientId;
       
       var grids = FindObjectsOfType<DrawingGrid>();        // get drawing grid
       foreach (var grid in grids)
       {
           if (grid.OwnerClientId  == id)
           {
               OwningDrawingGrid = grid;
               dirToMove = OwningDrawingGrid.MoveDirection;
           }
       }
       
       
       
    }
    

    public override void OnNetworkDespawn()
    {
        
    }

    
    
    
 
    private void GrabInteractableOnWhenStateChanged(InteractableStateChangeArgs obj)
    {
        if (obj.NewState == InteractableState.Select && 
            (obj.PreviousState == InteractableState.Hover || obj.PreviousState == InteractableState.Normal))
        {
            // new grab
        }
        
        if  ((obj.NewState == InteractableState.Hover || obj.NewState == InteractableState.Normal) && 
             obj.PreviousState == InteractableState.Select )
        {
            // new release

            OnMissileReleased();
        }
    }
    
    

    void OnMissileReleased()
    {
        // _ = StartCoroutine(Impl());
        //
        // IEnumerator Impl()
        // {
        //     yield return new WaitUntil(() => m_grabbable.IsGrabbed == false);
        //
        //
        //     StartVoip();
        // } 
       
        Vector3 missilePosition = transform.position;
        
        Vector3 planeOrigin = OwningDrawingGrid.transform.position; 
        Vector3 planeNormal = OwningDrawingGrid.transform.forward; 
        
        Vector3 projectedPoint = missilePosition - Vector3.Dot(missilePosition - planeOrigin, planeNormal) * planeNormal;

        SnapZone nearestSnapzone = OwningDrawingGrid.AllSnapZones
            .OrderBy(snapzone => Vector3.Distance(projectedPoint, snapzone.transform.position))
            .FirstOrDefault();
        
        if (nearestSnapzone != null)
        {
            TriggeringSnapzone = nearestSnapzone;
            
            transform.position = TriggeringSnapzone.transform.position;   // doesnt work grabbable dictates a release position rotation ???
            transform.rotation = TriggeringSnapzone.transform.rotation;
            rb.MovePosition(TriggeringSnapzone.transform.position);
            rb.MoveRotation(TriggeringSnapzone.transform.rotation);
            ShouldMove = true;
            FireMissileClient();
        }
    }

    private void m_netShouldMoveChanged(bool previousvalue, bool newvalue)
    {
        if (ShouldMove)
        {
            FireMissileClient();
        }
    }
    


    private void FixedUpdate()
    {
        if (ShouldMove)
        {
            rb.position += dirToMove * speed;
            transform.localScale *= 0.993f;
        }

    }

    public  void FireMissileClient()
    {
       // play particles
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "SceneCube")
        {
           var scs = other.GetComponentInChildren<SceneCubeNetworking>();
           if (scs)
           {
               if (scs == null || !scs.IsSpawned) return;
        
               scs.KillSceneCubeServerRpc();
           }
        }
    }
}
