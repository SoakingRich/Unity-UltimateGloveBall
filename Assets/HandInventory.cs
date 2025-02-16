using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using Oculus.Interaction.MoveFast;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class HandInventory : MonoBehaviour
{
     public TriggerPinchEvents OwningTriggerPinchEvents;
    // public Transform interactingHandRef;
    public Transform palmTransform;
    public Camera headTrackedCamera;
    public float showLauncherThreshold = 35.0f;

    [SerializeField] private  InventorySlot[] inventorySlots;
    public InventorySlot selectedSlot;
    private Vector3 defaultSlotScale;
    public  float selectedScaleMultiplier = 1.2f;
    public  float lerpSpeed = 5f;
    
    
    public event Action<GameObject> OnItemSpawned;
    
    [Tooltip("T.")]
    [SerializeField, Interface(typeof(IActiveState))]
    private UnityEngine.Object _BlockingActiveState;
    private IActiveState BlockingActiveState;
    
    [Tooltip("T.")]
    [SerializeField, Interface(typeof(IActiveState))]
    private UnityEngine.Object _FistActiveState;
    private IActiveState FistActiveState;

    protected virtual void Awake()
    {
        BlockingActiveState = _BlockingActiveState as IActiveState;
        FistActiveState = _FistActiveState as IActiveState;
    }


    
    
    
    private bool InventoryShouldBeActive()
    {

        if (BlockingActiveState.Active || FistActiveState.Active)
        {
            return false;
        }
        
            float angleBetweenHeadAndPalm = Vector3.Angle(-palmTransform.up, headTrackedCamera.transform.forward);
            return Mathf.Abs(angleBetweenHeadAndPalm) < showLauncherThreshold;
        
    }
    
    
    
    
    
    
    
    private void Start()
    {
        inventorySlots = GetComponentsInChildren<InventorySlot>();
        if (inventorySlots.Length > 0)
            defaultSlotScale = inventorySlots[0].transform.localScale;
    }

    
    
    
    private void Update()
    {
        if (headTrackedCamera == null)
        {
            headTrackedCamera = Camera.main;
            return;
        }
        if(palmTransform == null)
        {
            palmTransform = GameObject.FindGameObjectWithTag("HandMenuPalm").transform;
            return;
        }
        
        
        bool shouldBeActive = InventoryShouldBeActive();

        foreach (var slot in inventorySlots)
        {
            slot.gameObject.SetActive(shouldBeActive);
        }

        if (shouldBeActive)
        {
            UpdateForSelectedSlot();
        }
    }

    
    
    
    private void UpdateForSelectedSlot()
    {
        InventorySlot closestSlot = null;
        float closestAngle = float.MaxValue;

        foreach (var slot in inventorySlots)
        {
            Vector3 toSlot = slot.transform.position - headTrackedCamera.transform.position;
            float angle = Vector3.Angle(headTrackedCamera.transform.forward, toSlot);

            if (angle < closestAngle)
            {
                closestAngle = angle;
                closestSlot = slot;
            }
        }

        if (selectedSlot != closestSlot)
        {
            selectedSlot = closestSlot;
        }

        foreach (var slot in inventorySlots)
        {
            float targetScale = (slot == selectedSlot) ? defaultSlotScale.x * selectedScaleMultiplier : defaultSlotScale.x;
            slot.transform.localScale = Vector3.Lerp(slot.transform.localScale, Vector3.one * targetScale, Time.deltaTime * lerpSpeed);
         //   slot.jointTracker.enabled = slot == selectedSlot;
        }
    }




    public void SpawnInventoryItem(InventorySlot slot)
    {
        // if (slot.CurrentItem != null)
        // {
        //     GameObject spawnedItem = Instantiate(slot.CurrentItem, slot.transform.position, Quaternion.identity);
        //     OnItemSpawned?.Invoke(spawnedItem);
        // }

        if (NetworkManager.Singleton == null) return;

        if (slot.CurrentItemPrefab == null)
        {
            Debug.Log("InventorySlot does not have a prefab assigned!");
            return;
        }

        var NetPrefab =
            NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs.FirstOrDefault(x =>
                x.Prefab.name == slot.CurrentItemPrefab.name);

        if (NetPrefab != null)
        {
            // Ensure CurrentItemPrefab is a GameObject that has a NetworkObject component attached to it

            var id = NetworkManager.Singleton.LocalClientId;
            SpawnManager.Instance.RequestSpawnItemServer(slot.CurrentItemPrefab.name, slot.transform.position,
                slot.transform.rotation, id);
        }
        else
        {
            Debug.LogError("NetworkObject not found for prefab: " + slot.CurrentItemPrefab.name);
        }
        
        
        slot.SetCurrentItemPrefab(null);
    }
    
    
    

    public void TryAddToInventory(string prefabName)
    {
        var NetPrefab = NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs.FirstOrDefault(x => x.Prefab.name == prefabName);
        if (NetPrefab == null)
        {
            Debug.LogError("NetworkObject not found for prefab: " + prefabName);
            return;
        }

        var slot = inventorySlots.FirstOrDefault(x => x.CurrentItemPrefab == null);
        if (slot == null)
        {
            Debug.LogError("No available inventory slots");
            return;
        }

        slot.SetCurrentItemPrefab(NetPrefab.Prefab);
        
        
        // GameObject spawnedItem = Instantiate(NetPrefab.Prefab, Position, rotation);
        // NetworkObject networkObject = spawnedItem.GetComponent<NetworkObject>();
        //     
        // if (networkObject != null)
        // {
        //     networkObject.SpawnWithOwnership(clientId,true);  // Make sure the object is spawned and networked
        // }
        // else
        // {
        //     Debug.LogError("Prefab does not contain a NetworkObject.");
        // }

    }
}

