using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEngine;

public class HandMenuInteractSelector : MonoBehaviour
{
    private Camera mainCamera;

    [SerializeField] public List<Hand> hands = new List<Hand>();

    [SerializeField] public List<RayInteractor> rayInteractors = new List<RayInteractor>();

    private Hand currentlyActiveHand = null; // Tracks the hand currently enabling interactors


    void Update()
    {
        //    HOW DOES THIS NOT FUCKING WORK, WHEN I DISABLE IN EDITOR IT WORKS FINE

        SetHighestInteractingHand();
    }





    [CanBeNull]
    private Hand SetHighestInteractingHand()
    {
        if (hands.Count <= 0 || rayInteractors.Count <= 0) return null;

        Hand handWithHighestY = null;
        float highestY = float.NegativeInfinity;

        Pose pose = new Pose(); // Initialize the Pose struct

        foreach (Hand hand in hands)
        {
            if (hand.GetJointPose(0, out pose))
            {
                var handPosition = pose.position;
                DebugUtils.DrawSphere(handPosition, 0.1f);

                if (handPosition.y > highestY)
                {
                    highestY = handPosition.y;
                    handWithHighestY = hand;
                }
            }
        }


        //   Only update interactors if the hand with the highest Y has changed
        //        IS SOME OTHER OBJECT MODIFYING ENABLE STATE??????

        currentlyActiveHand = handWithHighestY; // Update the active hand

        for (int i = 0; i < hands.Count; i++)
        {
            if (hands[i] == handWithHighestY)
            {
                rayInteractors[i].MaxRayLength = 4.0f;


            }
            else
            {
                rayInteractors[i].MaxRayLength = 0.1f;
            }
        }

        return currentlyActiveHand;
    }


}
