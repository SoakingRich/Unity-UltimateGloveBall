using Oculus.Interaction.MoveFast;
using Unity.Netcode;
using UnityEngine;

public class InventorySlot : MonoBehaviour
{
    public GameObject CurrentItem;
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
    }

    
    
    
    private void OnTriggerEnter(Collider other)
    {
        //
        // TriggerPinchEvents pinchEvents = other.GetComponent<TriggerPinchEvents>();
        // if (pinchEvents != null && handInventory != null && handInventory.selectedSlot == this)
        // {
        //     if (pinchEvents != handInventory.OwningTriggerPinchEvents)
        //     {
        //         handInventory.SpawnInventoryItem(this);
        //     }
        // }
    }
    
    
    
    

    public void SetCurrentItemPrefab(GameObject newPrefab)
    {
        CurrentItemPrefab = newPrefab;
        SetCurrentItemGhost(newPrefab);
    }
    
    
    
    
    
    
    
    public void SetCurrentItemGhost(GameObject newItem)
    {
        if (CurrentItem != null)
        {
            Destroy(CurrentItem);
        }

        CurrentItem = Instantiate(newItem, transform);
        CurrentItem.transform.localPosition = Vector3.zero;
        CurrentItem.transform.localRotation = Quaternion.identity;

        // Get Mesh Filter
        currentItemMeshFilter = CurrentItem.GetComponentInChildren<MeshFilter>();
        if (currentItemMeshFilter != null)
        {
            ScaleMeshToFit();
        }

        // Disable collisions
        Collider[] colliders = CurrentItem.GetComponentsInChildren<Collider>();
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

        CurrentItem.transform.localScale = Vector3.one * scaleFactor;
    }
}