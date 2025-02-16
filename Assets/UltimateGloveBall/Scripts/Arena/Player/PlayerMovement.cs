// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System.Collections;
using Blockami.Scripts;
using Meta.Multiplayer.Core;
using Meta.Utilities;
using Oculus.Interaction.MoveFast;
using UltimateGloveBall.App;
using UltimateGloveBall.Arena.Services;
using UltimateGloveBall.Utils;
using UnityEngine;
using UnityEngine.XR;

namespace UltimateGloveBall.Arena.Player
{
    /// <summary>
    /// Handles the players movement. The player has different types of movements that are handles accordingly.
    /// Teleport, snap/zoom, walking.
    /// It is possible to set boundaries so that the player can't move beyond those borders.
    /// The movements are based on the players head position rather than the center of the CameraRig.
    /// </summary>
    public class PlayerMovement : Singleton<PlayerMovement>
    {
        [SerializeField] private OVRCameraRig m_cameraRig;
        [SerializeField] private float m_movementSpeed = 3;
        [SerializeField] private float m_walkSpeed = 2;
        [SerializeField] private Transform m_head;
        [SerializeField] private float m_inEditorHeadHeight = 1.7f;
        private bool m_isMoving = false;
        private Vector3 m_destination;

        public bool RotationEitherThumbstick = true;
        public bool IsRotationEnabled = true;
        public float RotationAngle = 45.0f;
        private bool m_readyToSnapTurn;

        private bool m_useLimits;
        private float[] m_limits;

        private bool HasSnappedAvatarToPosition;
        
              
        private void Start()
        {

            
#if UNITY_EDITOR
            // // In editor we set the camera to a certain height in case we don't get HMD inputs
            // var localPos = m_head.localPosition;
            // m_head.localPosition = localPos.SetY(m_inEditorHeadHeight);
#endif
        }

        public void SetLimits(float minX, float maxX, float minZ, float maxZ)
        {
            m_useLimits = false;    // dont use limits
            return;
            
            // m_useLimits = true;
            // m_limits = new float[4] { minX, maxX, minZ, maxZ };
        }

        public void ResetLimits()
        {
            m_useLimits = false;
        }

        public void SnapPositionToTransform(Transform trans)      // this gets used in PlayerStateNetwork,  to snap the local camera rig to avatar position.
        {
            SnapPosition(trans.position, trans.rotation);
            HasSnappedAvatarToPosition = true;
        }

        public void SnapPosition(Vector3 destination, Quaternion rotation)
        {
            var thisTransform = transform;
            var curPosition = thisTransform.position;
            var headOffset = m_head.position - curPosition;       // move player, accounting for headset offset
            headOffset.y = 0;
            destination -= headOffset;
            thisTransform.position = destination;
            thisTransform.rotation = rotation;
        }

        
        private Vector3 savedPosition;
        private Quaternion savedRotation;

        public IEnumerator TryTeleportTo()
        {
            float elapsedTime = 0f;
            
            while (!HasSnappedAvatarToPosition)
            {
                if (elapsedTime >= 5.0f)
                {
                    Debug.LogError("Failed to teleport: Avatar not available within timeout.");
                    yield break; // Exit the coroutine if the timeout is reached
                }

                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            TeleportTo(savedPosition, savedRotation);

        }
        
        
        public void TeleportTo(Vector3 destination, Quaternion rotation)           // teleport player,  accounting for networking
        {
            if(!HasSnappedAvatarToPosition || !LocalPlayerEntities.Instance.Avatar)
            {
                Debug.Log("no avatar on teleport");
                savedPosition = destination;
                savedRotation = rotation;
             StartCoroutine("TryTeleportTo");
             return;
            }
            
            var netTransformComp = LocalPlayerEntities.Instance.Avatar.GetComponent<ClientNetworkTransform>();
            var thisTransform = transform;
            var curPosition = thisTransform.position;
            var headOffset = m_head.position - curPosition;
            headOffset.y = 0;
            destination -= headOffset;
            thisTransform.position = destination;
            thisTransform.rotation = rotation;
            var netTransform = netTransformComp.transform;
            netTransform.position = destination;
            netTransform.rotation = rotation;
            netTransformComp.Teleport(destination, rotation, Vector3.one);
            m_isMoving = false;
            FadeOutScreen();
            
            var HeightAdjust = FindObjectOfType<HeightAdjustment>();
            if(HeightAdjust) HeightAdjust.SetHeight();
                
        }

        public void MoveTo(Vector3 destination)
        {
            FadeScreen();
            var playerTransform = transform;
            var position = playerTransform.position;
            var headOffset = m_head.position - position;
            headOffset.y = 0;
            var newPos = destination - headOffset;
            StayWithinLimits(ref newPos, Vector3.zero);
            m_destination = newPos;
            m_isMoving = true;
        }

        public void WalkInDirectionRelToForward(Vector3 direction)
        {
            var headDir = m_head.forward;
            headDir.y = 0; // remove height dir
            var dir = Quaternion.FromToRotation(Vector3.forward, headDir) * direction;      // we need to move the player (who is not rotated same dir as head) in dir of the heads forward
            var moveDist = Time.deltaTime * m_walkSpeed;
            var playerTransform = transform;
            var position = playerTransform.position;
            var headOffset = m_head.position - position;
            var newPos = position + dir * moveDist;
            StayWithinLimits(ref newPos, headOffset);

            transform.position = newPos;
        }

        private void StayWithinLimits(ref Vector3 newPos, Vector3 headOffset)
        {
            if (m_useLimits)
            {
                var headnewPos = newPos + headOffset;
                if (headnewPos.x < m_limits[0])
                {
                    newPos.x = m_limits[0] - headOffset.x;
                }

                if (headnewPos.x > m_limits[1])
                {
                    newPos.x = m_limits[1] - headOffset.x;
                }

                if (headnewPos.z < m_limits[2])
                {
                    newPos.z = m_limits[2] - headOffset.z;
                }

                if (headnewPos.z > m_limits[3])
                {
                    newPos.z = m_limits[3] - headOffset.z;
                }
            }
        }

        private void FadeScreen()
        {
            if (GameSettings.Instance.UseBlackoutOnSnap)
            {
                OVRScreenFade.instance.SetExplicitFade(1);
            }
        }
        private void FadeOutScreen()
        {
            if (GameSettings.Instance.UseBlackoutOnSnap)
            {
                OVRScreenFade.instance.SetExplicitFade(0);
            }
        }

        
        
        
        
        private void Update()
        {
            if (m_isMoving)
            {
                var moveDist = Time.deltaTime * m_movementSpeed;
                transform.position = Vector3.MoveTowards(transform.position, m_destination, moveDist);
                if (Vector3.SqrMagnitude(transform.position - m_destination) <= Mathf.Epsilon * Mathf.Epsilon)
                {
                    transform.position = m_destination;
                    m_isMoving = false;
                    FadeOutScreen();
                }
            }       // seems to be some sort of AutoMoving,  rather than player input moving??

#if UNITY_EDITOR

           

                var editorobj = GameObject.FindGameObjectWithTag("EditorLocation");

                if (editorobj)
                {
                    var ovrCam = FindObjectOfType<OVRCameraRig>();


                    if (ovrCam != null)
                    {
                        ovrCam.transform.SetPositionAndRotation(editorobj.transform.position,
                            editorobj.transform.rotation);

                        // if (ovrCam.transform.position.y != 1.18f)
                        // {
                        //     ovrCam.transform.position = new Vector3(ovrCam.transform.position.x, 1.18f,
                        //         ovrCam.transform.position.z);
                        // }
                    }

                    if (!(XRSettings.enabled && XRSettings.isDeviceActive))
                    {
                        ovrCam.transform.position = new Vector3(
                            editorobj.transform.position.x,
                            ovrCam.transform.position.y,
                            editorobj.transform.position.z);
                    }
                }
            

#endif
            
        }

        public void DoSnapTurn(bool toRight)
        {
            if (IsRotationEnabled)

            {
                transform.RotateAround(m_cameraRig.centerEyeAnchor.position, Vector3.up, toRight ? RotationAngle : -RotationAngle);    // whole Gameobject can be rotated around Camera Position, so camera doesnt move
            }
        }
    }
}