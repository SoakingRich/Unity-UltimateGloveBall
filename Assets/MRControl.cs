using System;
using System.Collections;
using System.Collections.Generic;
using Meta.XR.MRUtilityKit;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class MRControl : MonoBehaviour
{

    [SerializeField] bool HideAllMRIfEditor;
    
    
    private void Awake()
    {
        
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        CheckHideAllMR();
        
    }

    private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        CheckHideAllMR();
    }
    
    
    
    private void CheckHideAllMR()
    {
        
#if UNITY_EDITOR

        if (HideAllMRIfEditor)
        {
         

            var mruk = FindObjectOfType<MRUK>();
            if (mruk) mruk.enabled = false;
            

            var effectmesh = FindObjectOfType<EffectMesh>();
            if(effectmesh) effectmesh.enabled = false;

        }

#endif
        
    }
}
