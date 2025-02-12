using Oculus.Interaction.MoveFast;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class InventorySlot : MonoBehaviour
{
    public GameObject CurrentItemGhost;
    public GameObject CurrentItemPrefab;
    private JointTracker jointTracker;
    private HandInventory handInventory;
    private MeshFilter currentItemMeshFilter;
    private SphereCollider slotCollider;

    private void Start()
    {
        handInventory = GetComponentInParent<HandInventory>();
        slotCollider = GetComponent<SphereCollider>();
        jointTracker = GetComponent<JointTracker>();


        if (CurrentItemPrefab != null)         // set initial inventory
        {
            SetCurrentItemPrefab(CurrentItemPrefab);
        }
    }

    
    
    
    private void OnTriggerEnter(Collider other)
    {
        
        TriggerPinchEvents pinchEvents = other.GetComponent<TriggerPinchEvents>();
        if (pinchEvents != null && handInventory != null && handInventory.selectedSlot == this)
        {
            if (pinchEvents != handInventory.OwningTriggerPinchEvents)
            {
                
                handInventory.SpawnInventoryItem(this);
            }
        }
    }
    
    
    
    

    public void SetCurrentItemPrefab(GameObject newPrefab)        // set the prefab class to be spawned, when item spawning happens
    {
        CurrentItemPrefab = newPrefab;
        SetCurrentItemGhost(newPrefab);
    }
    
    
    
    
    
    
    
    public void SetCurrentItemGhost(GameObject newItem)         // instantiate an Item Ghost in the slot,  to be shrunk and spinning
    {
        if (CurrentItemGhost != null)
        {
            Destroy(CurrentItemGhost);
        }
        
        if(newItem == null)   // has been set null destroy everything
            return;

        CurrentItemGhost = Instantiate(newItem, transform);
        CurrentItemGhost.transform.localPosition = Vector3.zero;
        CurrentItemGhost.transform.localRotation = Quaternion.identity;
        CurrentItemGhost.transform.SetParent(this.transform);

        // Get Mesh Filter
        currentItemMeshFilter = CurrentItemGhost.GetComponentInChildren<MeshFilter>();
        if (currentItemMeshFilter != null)
        {
            ScaleMeshToFit();
        }

        // Disable collisions
        Collider[] colliders = CurrentItemGhost.GetComponentsInChildren<Collider>();
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }
    }

    
    
    
    private void ScaleMeshToFit()
    {
        if (currentItemMeshFilter == null || slotCollider == null)
            return;

        Mesh mesh = currentItemMeshFilter.sharedMesh;
        if (mesh == null)
            return;

        Bounds bounds = mesh.bounds;
        Vector3 meshSize = bounds.size;
        float maxMeshDimension = Mathf.Max(meshSize.x, meshSize.y, meshSize.z);

        float slotRadius = slotCollider.radius * transform.lossyScale.x; // Adjust for InventorySlot scale
        float scaleFactor = (slotRadius * 2) / maxMeshDimension; // Fit within sphere diameter

        CurrentItemGhost.transform.localScale = Vector3.one * scaleFactor;
    }
}