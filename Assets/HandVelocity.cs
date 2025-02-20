using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blockami.Scripts;
using Oculus.Interaction;
using Oculus.Interaction.Throw;
using UnityEngine;

public class HandVelocity : MonoBehaviour
{
    
    
    [Header("Settings")]
    // [SerializeField] private float fromMin;
    // [SerializeField] private float fromMax;
   private float resetThreshold = 0.8f;
   // [SerializeField] public float punchCooldown = 1.0f;
    [SerializeField] public float camHandAlignmentDotCheck = 0.75f;
    [SerializeField] public bool DebugSpeed;
    [SerializeField] public bool IsRight;
    [SerializeField] public EyeTracking _eyeTracking;
    
    [Header("State")]
    [SerializeField] public float lastPunchTime;
    [SerializeField] private bool canPunch = true;
    
    public Action<bool,Transform> OnStraightPunch;
    public Action<bool,Transform> OnLeftHook;
    public Action<bool,Transform> OnRightHook;
    public Action<bool,Transform> OnUppercut;
    
    [Header("Internal")]
    private LineDrawer lineDraw;
    
    //create the velocity calculator with a small memory to store
    //the last few pose samples
    RANSACVelocity _velocityCalculator = new RANSACVelocity(10, 2);


    private void Start()
    {
        lineDraw = new LineDrawer(0.02f);
        
        
        _eyeTracking = FindObjectOfType<EyeTracking>();
        
        if (_eyeTracking == null || _eyeTracking.enabled == false)
        {
            enabled = false;
        }

        resetThreshold = BlockamiData.Instance.punchThreshold * 0.8f;
    }
    
    
    
    private void OnEnable()
    {
        //reset the calculation everytime you enable the component
        _velocityCalculator.Initialize();
    }
 
    
    
    private void Update()
    {
        if (!BlockamiData.Instance.BoxingEnabled) return;
        
        _velocityCalculator.Process(this.transform.GetPose(), Time.time, true);


        if (!Camera.main) return;
        
        Vector3 velocity = GetVelocity();
        float speed = velocity.magnitude;
        
        if(DebugSpeed)
        {
            Debug.Log("Speed: " + speed);
        }
     
        
     
        if (speed < resetThreshold)
        {
            canPunch = true;
        }

       
        if (!canPunch || speed < BlockamiData.Instance.punchThreshold)
            return;

        
     
        Vector3 cameraForward = Camera.main.transform.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();

        
        Vector3 velocityDir = velocity.normalized;

      
        Vector3 straightPunch = cameraForward;
        Vector3 leftHook = Quaternion.Euler(0, 90, 0) * cameraForward;
        Vector3 rightHook = Quaternion.Euler(0, -90, 0) * cameraForward;
        Vector3 uppercut = Vector3.up;
        Vector3 back = cameraForward * -1;

        // Compare using dot product
        float dotStraight = Vector3.Dot(velocityDir, straightPunch);
        float dotLeftHook = Vector3.Dot(velocityDir, leftHook);
        float dotRightHook = Vector3.Dot(velocityDir, rightHook);
        float dotUppercut = Vector3.Dot(velocityDir, uppercut);
        float dotBack = Vector3.Dot(velocityDir, back);

        // Store dot products in a dictionary for easy lookup
        Dictionary<string, float> punchTypes = new Dictionary<string, float>
        {
            { "Straight Punch", dotStraight },
            { "Left Hook", dotLeftHook },
            { "Right Hook", dotRightHook },
            { "Uppercut", dotUppercut },
            { "Back", dotBack }
        };

        // Check if the controller is roughly aligned with the camera's forward direction
        Vector3 cameraToController = (this.transform.position - Camera.main.transform.position).normalized;
        cameraToController = Vector3.Scale(cameraToController, new Vector3(1.0f, 0.0f, 1.0f));
        float cameraDot = Vector3.Dot(cameraToController, Camera.main.transform.forward);
        if (cameraDot < camHandAlignmentDotCheck) 
        {
           // Debug.Log("Controller is not in front of the camera, not a punch.");
            return; // Early return, no punch detected
        }
        
// Find the punch type with the highest dot value
        string detectedPunch = punchTypes.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;

        Debug.Log("PunchDetected " + detectedPunch);
        
        switch (detectedPunch)
        {
            case "Back" : return;
            
            case "Straight Punch":
                OnStraightPunch?.Invoke(IsRight, this.transform);
                break;
            case "Left Hook":
                OnLeftHook?.Invoke(IsRight, this.transform);
                break;
            case "Right Hook":
                OnRightHook?.Invoke(IsRight, this.transform);
                break;
            case "Uppercut":
                OnUppercut?.Invoke(IsRight, this.transform);
                break;
           
                    
        }
        
     
        
        canPunch = false;
        lastPunchTime = Time.time;

        // Start cooldown coroutine
        StartCoroutine(PunchCooldownRoutine());
    }
    
    
    private IEnumerator PunchCooldownRoutine()
    {
        yield return new WaitForSeconds(BlockamiData.Instance.punchCooldown);
        canPunch = true;  // Reset punch detection after cooldown
    }
 
    public Vector3 GetVelocity()
    {
        //Call this to get the filtered velocity of the controller
        _velocityCalculator.GetVelocities(out Vector3 velocity, out Vector3 torque);
        return velocity;
    }
}

