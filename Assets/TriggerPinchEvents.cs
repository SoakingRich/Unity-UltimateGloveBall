using System;
using System.Collections;
using System.Collections.Generic;
using Blockami.Scripts;
using Meta.Utilities;
using Meta.XR.MultiplayerBlocks.Shared;
using Oculus.Interaction.Input;
using UltimateGloveBall.Arena.Services;
using Unity.Netcode;
using Oculus.Avatar2;
using UltimateGloveBall.Arena.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;


public class TriggerPinchEvents : MonoBehaviour // trigger / pinch events Per Hand/Controller
{
    [SerializeField] public BlockamiData BlockamiData;

    [SerializeField] public Hand m_hand; // hand is useful for poses
    public OVRHand TrackingHand; // ovr hand is useful for pinching functions

    public Controller TrackingController;

    [SerializeField] public bool IsRight = false;
    [SerializeField] public bool m_IsCurrentlyPressed;

    public InputAction RightTriggerAction;
    public InputAction LeftTriggerAction;
    
    public HandVelocity[] HandVelocities;
    public EyeTracking eyeTracking;



    public event Action<bool, OVRHand, Controller> TriggerPinchPressedEvent; // pressed event
    public event Action<bool, OVRHand, Controller> TriggerPinchReleasedEvent; // released event
    public event Action<bool, OVRHand, Controller> IsTriggerPinchingEvent; // update while pinching trigger

    private bool LastPressed = false;

    private Pose currentPose;
    private HandJointId handJointId = HandJointId.HandIndexTip; //  pointer index bone
    
    
    
    
    
    
    
    

    void OnStraightPunch(bool isRight, Transform t)
    {
        eyeTracking.CurrentEyetrackedSnapZone?.TrySpawnCube(isRight);
    }
    
    void OnLeftHook(bool isRight, Transform t)
    {
        FirePunch();
    }
    
    void OnRightHook(bool isRight, Transform t)
    {
        FirePunch();
    }
    
    void OnUppercut(bool isRight, Transform t)
    {
        FirePunch();
    }

    void FirePunch()
    {
        FireCurrentShot();
    }
    
    
    void Start()
    {
        eyeTracking = FindObjectOfType<EyeTracking>();
        HandVelocities = FindObjectsOfType<HandVelocity>();
        foreach (var hv in HandVelocities)
        {
            hv.OnStraightPunch += OnStraightPunch;
            hv.OnLeftHook += OnLeftHook;
            hv.OnRightHook += OnRightHook;
            hv.OnUppercut += OnUppercut;
        }
        
        
        BlockamiData[] allBlockamiData = Resources.LoadAll<BlockamiData>("");
        BlockamiData = System.Array.Find(allBlockamiData, data => data.name == "BlockamiData");

        RightTriggerAction.Enable();
        LeftTriggerAction.Enable();

        // BlockamiData = Resources.Load<BlockamiData>("BlockamiData");


        //  m_hand = Handroot_gO.GetComponentInChildren<Hand>();
        // TrackingHand = Handroot_gO.GetComponent<OVRHand>();

        IsTriggerPinchingEvent += OnIsTriggerPinchingEvent; // internal binding
        TriggerPinchReleasedEvent += OnTriggerPinchReleasedEvent; // internal binding
    }

    public void BindToPunchDetection(PunchDetectionPlane pd)
    {
        pd.onHit += OnPunchDetected;
    }

    private void OnPunchDetected()
    {
        if (BlockamiData.ShootCubesOnPinchTriggerRelease) return;

        FireCurrentShot();
    }


    private void OnTriggerPinchReleasedEvent(bool arg1, OVRHand arg2, Controller arg3)
    {
        if (!BlockamiData.ShootCubesOnPinchTriggerRelease) return;
        
        FireCurrentShot();
        
        
        // if (LastPressed == true) // weird retarded bug with FPS controller
        // {
        //     LastPressed = false;
        //     return;
        // }
    }

    private void FireCurrentShot()
    {
        if (!LocalPlayerEntities.Instance.LocalPlayerController) return;
        var shotUlong = LocalPlayerEntities.Instance.LocalPlayerController.CurrentPlayerShot.Value;
        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue((ulong)(int)shotUlong, out var
            netObj);
        var shot = netObj ? netObj.GetComponent<PlayerShotObject>() : null;

        if (shot != null && shot.AllPcs.Value.Count > 0)
        {
            if (shot.IsRight.Value == IsRight) // fire shot if the shot belongs to This TriggerPinchEvents handedness
            {
                shot.FireShotServerRpc();

                LocalPlayerEntities.Instance.LocalPlayerController.CyclePlayerColor();

                foreach (var sz in LocalPlayerEntities.Instance.LocalPlayerController.OwnedDrawingGrid.AllSnapZones)
                {
                    sz.HasCurrentlySpawnedCube = false;
                }
            }
        }
    }


    private void OnIsTriggerPinchingEvent(bool isRight, OVRHand Hand, Controller Controller)
    {

    }




    void Update()
    {

        var isCurrentlyPressed = false;

        m_hand.GetJointPose(handJointId, out currentPose);
        transform.position = currentPose.position;

        if (OVRInput.activeControllerType != OVRInput.Controller.Hands)
        {

            // if (!LocalPlayerEntities.Instance.Avatar)
            // {
            //
            //     // var avat = FindObjectOfType<AvatarEntity>();
            //     // transform.position = Meta.Multiplayer.Avatar.AvatarEntity  avat.GetJointTransform(CAPI.ovrAvatar2JointType.RightHandWrist).position;
            //
            // }
            // else
            // {
            //
            //     transform.position = IsRight
            //         ? LocalPlayerEntities.Instance.Avatar.GetJointTransform(CAPI.ovrAvatar2JointType.RightHandWrist)
            //             .position
            //         : LocalPlayerEntities.Instance.Avatar.GetJointTransform(CAPI.ovrAvatar2JointType.LeftHandWrist)
            //             .position;
            //
            // }



            if (IsRight)
            {

                if (RightTriggerAction.phase == InputActionPhase.Performed)
                {
                    if (IsRight)
                        TriggerPinchPressedEvent?.Invoke(true, TrackingHand, null); // Trigger was pressed this frame
                }
                else if (RightTriggerAction.phase == InputActionPhase.Canceled)
                {
                    if (IsRight)
                        TriggerPinchReleasedEvent?.Invoke(true, TrackingHand, null); // Trigger was released this frame
                }
                else if (RightTriggerAction.ReadValue<float>() > 0)
                {
                    if (IsRight)
                        IsTriggerPinchingEvent?.Invoke(true, TrackingHand, null); // Trigger is still held this frame
                }

                isCurrentlyPressed = RightTriggerAction.ReadValue<float>() > 0;

            }
            else
            {


                if (LeftTriggerAction.phase == InputActionPhase.Performed)
                {
                    if (!IsRight) TriggerPinchPressedEvent?.Invoke(false, TrackingHand, null);
                }
                else if (LeftTriggerAction.phase == InputActionPhase.Canceled)
                {
                    if (!IsRight) TriggerPinchReleasedEvent?.Invoke(false, TrackingHand, null);
                }
                else if (LeftTriggerAction.ReadValue<float>() > 0)
                {
                    if (!IsRight) IsTriggerPinchingEvent?.Invoke(false, TrackingHand, null);
                }

                isCurrentlyPressed = LeftTriggerAction.ReadValue<float>() > 0;

            }

            if (isCurrentlyPressed && !LastPressed)
            {
                TriggerPinchPressedEvent?.Invoke(false, TrackingHand, null);
            }
            else if (!isCurrentlyPressed && LastPressed)
            {
                TriggerPinchReleasedEvent?.Invoke(false, TrackingHand, null);
            }

            LastPressed = isCurrentlyPressed;

            // isCurrentlyPressed = (RightTriggerAction.ReadValue<float>() > 0 && IsRight) ||
            //  LeftTriggerAction.ReadValue<float>() > 0 || LeftTriggerAction.ReadValue<float>() > 0;

        }
    

            //////////////////// Track Pinching State


        else if (OVRInput.activeControllerType == OVRInput.Controller.Hands)
        {

            // if (!TrackingHand) return;
            // if (!TrackingHand.IsTracked || TrackingHand.IsSystemGestureInProgress) return;
            //
            //                     if (transform.parent.gameObject!= TrackingHand.gameObject)        // ensure this gO is attached to m_hands
            //                     {
            //                         transform.position = currentPose.position;         // set world position to pose, but set transform parent to the trackinghand??
            //                         transform.rotation = currentPose.rotation;
            //                         transform.parent = TrackingHand.transform;
            //                     }

            isCurrentlyPressed = TrackingHand.IsPressed(); // returns GetFingerIsPinching(HandFinger.Index);

            if (isCurrentlyPressed && !LastPressed)
            {
                TriggerPinchPressedEvent?.Invoke(IsRight, TrackingHand, null);
            }
            else if (!isCurrentlyPressed && LastPressed)
            {
                TriggerPinchReleasedEvent?.Invoke(IsRight, TrackingHand, null);
            }

            if (isCurrentlyPressed && LastPressed)
            {
                IsTriggerPinchingEvent?.Invoke(IsRight, TrackingHand, null);
            } // OnPinchStay method

            LastPressed = TrackingHand.IsPressed();

        }

        m_IsCurrentlyPressed = isCurrentlyPressed;


    }
}






