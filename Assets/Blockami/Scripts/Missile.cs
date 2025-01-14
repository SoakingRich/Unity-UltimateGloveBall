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
    [SerializeField] public BlockamiData BlockamiData;
    
    [Header("NetworkGrabbable")]
    public Grabbable m_grabbable;
    public GrabInteractable m_grabInteractable;
    public TouchHandGrabInteractable m_touchHandGrabInteractable;

    [Header("Internal")]
    [SerializeField] private Rigidbody rb;
    
    [Header("Settings")]
    [SerializeField] private float speed;
    
    [Header("MissileState")]
    public SnapZone TriggeringSnapzone;
    [SerializeField] public bool ShouldMove { get => m_netShouldMove.Value; set => m_netShouldMove.Value = value; }

  
    [SerializeField] private Vector3 dirToMove;
    [SerializeField] public  DrawingGrid OwningDrawingGrid;

    [Header("NetworkVariables")] 
    public NetworkVariable<bool> m_netShouldMove;
    //private NetworkVariable<ulong> m_owner = new(ulong.MaxValue); 
    

   

    private void OnEnable()
    {
        rb = GetComponent<Rigidbody>();
      
        m_grabbable = GetComponentInChildren<Grabbable>();
        
        m_touchHandGrabInteractable = GetComponentInChildren<TouchHandGrabInteractable>();
        m_grabInteractable = GetComponentInChildren<GrabInteractable>();

        m_grabInteractable.WhenStateChanged += GrabInteractableOnWhenStateChanged;
        m_touchHandGrabInteractable.WhenStateChanged += GrabInteractableOnWhenStateChanged;
        
        m_netShouldMove.OnValueChanged += m_netShouldMoveChanged;
    }

    private void OnDisable()
    {
        m_grabInteractable.WhenStateChanged -= GrabInteractableOnWhenStateChanged;
        m_touchHandGrabInteractable.WhenStateChanged -= GrabInteractableOnWhenStateChanged;
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
            // Any additional logic you want to execute when a snapzone is triggered
            transform.position = TriggeringSnapzone.transform.position;
            transform.rotation = TriggeringSnapzone.transform.rotation;
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
