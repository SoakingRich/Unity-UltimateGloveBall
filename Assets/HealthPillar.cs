using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class HealthPillar : MonoBehaviour
{

    [Header("Fill Material")]
    public Renderer targetRenderer;
    [SerializeField] private MaterialPropertyBlock propertyBlock;
    public float CurrentFillTarget = 0.5f;
   public float CurrentFillLerp = 0.5f;
    public float LerpSpeed = 5.0f;

    [Header("Health")] 
    private int HealthInt = 3;
    
    public HealthCubeTransform healthCubeTransform;
    

    void Start()
    {

    }

    void Update()
    {
        
        
        
        UpdateFillMaterial();
    }

    private void UpdateFillMaterial()
    {
        if (targetRenderer == null) return;
        if (propertyBlock == null)
        {
            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock = new MaterialPropertyBlock();
        }

        
        CurrentFillLerp = Mathf.Lerp(CurrentFillLerp, CurrentFillTarget, Time.deltaTime * LerpSpeed);
        propertyBlock.SetFloat("_Fill", CurrentFillLerp);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }
}
