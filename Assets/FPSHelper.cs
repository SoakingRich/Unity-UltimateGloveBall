using System;
using System.Collections;
using System.Collections.Generic;
using Meta.Multiplayer.Core;
using Meta.Utilities.Input;
using Oculus.Interaction.AvatarIntegration;
using UnityEngine;
using UnityEngine.XR;

public class FPSHelper : MonoBehaviour
{
    private CameraRigRef camRig;
    private XRInputManager XRInput;
    private HandTrackingInputManager HandTrackingInput;
    
    
    private void Awake()
    {
        camRig = FindObjectOfType<CameraRigRef>();
        XRInput = FindObjectOfType<XRInputManager>();
        HandTrackingInput = FindObjectOfType<HandTrackingInputManager>();
        
        if (XRSettings.loadedDeviceName?.Trim() is ("MockHMD Display" or "" or null))
        {
            camRig.AvatarInputManager = XRInput;
        }
        else
        {
            camRig.AvatarInputManager = HandTrackingInput;
        }
    }

   
}
