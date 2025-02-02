using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Oculus.Interaction.Input;
using Oculus.Interaction.MoveFast;
using Unity.Netcode;
using UnityEngine;

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

    private bool InventoryShouldBeActive
    {
        get
        {
            float angleBetweenHeadAndPalm = Vector3.Angle(-palmTransform.up, headTrackedCamera.transform.forward);
            return Mathf.Abs(angleBetweenHeadAndPalm) < showLauncherThreshold;
        }
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
        
        
        bool shouldBeActive = InventoryShouldBeActive;

        foreach (var slot in inventorySlots)
        {
            slot.gameObject.SetActive(shouldBeActive);
        }

        if (shouldBeActive)
        {
            UpdateSelectedSlot();
        }
    }

    
    
    
    private void UpdateSelectedSlot()
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
        
        if(NetworkManager.Singleton == null) return;
        
        if (slot.CurrentItemPrefab == null)
        {
            Debug.LogError("InventorySlot does not have a prefab assigned!");
            return;
        }

        var NetPrefab = NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs.FirstOrDefault(x => x.Prefab.name == slot.CurrentItemPrefab.name);
        
        if (NetPrefab != null)
        {
            // Ensure CurrentItemPrefab is a GameObject that has a NetworkObject component attached to it
          
            var id = NetworkManager.Singleton.LocalClientId;
            SpawnManager.Instance.RequestSpawnItemServer(slot.CurrentItemPrefab.name, slot.transform.position, slot.transform.rotation, id);
        }
        else
        {
            Debug.LogError("NetworkObject not found for prefab: " + slot.CurrentItemPrefab.name);
        }
        
       


    }
}

