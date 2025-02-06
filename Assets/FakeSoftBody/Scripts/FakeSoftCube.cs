using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blockami.Scripts;
using UnityEngine;
using UnityEngine.Serialization;

public class FakeSoftCube : MonoBehaviour
{
    
    [Header("Settings")]
    [SerializeField] public BlockamiData BlockamiData;
    public bool Ignore;
    public float springStrength;
    public float springDamper;
    public Vector3 halfExtentsOverlapTest;
    public float MoveTowardsMax = 0.005f;
    public float m_timeBeforeEnsureDeactivate = 3.0f;
    public float m_timeBeforeDeactivate = 1.0f;
    public bool LimitToOnceBounce;
    public float ForceClampMax = 100.0f;
    public float ForceClampMin = 0.0f;

    [Header("State")]
    public bool deactivated;
    float squashAmountTarget;
    bool notColliding;
    private Vector3 closestPointFromCentre, normalDir;
    private Collider[] ContactColliders;
    Rigidbody rb;
    public squashStretch squashStretchObjectVisual;
    public Vector3 WorldDir;






    //Composition
    //    * this object is intended to have a large outer collider on this GO that is Trigger only,  and a small inner collider which is the actual collider which informs the rigidbody
    //  Physics is constantly pushing object up when at rest??
    
    // Large outer collider is used here with OnTriggerStay and OnTriggerExit


    private void OnEnable()
    {
        TrackBlockamiData();
    }


    void Start()
    {
        if (Ignore)
        {
            enabled = false;
        }

        notColliding = true;
        squashAmountTarget = 0;
        rb = GetComponent<Rigidbody>();         // this .cs should be on an object with a rigidbody, with a seperate object to affect the visual scale of
    }

    
    
    void TrackBlockamiData()
    {
        if (!UnityEngine.Application.isEditor) return;
        
        springStrength = BlockamiData.springStrength;
        springDamper = BlockamiData.springDamper;
        halfExtentsOverlapTest = BlockamiData.halfExtents;
        MoveTowardsMax = BlockamiData.MoveTowardsMax;
        m_timeBeforeEnsureDeactivate = BlockamiData.m_timeBeforeEnsureDeactivate;
        m_timeBeforeDeactivate = BlockamiData.m_timeBeforeDeactivate;
    }

    
    
    
    
    private void Update() // update squashAmount if colliding
    {
        TrackBlockamiData();

        if (notColliding)  // // if not colling, move squashAmountTarget toward zero 
        {
            squashAmountTarget = Mathf.MoveTowards(squashAmountTarget, 0, MoveTowardsMax); 
        }
        else           // Calculate squashAmount based on the distance between the object and the closest current collision point with other objects
                            // The closer this object is to the collision location, the higher the squashAmount (up to 1)
                            // during collision, physics will push this object away from collision
        {
            float desiredSquashAmount = (Vector3.Distance(transform.position, closestPointFromCentre) /
                             (halfExtentsOverlapTest.magnitude / 2.0f));
            
            //  squashAmount = 1 - Mathf.MoveTowards(squashAmount, desired, MoveTowardsMax);   
            
            squashAmountTarget = 1 - (Vector3.Distance(transform.position, closestPointFromCentre) /       // one minus it
                                (halfExtentsOverlapTest.magnitude / 2.0f));
            
            Mathf.Clamp(squashAmountTarget, -0.5f, 0.5f);
            
            // Calculate squashAmount based on the distance between the object and the closest collision point
            // The closer the object is to the collision location, the higher the squashAmount (up to 1)
        }

        squashStretchObjectVisual.squashAmountTarget = -squashAmountTarget;    // is diff from on this script
        squashStretchObjectVisual.transform.up = normalDir;


    }




    void FixedUpdate() // do testing for overlap with surrounding objects        // move away from collisions
    {
        ContactColliders =
            Physics.OverlapBox(transform.position, halfExtentsOverlapTest,
                transform.rotation);  
        
        foreach (var contactCol in ContactColliders)
        {
            Vector3 closest = contactCol.ClosestPoint(transform.position);
          //  if(Vector3.Dot((closest-transform.position).normalized,Vector3.up) < 0.0f) 
            {AddPointSpring(closest);}
        }
    }

    
    
    
    
    void AddPointSpring(Vector3 vertexPos)
    {
        if (deactivated)
        {
            notColliding = true;
            return;
        }

        RaycastHit VetexHit = new RaycastHit();
        Vector3 vertexDir = transform.position - vertexPos;
        float vertexMaxDistance = halfExtentsOverlapTest.magnitude;

        Vector3 VertexWorldVel = rb.GetPointVelocity(transform.position);
        WorldDir = VertexWorldVel.normalized;

        float offset = vertexMaxDistance - 0.1f - VetexHit.distance;
        float vel = Vector3.Dot(vertexDir, VertexWorldVel);
        float force = (offset * springStrength) - (vel * springDamper);
        Mathf.Clamp(force, ForceClampMin, ForceClampMax);
        rb.AddForceAtPosition(vertexDir.normalized * force, vertexPos);

        if (LimitToOnceBounce)
        {
            CancelInvoke("Deactivate");
            Invoke("Deactivate", m_timeBeforeDeactivate);

            Invoke("EnsureDeactivate", m_timeBeforeEnsureDeactivate);
        }
    }

    private void EnsureDeactivate()
    {
        Deactivate();
    }

    private void Deactivate()
    {

        deactivated = true;
        CancelInvoke("Deactivate");
        CancelInvoke("EnsureDeactivate");
    }


    private void
        OnTriggerStay(
            Collider other)             // during trigger stay with the outer collider, record the closestpoint on the other collider to self
    {
        if (deactivated) return;

        notColliding = false;
        closestPointFromCentre = other.ClosestPoint(transform.position);
        Debug.DrawLine(transform.position, closestPointFromCentre, Color.green);
        normalDir = (transform.position - closestPointFromCentre).normalized;
    }

    private void
        OnTriggerExit(
            Collider other) // when we stop colliding with others (due to spring)  set squashAmount to -0.1 and lerp towards 0 again
    {
        squashAmountTarget = -0.1f; // why 0.1 ??  spring effect?
        notColliding = true;
    }

    // private void OnDrawGizmos()
    // {
    //     Gizmos.color = Color.magenta;
    //     Gizmos.DrawWireSphere(transform.position, halfExtents.magnitude);
    //     
    //     if (Application.isPlaying)
    //     {
    //         Gizmos.color = Color.cyan;
    //
    //      
    //             // foreach (var contactCol in ContactColliders)
    //             // {
    //             //     //  Gizmos.DrawSphere(contactCol.ClosestPoint(transform.position), 0.05f);
    //             // }
    //         
    //     }
    // }
}


