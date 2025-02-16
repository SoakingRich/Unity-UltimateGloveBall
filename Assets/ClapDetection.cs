using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blockami.Scripts;
using Oculus.Interaction.Input;
using Oculus.Interaction.MoveFast;
using Oddworm.Framework;
using UltimateGloveBall.Arena.Player;
using UnityEngine;
using UnityEngine.Events;

public class ClapDetection : MonoBehaviour
{
    private BlockamiData BlockamiData;
    
    public event Action OnClapDetected;   // clapped event
    public UnityEvent  OnClapEventDetected;   // clapped event
    
    [Header("Settings")]
    [SerializeField]  private float m_clapMinDist = 0.5f;
    [SerializeField]  private HandJointId handJointId = HandJointId.HandForearmStub;   
    [SerializeField] private double fuzzyborder = 0.2f;
    
    [Header("State")]
    [SerializeField] public bool DoRainbow;
    [SerializeField] public bool DebugDist;
    
    [Header("Internal")]
    private List<SyntheticHand> AllSyntheticHands = new List<SyntheticHand>();
    private bool onCooldown;
    Pose hand1pose;
    Pose hand2pose;
    private OVRManager ovrManager;
    
    void Start()
    {
        // BlockamiData[] allBlockamiData = Resources.LoadAll<BlockamiData>("");
        // BlockamiData = System.Array.Find(allBlockamiData, data => data.name == "BlockamiData");
        BlockamiData = BlockamiData.Instance;
        
        ovrManager = FindObjectOfType<OVRManager>();
        AllSyntheticHands = ovrManager.GetComponentsInChildren<SyntheticHand>()?.ToList();

    }

    
    
    void Update()
    {
        var hand1 = AllSyntheticHands[0];
        var hand2 = AllSyntheticHands[1];
        
        hand1.GetJointPose(handJointId, out hand1pose);
        hand2.GetJointPose(handJointId, out hand2pose);

        var dist = Vector3.Distance(hand1pose.position, hand2pose.position);

        if (DebugDist)
        {
            Debug.Log("Distance: " + dist);
            Debug.Log("onCooldown: " + onCooldown);
        }

        if (!onCooldown)
        {
            if (dist < m_clapMinDist)
            {
                ClapDetected();
                onCooldown = true;

                if (DoRainbow) onCooldown = false;
            }
        }
        else
        {
            if (dist > m_clapMinDist + fuzzyborder)
            {
                onCooldown = false;
            }
        }
    }

    
    
    
    private void ClapDetected()
    {
        OnClapDetected?.Invoke();
        OnClapEventDetected?.Invoke();

        return;
        
        PlayerControllerNetwork pcn = FindObjectOfType<PlayerControllerNetwork>();     
        BlockamiHandTint bht = FindObjectOfType<BlockamiHandTint>();
        if (bht && !pcn)                                                // allow me to clap in the main menu for testing
        {
            bht.CurrentColor = BlockamiData.GetColorFromColorID(BlockamiData.GetRandomColorID());
          //  DbgDraw.WireSphere(hand1pose.position, hand1pose.rotation, new Vector3(0.3f,0.3f,0.3f), Color.yellow, 1.0f);
        }
    }
}
