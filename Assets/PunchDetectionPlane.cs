using System;
using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction.Input;
using Oculus.Interaction.MoveFast;
using UnityEngine;
using UnityEngine.Events;

public class PunchDetectionPlane : MonoBehaviour
{
    
    [Header("Settings")]
    [SerializeField] private double _minTimeBetweenHits  = 0.6f;
    [SerializeField] int _poseFrameTolerance = 3;
    
    [Header("State")]
    [SerializeField] private bool _canHit = true;
    
   // [SerializeField] ReferenceActiveState _canHit;
   
    [SerializeField] private double _lastHitTime;
    public UnityEvent onHitEvent;
    public Action onHit;
    [SerializeField] private bool PoseWasCorrect;
    
    [Header("Internal")]
  public IHand LastHand;


  
  

    void Start()
    {
        var meshRend = GetComponent<MeshRenderer>();
        if (meshRend != null)
        {
            meshRend.enabled = false;
        }
        
        
        var allTPE = FindObjectsOfType<TriggerPinchEvents>();
        foreach (var tpe in allTPE)
        {
            tpe.BindToPunchDetection(this);
        }
    }
    
    
    
    

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var TPE = other.GetComponent<TriggerPinchEvents>();
            TestForHitPose(TPE.m_hand);
        }
    }

    
    
    

    private void TestForHitPose(IHand hand)
    {
        if (!_canHit) return;
        if (Time.time - _lastHitTime < _minTimeBetweenHits) return;

        _lastHitTime = Time.time;
        LastHand = hand;
        
        IngameHit(hand);
        
    }

    
    
    
    
    private void IngameHit(IHand hand)
    {
        bool hasPoseList = hand.TryGetAspect<HandPoseActiveStateList>(out var handPoseList);
        
        // // hand poses are not available, probably a controller, just say it was posed right
        // if (!hasPoseList)
        // {
        //     ResolveHit(hand, true);
        //     return;
        // }
        //
        // StartCoroutine(Tolerance());
        // IEnumerator Tolerance()
        // {
        //     var handPoseActiveState = handPoseList.Get(_poseName);
        //     if (handPoseActiveState != null)
        //     {
        //         for (int i = 0; i < _poseFrameTolerance; i++)
        //         {
        //             // hand was in the right pose \o/
        //             if (handPoseActiveState.Active)
        //             {
        //                 ResolveHit(hand, true);
        //                 yield break;
        //             }
        //             yield return null;
        //         }
        //     }
        //     ResolveHit(hand, false);
        // }
        
        ResolveHit(hand, true);
        
    }

    
    

    void ResolveHit(IHand hand, bool poseCorrect)
    {
        PoseWasCorrect = poseCorrect;

        if (poseCorrect)
        {
            onHitEvent?.Invoke();
            onHit?.Invoke();
        }

    }

}
