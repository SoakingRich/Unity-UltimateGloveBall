using Nova;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace NovaSamples.HandMenu
{
    /// <summary>
    /// Tracks two OVRHands and calls <see cref="Interaction.Point(Sphere, uint, object, int, InputAccuracy)"/> to send hand-tracked gesture events to the Nova UI content.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        /// <summary>
        /// A struct tracking a single OVRHand and a SphereCollider on tip of the hand's index finger
        /// </summary>
        [Serializable]
        private struct SingleHand
        {
            [Tooltip("A sphere collider on the tip of the Hand's index finger")]
            public SphereCollider Collider;
            [Tooltip("The tracked hand.")]
            public XRHandTrackingEvents Hand;

            [NonSerialized]
            public uint ID;

            public void Update()         // update for each Hand struct, which has a collider and ID
            {
                // if (!Hand.handIsTracked)
                // {
                //     return;
                // }
                
                if(Collider == null)
                {
                    return;
                }

                Interaction.Point(Collider, ID);
            }
        }
        
        
        

        private const uint LeftHandID = 0;
        private const uint RightHandID = 1;

        [SerializeField]
        [Tooltip("The left hand to track.")]
        private SingleHand leftHand = new SingleHand()
        {
            ID = LeftHandID,
        };

        [SerializeField]
        [Tooltip("The right hand to track.")]
        private SingleHand rightHand = new SingleHand()
        {
            ID = RightHandID,
        };


        private bool SetupComplete = false;

        public void Setup()
        {
            var allTPE = FindObjectsOfType<TriggerPinchEvents>();
            if (allTPE.Length < 1) { return; }

            var leftTPE = allTPE.Where(s => s.IsRight == false).ToArray();
            leftHand.ID = LeftHandID;
            leftHand.Collider = leftTPE[0].gameObject.GetComponent<SphereCollider>();
            
            var rightTPE = allTPE.Where(s => s.IsRight == true).ToArray();
            rightHand.ID = RightHandID;
            rightHand.Collider = rightTPE[0].gameObject.GetComponent<SphereCollider>();

            SetupComplete = true;

        }



        private void Update()
        {
            if (!SetupComplete)
            {
                Setup();
                return;
            }
            
            
            
            // Update each hand.
            leftHand.Update();
            rightHand.Update();
        }
    }
}

