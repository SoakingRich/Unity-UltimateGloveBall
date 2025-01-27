using System;
using System.Collections;
using System.Collections.Generic;
using Meta.XR.MRUtilityKit;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MRControl : MonoBehaviour
{

    [SerializeField] bool HideAllMR;
    
    
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

        if (HideAllMR)
        {

            var mruk = FindObjectOfType<MRUK>();
            mruk.enabled = false;
            
            var effectmesh = FindObjectOfType<EffectMesh>();
            effectmesh.enabled = false;

        }

#endif
        
    }
}
