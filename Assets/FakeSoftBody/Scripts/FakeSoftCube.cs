using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blockami.Scripts;
using UnityEngine;
using UnityEngine.Serialization;

public class FakeSoftCube : MonoBehaviour
{
    [SerializeField] public BlockamiData BlockamiData;

    public bool Ignore;

    public float springStrength;
    public float springDamper;
    public Vector3 halfExtents;
    public float MoveTowardsMax = 0.005f;
    public float m_timeBeforeEnsureDeactivate = 3.0f;
    public float m_timeBeforeDeactivate = 1.0f;

    float squashAmount;
    Rigidbody rb;
    private Vector3 closestPointFromCentre, normalDir;
    private Collider[] ContactColliders;
    bool notColliding;

    public bool LimitToOnceBounce;
    public bool deactivated;

    public squashStretch squashStretchObjectVisual;

    public Vector3 WorldDir;

    //Composition
    //    * this is intended to have a large outer collider that is Trigger only,  and a small inner collider which is the actual collider which informs the rigidbody
    //  Physics is constantly pushing object up when at rest??


    void Start()
    {
        if (Ignore)
        {
            enabled = false;
            squashStretchObjectVisual.enabled = false;
        }

        notColliding = true;
        squashAmount = 0;
        rb = GetComponent<Rigidbody>(); // this .cs should be on an object with a rigidbody, with a seperate object to affect the visual scale of
    }

    void TrackBlockamiData()
    {
        springStrength = BlockamiData.springStrength;
        springDamper = BlockamiData.springDamper;
        halfExtents = BlockamiData.halfExtents;
        MoveTowardsMax = BlockamiData.MoveTowardsMax;
        m_timeBeforeEnsureDeactivate = BlockamiData.m_timeBeforeEnsureDeactivate;
        m_timeBeforeDeactivate = BlockamiData.m_timeBeforeDeactivate;
    }

    private void Update() // update squashAmount if colliding
    {
        TrackBlockamiData();

        if (notColliding)
        {
            squashAmount = Mathf.MoveTowards(squashAmount, 0, MoveTowardsMax); // move squashAmount toward zero 
        }
        else
        {
            float desired = (Vector3.Distance(transform.position, closestPointFromCentre) /
                             (halfExtents.magnitude / 2.0f));
            //  squashAmount = 1 - Mathf.MoveTowards(squashAmount, desired, MoveTowardsMax);   
            squashAmount = 1 - (Vector3.Distance(transform.position, closestPointFromCentre) /
                                (halfExtents.magnitude / 2.0f));
            // Calculate squashAmount based on the distance between the object and the closest collision point
            // The closer the object is to the collision location, the higher the squashAmount (up to 1)
        }

        squashStretchObjectVisual.squashAmount = -squashAmount;
        squashStretchObjectVisual.transform.up = normalDir;


        squashStretchObjectVisual.deactivated = deactivated;

    }




    void FixedUpdate() // do testing for overlap with surrounding objects        // move away from collisions
    {
        ContactColliders =
            Physics.OverlapBox(transform.position, halfExtents,
                transform.rotation); // doing sphereoverlap explicitly, needs to be box   
        foreach (var contactCol in ContactColliders)
        {
            AddPointSpring(contactCol.ClosestPoint(transform.position));
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
        float vertexMaxDistance = halfExtents.magnitude;

        Vector3 VertexWorldVel = rb.GetPointVelocity(transform.position);
        WorldDir = VertexWorldVel.normalized;

        float offset = vertexMaxDistance - 0.1f - VetexHit.distance;
        float vel = Vector3.Dot(vertexDir, VertexWorldVel);
        float force = (offset * springStrength) - (vel * springDamper);
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
            Collider other) // during trigger stay with the outer collider, record the closestpoint on the other collider to self
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
        squashAmount = -0.1f; // why 0.1 ??  spring effect?
        notColliding = true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, halfExtents.magnitude);
        
        if (Application.isPlaying)
        {
            Gizmos.color = Color.cyan;

         
                // foreach (var contactCol in ContactColliders)
                // {
                //     //  Gizmos.DrawSphere(contactCol.ClosestPoint(transform.position), 0.05f);
                // }
            
        }
    }
}


