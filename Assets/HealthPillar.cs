using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;


public class HealthPillar : NetworkBehaviour
{

    // [Header("Fill Material")]
    // public Renderer targetRenderer;
    // [SerializeField] private MaterialPropertyBlock propertyBlock;
    // public float CurrentFillTarget = 0.5f;
    // public float LerpSpeed = 5.0f;

    [Header("Settings")] 
    public int HealthInt = 6;
    public int MaxHealth = 6;
    public float m_duration = 1.0f;
    public Transform PillarVisual;
    
    [Header("State")]
    
    [Header("Internal")]
    public Action<HealthPillar> OnHealthPillarDepleted;

    private HealthCubeTransform[] AllRelevantHCTs;
    private Vector3 OriginalLocalScale;

    private void Awake()
    {
       var AllHCTs = new List<HealthCubeTransform>(FindObjectsOfType<HealthCubeTransform>());
       AllRelevantHCTs = AllHCTs.Where(s => s._HealthPillar == this).ToArray();

       foreach (var hct in AllRelevantHCTs)
       {
           hct.OnHealthTransformHit += OnHealthTransformHit;
       }
    }

    
    
    
    
    void Start()
    {
        OriginalLocalScale = PillarVisual.localScale;
        
       
    }



    [ContextMenu("DebugOnHealthTransformHit")]
    public void DebugOnHealthTransformHit()
    {
        OnHealthTransformHit(null);
    }

    public void RestoreHealth(int HealAmount)
    {
        HealthInt += HealAmount;
        HealthInt = Mathf.Clamp(HealthInt, 0, MaxHealth);
        float scalefactor = (float)HealthInt / (float)MaxHealth;
        ScaleToFactorClientRpc(scalefactor);

    }
    
    public virtual void OnHealthTransformHit(HealthCubeTransform obj)
    {
        var cubesToDestroy = new List<SceneCubeNetworking>();
        
        HealthInt--;
        
        if(HealthInt <= 0)
        {
            HealthInt = 0;
            
            OnHealthPillarDepleted?.Invoke(this);
            
            foreach (var hct in AllRelevantHCTs)
            {
                cubesToDestroy.Add(hct.OwningHealthCube);
            }
            
            foreach (var cube in cubesToDestroy)
            {
                if(cube == null) continue;
                cube.LocalKillSceneCube();
            }
            
           
        }

        float scalefactor = (float)HealthInt / (float)MaxHealth;
        
        ScaleToFactorClientRpc(scalefactor);
            
     
      
    }
    
    [ClientRpc]
    public void ScaleToFactorClientRpc(float factor, bool OnCompleteEvents = true)
    {
        float finalscale = OriginalLocalScale.y * factor;
       
        m_duration = 1;
        PillarVisual.transform.DOScale(new Vector3(OriginalLocalScale.x, finalscale, OriginalLocalScale.z), m_duration).OnComplete(() =>
        {
            if(!OnCompleteEvents) return;
            
            // actually call GameManager event,  if we want health deplete consequence to happen on visual depletion
        });
    }
    

    void Update()
    {
        
        
        
        UpdateFillMaterial();
    }

    private void UpdateFillMaterial()
    {
        // if (targetRenderer == null) return;
        // if (propertyBlock == null)
        // {
        //     targetRenderer.GetPropertyBlock(propertyBlock);
        //     propertyBlock = new MaterialPropertyBlock();
        // }
        //
        //
        // CurrentFillLerp = Mathf.Lerp(CurrentFillLerp, CurrentFillTarget, Time.deltaTime * LerpSpeed);
        // propertyBlock.SetFloat("_Fill", CurrentFillLerp);
        // targetRenderer.SetPropertyBlock(propertyBlock);
    }
    
    
    
    
    public void ResetPillar()
    {
        HealthInt = MaxHealth;
    }
    
}
