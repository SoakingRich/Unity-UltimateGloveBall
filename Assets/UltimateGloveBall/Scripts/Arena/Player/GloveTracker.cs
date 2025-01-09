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

        
        
        
        
        private void Update()
        {
            UpdateTracking();
        }
        
        
        

        public void UpdateTracking()        // this script is on 
        {
            if (Glove && Armature)
            {
                // This moves the armature and hand together
                {
                    var trans = transform;           
                    var wristPosition = trans.position;
                    var wristRotation = trans.rotation;

                    Glove.Move(wristPosition, wristRotation);            // move the glove to this position rotation

                    var armTrans = Armature.transform;           // move the Armature Transfrom by Ref ????
                    armTrans.position = wristPosition; 
                    armTrans.rotation = wristRotation;
                }
            }
        }
    }
}