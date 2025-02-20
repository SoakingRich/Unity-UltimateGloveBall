using System;
using System.Collections;
using System.Collections.Generic;
using Blockami.Scripts;
using UnityEngine;
using static UtilityLibrary;

public class BlockamiWall : MonoBehaviour
{
    [SerializeField] private MaterialPropertyBlock propertyBlock;
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private string MatParam = "_LastTriggerTime";

    private void OnEnable()
    {
       SpawnManager.Instance.OnHealthCubeHit += OnHealthCubeHit;
    }

    private void OnHealthCubeHit(SceneCubeNetworking obj)   // needs a client rpc
    {
        SetMaterialParamToCurrentTime();
        targetRenderer.GetPropertyBlock(propertyBlock);   
        propertyBlock.SetColor("_Color", BlockamiData.Instance.GetColorFromColorID(obj.ColorID));
        targetRenderer.SetPropertyBlock(propertyBlock);
        
    }

    [ContextMenu("DebugDoThis")]
    private void DebugDoThis()
    {
        SetMaterialParamToCurrentTime();
        targetRenderer.GetPropertyBlock(propertyBlock);   
        propertyBlock.SetColor("_Color", Color.red);
        targetRenderer.SetPropertyBlock(propertyBlock);
        
    }
    
    
    private void OnDisable()
    {
        if (IsWithEditor()) return;
        SpawnManager.Instance.OnHealthCubeHit -= OnHealthCubeHit;
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

    void Update()
    {
        if (targetRenderer)
        {
            targetRenderer.material.SetFloat("_GlobalTime", GlobalTimeManager.Instance.GetGlobalTime());
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
        
      //  float currentTime = Time.timeSinceLevelLoad;
      float currentTime = GlobalTimeManager.Instance.GetGlobalTime();
        
        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(MatParam, currentTime);
        targetRenderer.SetPropertyBlock(propertyBlock);
        
    //    Debug.Log("Set BlockamiWall time to " + currentTime);
        
    }
}
