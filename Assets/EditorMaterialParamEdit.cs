using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class EditorMaterialParamEdit : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private MaterialPropertyBlock propertyBlock;
    [SerializeField]  float m_repeatRate = 3.0f;
    [SerializeField]  bool UseOnUpdate = true;
    [SerializeField]  string m_VariableName = "_FakeTime";

  
    void Update()
    {
        if(UseOnUpdate)  UpdateMaterial();
    }
    
    
    [ContextMenu("SetInvoking")]
    void SetInvoking()
    {
        CancelInvoke("UpdateMaterial");
        InvokeRepeating("UpdateMaterial",0.0f,m_repeatRate);
    }
    
       
    
    [ContextMenu("UpdateMaterialManual")]
    void UpdateMaterialManual()
    {
        UseOnUpdate = false;
        CancelInvoke("UpdateMaterial");
        UpdateMaterial();
    }

    
    
    
    private void UpdateMaterial()
    {
        // if (targetRenderer == null)
        // {
        //     SetMaterial();
        // }
        //
        // targetRenderer.SetPropertyBlock(propertyBlock);
        //
        // float currentTime = Application.isPlaying
        //     ? Time.timeSinceLevelLoad
        //     : (float)EditorApplication.timeSinceStartup;
        //
        // targetRenderer.GetPropertyBlock(propertyBlock);
        // propertyBlock.SetFloat(m_VariableName, currentTime);
        // targetRenderer.SetPropertyBlock(propertyBlock);
        //
        //
        // EditorUtility.SetDirty(this.gameObject);
        // SceneView.RepaintAll ();
    }
    
    
    
    
    
    private void SetMaterial()
    {
       //  ParticleSystemRenderer psRenderer = GetComponent<ParticleSystemRenderer>();
       //  if (psRenderer != null)
       //  {
       //      targetRenderer = psRenderer.GetComponent<Renderer>();
       //  }
       //  else
       //  {
       //      targetRenderer = GetComponent<Renderer>();
       //  }
       //
       //  if (targetRenderer == null) return;
       //
       //  
       //  var mpb = new MaterialPropertyBlock();
       //  mpb.GetFloat(m_VariableName);
       //  targetRenderer.GetPropertyBlock(mpb);
       //  propertyBlock = mpb;
       //  
       //  
       // // propertyBlock = new MaterialPropertyBlock();
    }

    
  

    
    
    
   
}
