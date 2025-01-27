using System;
using Oculus.Interaction;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class SetMatParamOnRepeat : MonoBehaviour
{
    [Tooltip("Name of the material parameter to set. Default: '_LastTriggerTime'")]
    public string MatParam = "_LastTriggerTime"; // Default parameter name
    public float m_repeatRate = 3.0f;

    public MaterialPropertyBlock propertyBlock; // Property block for modifying properties
    private Renderer targetRenderer; // Renderer from which the material is retrieved

    private void Start()
    {
     
        SetMaterial();
        m_repeatRate = 3.0f;

        InvokeRepeating(nameof(SetMaterialParamToCurrentTime), 0.0f, m_repeatRate);
    }

    private void OnGUI()
    {
      //  InvokeMat();
    }

    void Update()
    {
       
        
#if UNITY_EDITOR
        if (propertyBlock == null) return;
        
            var time = Application.isEditor ? (float)EditorApplication.timeSinceStartup : 0.0f;
            propertyBlock.SetFloat("_FakeTime", time);
            
            Shader.SetGlobalFloat("_Time", time );
            Shader.SetGlobalFloat("_FakeTime", time );
            
            Vector4 vTime = new Vector4( time / 20, time, time*2, time*3);
            Shader.SetGlobalVector("_Time", vTime );
        
            

            // Ensure the material updates in the editor
            if (targetRenderer.sharedMaterial != null)
            {
                  EditorUtility.SetDirty(targetRenderer.sharedMaterial);
            }
#endif
    }

    private void InvokeMat()
    {
        if (!Application.isPlaying)
        {
            InvokeRepeating(nameof(SetMaterialParamToCurrentTime), 0.0f, m_repeatRate);
        }
    }

    private void SetMaterial()
    {
        // Attempt to retrieve the material from a Particle System
        ParticleSystemRenderer psRenderer = GetComponent<ParticleSystemRenderer>();
        if (psRenderer != null)
        {
            targetRenderer = psRenderer.GetComponent<Renderer>();
        }
        else
        {
            // Fallback: Attempt to retrieve the material from any other renderer
            targetRenderer = GetComponent<Renderer>();
        }

        if (targetRenderer == null)
        {
            Debug.LogWarning("No Renderer found on Particle System or Renderer. Please assign a renderer manually.");
        }
        else
        {
            // Initialize MaterialPropertyBlock
            propertyBlock = new MaterialPropertyBlock();
        }
    }






    private void SetMaterialParamToCurrentTime()
    {
#if UNITY_EDITOR
        if (targetRenderer == null)
        {
            Debug.LogWarning("Target renderer is null. Attempting to retrieve it again.");
            SetMaterial();
            if (targetRenderer == null)
            {
                return; // Abort if still null
            }
        }
        
        //propertyBlock = GetComponent<MaterialPropertyBlockEditor>().MaterialPropertyBlock;
        
        if (propertyBlock == null)
        {
        propertyBlock = new MaterialPropertyBlock();
        }
        
         targetRenderer.SetPropertyBlock(propertyBlock);
        
        
        // Get the current time to set the material parameter
        float currentTime = Application.isPlaying
            ? Time.timeSinceLevelLoad
            : (float)EditorApplication.timeSinceStartup;
        
        // Get the current material properties into the property block
        targetRenderer.GetPropertyBlock(propertyBlock);
        
        // Set the material parameter using MaterialPropertyBlock
        propertyBlock.SetFloat(MatParam, currentTime);
        
        // Apply the property block to the renderer
        targetRenderer.SetPropertyBlock(propertyBlock);
        
#endif
    


    }
}
