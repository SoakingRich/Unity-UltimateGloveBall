using System;
using System.Collections;
using System.Collections.Generic;
using UltimateGloveBall.Arena.Gameplay;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public class BlockamiParticleFloor : MonoBehaviour
{
    public GameManager gm;
    public SpawnManager sm;
    private Renderer targetRenderer;
    private MaterialPropertyBlock propertyBlock;
    [SerializeField] public string MatParam = "_LastTriggerTime";


    private void OnEnable()
    {
        sm = FindObjectOfType<SpawnManager>();
        sm.OnSCSDied += OnScsDied;
    }

    
    
    private void OnDisable()
    {
        // sm = FindObjectOfType<SpawnManager>();
        // sm.OnSCSDied -= OnScsDied;
    }


    private void SetMaterial()
    {
        
        propertyBlock = new MaterialPropertyBlock();
        
        ParticleSystemRenderer psRenderer = GetComponent<ParticleSystemRenderer>();
        if (psRenderer) targetRenderer = psRenderer.GetComponent<Renderer>();
        else
        {
            targetRenderer = GetComponent<Renderer>();
        }

        if (targetRenderer == null)
        {
            Debug.LogWarning("No Renderer found on Particle System or Renderer. Please assign a renderer manually.");
        }
       
    }


    private void OnScsDied(ulong id)
    {
        if (targetRenderer == null) return;
        
       NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue((ulong)(int)id, out var Cube);
       if (!Cube) return;
       
   //   SetMaterialParamToCurrentTime();
       
    }

    

    private void SetMaterialParamToCurrentTime()
    {
        if (targetRenderer == null) return;
        if (propertyBlock == null) return;
        
        float currentTime = Time.timeSinceLevelLoad;
        
        targetRenderer.GetPropertyBlock(propertyBlock);   // out var ??
        propertyBlock.SetFloat(MatParam, currentTime);
        targetRenderer.SetPropertyBlock(propertyBlock);
        
    }
}
