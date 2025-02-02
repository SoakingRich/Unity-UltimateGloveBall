// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using UnityEngine;

namespace UltimateGloveBall.Arena.Player
{
    /// <summary>
    /// Anchors the glove to the transform of this component.
    /// Executed after AvatarNetworking so that we follow the wrist properly
    /// </summary>
    [DefaultExecutionOrder(10100)]
    public class GloveTracker : MonoBehaviour                  // execution order is important here
    {
        public Glove Glove;
        public GloveArmatureNetworking Armature;
        
        // glove tracker is added to gO representing an avatar amatrue wrist in PlayerGameObjects

        
        
        
        
        private void Update()
        {
            UpdateTracking();
        }
        
        
        

        public void UpdateTracking()         
        {
            if (Glove && Armature)
            {
                // This moves the armature and glove to same as the present gO (avatar wrist transform)
                {
                    var trans = transform;           // get the present gO transform component, IE wrist transform
                    var wristPosition = trans.position;   // get present gO pos vector, then rot quat
                    var wristRotation = trans.rotation;

                    Glove.Move(wristPosition, wristRotation);            // move the glove to this position rotation on the avatar wrist

                    var armTrans = Armature.transform;           // get the armature transform component
                    armTrans.position = wristPosition;          // set the transforms position and rotation directly to same as glove/wrist
                    armTrans.rotation = wristRotation;
                }
            }
        }
    }
}