using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Meta.Multiplayer.Core;
using Meta.Utilities.Input;
using Oculus.Interaction.AvatarIntegration;
using Oculus.Interaction.Input;
using Oculus.Avatar2;
using Oculus.Interaction.MoveFast;
using UltimateGloveBall.Arena.Services;
using UnityEngine;
using UnityEngine.XR;

public class FPSHelper : MonoBehaviour
{
    private CameraRigRef camRig;
    private XRInputManager XRInput;
    private HandTrackingInputManager HandTrackingInput;

    SyntheticHand[] allSyntheticHands;

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



    void Update()
    {

        if (XRSettings.loadedDeviceName?.Trim() is ("MockHMD Display" or "" or null))
        {
            if (!(allSyntheticHands != null && allSyntheticHands.Any()))
            {
                allSyntheticHands = FindObjectsOfType<SyntheticHand>();
                return;
            }

            foreach (var syntheticHand in allSyntheticHands)
            {
                CAPI.ovrAvatar2JointType hand = syntheticHand.Handedness == Handedness.Left
                    ? CAPI.ovrAvatar2JointType.LeftHandWrist
                    : CAPI.ovrAvatar2JointType.RightHandWrist;
                if (LocalPlayerEntities.Instance.Avatar)
                {
                    // var t = LocalPlayerEntities.Instance.Avatar.GetJointTransform(hand);
                    // syntheticHand.transform.position = t.position;
                    // syntheticHand.transform.rotation = t.rotation;
                    
                  //  syntheticHand.transform.localPosition = new Vector3(-0.242f, 1.276f, 0.323f);
                }
            }

        }


    }
}
