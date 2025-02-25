// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System.Collections.Generic;
using Oculus.Avatar2;
using UltimateGloveBall.Arena.Gameplay;

namespace UltimateGloveBall.Arena.Player
{
    /// <summary>
    /// This keeps a reference to the game objects that forms a player entity. It also initializes once all components
    /// are assigned.
    /// </summary>
    public class PlayerGameObjects                        // htf is this different from LocalPlayerEntities - because theres only one LocalPlayerEntities in the game, but many PlayerGameObjects (of other players)
    {
        public PlayerControllerNetwork PlayerController;
        public PlayerAvatarEntity Avatar;
        public GloveArmatureNetworking LeftGloveArmature;
        public GloveArmatureNetworking RightGloveArmature;
        public Glove LeftGloveHand;
        public Glove RightGloveHand;

        public List<TeamColoringNetComponent> ColoringComponents = new();

       

        
        
        
        
        
        
        public void TryAttachObjects()
        {
            if (LeftGloveHand == null || RightGloveHand == null ||
                LeftGloveArmature == null || RightGloveArmature == null ||
                Avatar == null || !Avatar.IsSkeletonReady)
            {
                return;
            }
            ColoringComponents.Clear();
            
            var leftWrist = Avatar.GetJointTransform(CAPI.ovrAvatar2JointType.LeftHandWrist);       // once we have avatar, attach gloves to it 
            LeftGloveHand.HandAnchor = leftWrist;
            var leftTracker = leftWrist.gameObject.AddComponent<GloveTracker>();                               // glove tracker moves gloves and avatar wrists to same loc as controllers,   these get dynamically added here
            leftTracker.Glove = LeftGloveHand;
            leftTracker.Armature = LeftGloveArmature;
            leftTracker.UpdateTracking();
            LeftGloveArmature.ElectricTetherForHandPoint.SetParent(LeftGloveHand.transform, false);
            
            
            var rightWrist = Avatar.GetJointTransform(CAPI.ovrAvatar2JointType.RightHandWrist);
            RightGloveHand.HandAnchor = rightWrist;
            var rightTracker = rightWrist.gameObject.AddComponent<GloveTracker>();
            rightTracker.Glove = RightGloveHand;
            rightTracker.Armature = RightGloveArmature;
            rightTracker.UpdateTracking();
            RightGloveArmature.ElectricTetherForHandPoint.SetParent(RightGloveHand.transform, false);
            
            PlayerController.ArmatureLeft = LeftGloveArmature;
            PlayerController.ArmatureRight = RightGloveArmature;
            PlayerController.GloveLeft = LeftGloveHand.GloveNetworkComponent;
            PlayerController.GloveRight = RightGloveHand.GloveNetworkComponent;
            
            ColoringComponents.Add(LeftGloveHand.GetComponent<TeamColoringNetComponent>());                  // add team coloring components to our GloveHands and GloveArmatures
            ColoringComponents.Add(LeftGloveArmature.GetComponent<TeamColoringNetComponent>());
            ColoringComponents.Add(RightGloveHand.GetComponent<TeamColoringNetComponent>());
            ColoringComponents.Add(RightGloveArmature.GetComponent<TeamColoringNetComponent>());
        }
        
         public void DebugTryAttachObjects()
        {
            if (LeftGloveHand == null || RightGloveHand == null ||
                LeftGloveArmature == null || RightGloveArmature == null ||
                Avatar == null || !Avatar.IsSkeletonReady)
            {
                return;
            }
            
            
            
            ColoringComponents.Clear();
            
            // once we have avatar, attach gloves to it 
            var leftWrist = Avatar.GetJointTransform(CAPI.ovrAvatar2JointType.LeftHandWrist);         // get the transform comp of Left Wrist (controlled by bodytracking)
            LeftGloveHand.HandAnchor = leftWrist;                                          // glove must know the wrist transform, so it has somewhere to fly back to
            var leftTracker = leftWrist.gameObject.AddComponent<GloveTracker>();              // add a GloveTracker to left wrist, moves gloves and armature to same loc as avatar wrists
            leftTracker.Glove = LeftGloveHand;        // set the GloveTracker's glove and armature props
            leftTracker.Armature = LeftGloveArmature;
            leftTracker.UpdateTracking();       // do first update
            LeftGloveArmature.ElectricTetherForHandPoint.SetParent(LeftGloveHand.transform, false);
            
            
            var rightWrist = Avatar.GetJointTransform(CAPI.ovrAvatar2JointType.RightHandWrist);
            RightGloveHand.HandAnchor = rightWrist;
            var rightTracker = rightWrist.gameObject.AddComponent<GloveTracker>();
            rightTracker.Glove = RightGloveHand;
            rightTracker.Armature = RightGloveArmature;
            rightTracker.UpdateTracking();
            RightGloveArmature.ElectricTetherForHandPoint.SetParent(RightGloveHand.transform, false);
            
            PlayerController.ArmatureLeft = LeftGloveArmature;
            PlayerController.ArmatureRight = RightGloveArmature;
            PlayerController.GloveLeft = LeftGloveHand.GloveNetworkComponent;
            PlayerController.GloveRight = RightGloveHand.GloveNetworkComponent;
            
            ColoringComponents.Add(LeftGloveHand.GetComponent<TeamColoringNetComponent>());                  // add team coloring components to our GloveHands and GloveArmatures
            ColoringComponents.Add(LeftGloveArmature.GetComponent<TeamColoringNetComponent>());
            ColoringComponents.Add(RightGloveHand.GetComponent<TeamColoringNetComponent>());
            ColoringComponents.Add(RightGloveArmature.GetComponent<TeamColoringNetComponent>());
        }
    }
}