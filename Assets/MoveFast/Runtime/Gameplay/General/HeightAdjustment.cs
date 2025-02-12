// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Oculus.Interaction.MoveFast
{
    /// <summary>
    /// Adjusts the height of the player so it remains consistant regardless of actual height
    /// </summary>
    public class HeightAdjustment : MonoBehaviour
    {
        [SerializeField] private float _idealHeight = 1.63f;
        [SerializeField] private OVRCameraRig _cameraRig;

        private IEnumerator Start()
        {
            yield return null; //takes 2 frame for the camera to get its position
            yield return null;
            SetHeight();
            
          
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            SetHeight();
        }

        [ContextMenu("SetHeight")]
        public void SetHeight()
        {
          //   var _currentHeight = _cameraRig.centerEyeAnchor.position.y - _cameraRig.transform.position.y;
          // //  if (_currentHeight <= 1) { return; }
          //
          //   float diff = _idealHeight - _currentHeight;
          //   _cameraRig.transform.position = transform.position + Vector3.up * diff;
          //   
             Debug.Log("Blockami Log - SetHeight by HeightAdjustment");

          var diff = _idealHeight - _cameraRig.centerEyeAnchor.position.y;
          _cameraRig.transform.position = _cameraRig.transform.position + Vector3.up * diff;
        }
        
        // private void Start()
        // {
        //     _cameraRig = FindObjectOfType<OVRCameraRig>();
        // }
        //
        // private void Update()
        // {
        //     if (_cameraRig)
        //     {
        //         if(_cameraRig.transform.position.y != )
        //     }
        // }
    }
    
    
    
}
