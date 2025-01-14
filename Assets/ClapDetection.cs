using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blockami.Scripts;
using Oculus.Interaction.Input;
using UnityEngine;

public class ClapDetection : MonoBehaviour
{
    private BlockamiData BlockamiData;
    
    public event Action OnClapDetected;   // clapped event
    
    [Header("Settings")]
    [SerializeField]  private float m_clapMinDist = 0.5f;
    [SerializeField]  private HandJointId handJointId = HandJointId.HandForearmStub;   
    [SerializeField] private double fuzzyborder = 0.2f;
    
    [Header("State")]
    
    [Header("Internal")]
    private List<SyntheticHand> AllSyntheticHands = new List<SyntheticHand>();
    private bool onCooldown;

    
    
    void Start()
    {
        BlockamiData[] allBlockamiData = Resources.LoadAll<BlockamiData>("");
        BlockamiData = System.Array.Find(allBlockamiData, data => data.name == "BlockamiData");
        var ovrManager = FindObjectOfType<OVRManager>();
        AllSyntheticHands = ovrManager.GetComponentsInChildren<SyntheticHand>()?.ToList();

    }

    
    
    void Update()
    {
        // var hand1 = AllSyntheticHands[0];
        // var hand2 = AllSyntheticHands[1];
        // Pose hand1pose;
        // Pose hand2pose;
        //
        // hand1.GetJointPose(handJointId, out hand1pose);
        // hand2.GetJointPose(handJointId, out hand2pose);
        //
        // if (!onCooldown)
        // {
        //     if (Vector3.Distance(hand1pose.position, hand2pose.position) < m_clapMinDist)
        //     {
        //         OnClapDetected?.Invoke();
        //     }
        // }
        // else
        // {
        //     if (Vector3.Distance(hand1pose.position, hand2pose.position) > m_clapMinDist + fuzzyborder)
        //     {
        //         onCooldown = false;
        //     }
        // }
    }
}
