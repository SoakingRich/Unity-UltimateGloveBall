using System;
using System.Collections;
using System.Collections.Generic;
using UltimateGloveBall.Arena.Gameplay;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using static UtilityLibrary;

public class BlockamiParticleFloor : MonoBehaviour
{
    public GameManager gm;
    public SpawnManager sm;
    private Renderer targetRenderer;
    private MaterialPropertyBlock propertyBlock;
    [SerializeField] public string MatParam = "_LastTriggerTime";


    private void OnEnable()
    {
      
        SpawnManager.Instance.OnSCSDied += OnScsDied;
        SpawnManager.Instance.OnDownCubeLayerDestroy += OnDownCubeLayerDestroy;
    }

    private void OnDownCubeLayerDestroy()
    {
       SetMaterialParamToCurrentTime();
    }


    private void OnDisable()
    {
        if (IsWithEditor()) return;
        SpawnManager.Instance.OnSCSDied -= OnScsDied;
        SpawnManager.Instance.OnDownCubeLayerDestroy -= OnDownCubeLayerDestroy;
    }

    private void Update()
    {
        if (targetRenderer)
        {
            targetRenderer.material.SetFloat("_GlobalTime", GlobalTimeManager.Instance.GetGlobalTime());
        }
    }
   


    private void OnScsDied(ulong id)
    {
        if (targetRenderer == null) return;
        
       NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue((ulong)(int)id, out var Cube);
       if (!Cube) return;
       
   //   SetMaterialParamToCurrentTime();
       
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
    
    
[ContextMenu("SetMaterialParamToCurrentTime")]       // M_GridFloorParticle
    private void SetMaterialParamToCurrentTime()
    {
        if (targetRenderer == null)
        {
            SetMaterial();
        }
        if (propertyBlock == null) return;
        
    float currentTime = GlobalTimeManager.Instance.GetGlobalTime();
        
        targetRenderer.GetPropertyBlock(propertyBlock);   // out var ??
        propertyBlock.SetFloat(MatParam, currentTime);
        targetRenderer.SetPropertyBlock(propertyBlock);
        
    }
}
