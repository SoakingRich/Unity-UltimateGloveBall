using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blockami.Scripts;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UltimateGloveBall.Arena.Services;
using Unity.Netcode;
using UnityEngine;

public class Missile : NetworkBehaviour
{
    [Header("NetworkVariables")] 
    public NetworkVariable<bool> m_netShouldMove;
    [SerializeField] public bool ShouldMove { get => m_netShouldMove.Value; set => m_netShouldMove.Value = value; }
    
    [Header("Settings")]
    [SerializeField] private float speed;
    [SerializeField] private Vector3 dirToMove;
    
    [Header("State")]
    public SnapZone TriggeringSnapzone;
    [SerializeField] public  DrawingGrid OwningDrawingGrid;
    
    [Header("NetworkGrabbable")]
    public Grabbable m_grabbable;
    public GrabInteractable m_grabInteractable;
    public TouchHandGrabInteractable m_touchHandGrabInteractable;
    public HandGrabInteractable m_handgrabInteractable;
    public SnapInteractor m_SnapInteractor;

    [Header("Internal")]
    [SerializeField] private Rigidbody rb;
    public RigidbodyKinematicLocker m_rigidbodyKinematicLocker;
    public bool HasSnapped = false;



    void Start()
    {
        var allGrids = FindObjectsOfType<DrawingGrid>();
        OwningDrawingGrid = (DrawingGrid)UtilityLibrary.GetNearestObject(allGrids, transform.position);
        if (OwningDrawingGrid)
        {
        dirToMove = OwningDrawingGrid.MoveDirection;
            
        }
    }
   

    private void OnEnable()
    {
        rb = GetComponent<Rigidbody>();
        m_rigidbodyKinematicLocker = GetComponent<RigidbodyKinematicLocker>();
      
        m_grabbable = GetComponentInChildren<Grabbable>();
        
        m_touchHandGrabInteractable = GetComponentInChildren<TouchHandGrabInteractable>();
        m_grabInteractable = GetComponentInChildren<GrabInteractable>();
        m_handgrabInteractable = GetComponentInChildren<HandGrabInteractable>();
        m_SnapInteractor = GetComponentInChildren<SnapInteractor>();

        m_grabInteractable.WhenStateChanged += GrabInteractableOnWhenStateChanged;         // same func for both
        m_touchHandGrabInteractable.WhenStateChanged += GrabInteractableOnWhenStateChanged;
        m_handgrabInteractable.WhenStateChanged += GrabInteractableOnWhenStateChanged;
        m_SnapInteractor.WhenStateChanged += SnapInteractorStateChanged;
      
       
        
        m_netShouldMove.OnValueChanged += m_netShouldMoveChanged;
    }
    
    private void OnDisable()
    {
        m_grabInteractable.WhenStateChanged -= GrabInteractableOnWhenStateChanged;         // same func for both
        m_touchHandGrabInteractable.WhenStateChanged -= GrabInteractableOnWhenStateChanged;
        m_handgrabInteractable.WhenStateChanged -= GrabInteractableOnWhenStateChanged;
        m_SnapInteractor.WhenStateChanged -= SnapInteractorStateChanged;
        
        m_netShouldMove.OnValueChanged -= m_netShouldMoveChanged;
    }

    private void SnapInteractorStateChanged(InteractorStateChangeArgs obj)
    {
        //Debug.Log($"Missile Snap State changed from {obj.PreviousState} to {obj.NewState}");
        
        if  ((obj.NewState == InteractorState.Select  && 
             obj.PreviousState == InteractorState.Hover ))       // when missile interactor 'Selects' Grid as interactable
        {
       //     m_SnapInteractor.Interactable.WhenSelectingInteractorRemoved.Action += someevent;
         //   m_SnapInteractor.Interactable.WhenSelectingInteractorViewRemoved += someevent;
            OnMissileReleased();
         
        }
    }

    private void someevent(IInteractorView obj)
    {
       Debug.Log("snap interactable WhenSelectingInteractorViewRemoved");
      // OnMissileReleased();
    }


    public override void OnNetworkSpawn()
    {
        var id = NetworkObject.OwnerClientId;

        var grid = LocalPlayerEntities.Instance.GetPlayerObjects(id).PlayerController.OwnedDrawingGrid;
            
        OwningDrawingGrid = grid;
        dirToMove = OwningDrawingGrid.MoveDirection;
       
    }
    
    
    

    public override void OnNetworkDespawn()
    {
        
    }

    
    
    
 
    private void GrabInteractableOnWhenStateChanged(InteractableStateChangeArgs obj)
    {
        // if (obj.NewState == InteractableState.Select && 
        //     (obj.PreviousState == InteractableState.Hover || obj.PreviousState == InteractableState.Normal))
        // {
        //     // new grab
        // }
        //
        // if  ((obj.NewState == InteractableState.Hover || obj.NewState == InteractableState.Normal) && 
        //      obj.PreviousState == InteractableState.Select )
        // {
        //     // new release
        //
        //    // if(m_SnapInteractor.Interactable.())
        //     //OnMissileReleased();
        // }
    }
    
    

    void OnMissileReleased()       // on release
    {
        ShouldMove = true;
    }

    public void SnapToZone()
    {
        if (HasSnapped) return;
        
        Vector3 missilePosition = transform.position;
        
        Vector3 planeOrigin = OwningDrawingGrid.transform.position; 
        Vector3 planeNormal = OwningDrawingGrid.transform.forward; 
        
        Vector3 projectedPoint = missilePosition - Vector3.Dot(missilePosition - planeOrigin, planeNormal) * planeNormal;
        
        dirToMove = OwningDrawingGrid.MoveDirection;
        SnapZone nearestSnapzone = OwningDrawingGrid.AllSnapZones
            .OrderBy(snapzone => Vector3.Distance(projectedPoint, snapzone.transform.position))
            .FirstOrDefault();
        
        if (nearestSnapzone == null) return;
        
        TriggeringSnapzone = nearestSnapzone;
            
        transform.position = TriggeringSnapzone.transform.position;   // doesnt work grabbable dictates a release position rotation ???
        transform.rotation = TriggeringSnapzone.transform.rotation;
        rb.MovePosition(TriggeringSnapzone.transform.position);
        rb.MoveRotation(TriggeringSnapzone.transform.rotation);

        HasSnapped = true;
    }

    private void m_netShouldMoveChanged(bool previousvalue, bool newvalue)
    {
        if (ShouldMove)
        {
            
          //  FireMissileClient();
        }
    }
    


    private void FixedUpdate()
    {
        if (ShouldMove)
        {
            m_grabbable.enabled = false;
            if (rb.IsLocked())
            {
            rb.UnlockKinematic();
                
            }
            SnapToZone();
            rb.velocity = Vector3.zero;
            rb.position += dirToMove * speed;
            rb.rotation = OwningDrawingGrid.transform.rotation;
            
            rb.rotation = Quaternion.LookRotation(OwningDrawingGrid.transform.up*-1f, OwningDrawingGrid.transform.forward);

            //    transform.localScale *= 0.993f;
        }
        else
        {
            if (!rb.IsLocked())
            {
                rb.LockKinematic();
                
            }
        }

    }

    // public  void FireMissileClient()
    // {
    //    // play particles
    // }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "SceneCube")
        {
           var scs = other.GetComponentInChildren<SceneCubeNetworking>();
           if (scs)
           {
               if (scs == null || !scs.IsSpawned) return;

               if (scs.IsHealthCube)       // dont destroy health cubes, just do the consequence
               {
                   scs.HealthCubeHit();
                   NetworkObject.Despawn();
               }
               else
               {
                   
               scs.KillSceneCubeServerRpc();
               }
               
              
           }
        }
    }
}
