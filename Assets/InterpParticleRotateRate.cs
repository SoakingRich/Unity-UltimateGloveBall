using System.Collections;
using UnityEngine;

public class InterpParticleRotateRate : MonoBehaviour
{
    public float MoveToSpeed = 1.0f;          // Speed of interpolation
    public float Duration = 2.0f;             // Duration to hold at 1.0f before moving back to 0.0f

    private ParticleSystem _particleSystem;   // Reference to the ParticleSystem
    private float _rotationMask = 0.0f;       // Current rotation mask value
    private bool _isInterpolating = false;    // Flag to track if interpolation is in progress
    private float _targetValue = 0.0f;        // Target value for interpolation
    private float _durationTimer = 0.0f;      // Timer to track the duration while holding at 1.0f

    void Start()
    {
        // Get the ParticleSystem component on this GameObject
        _particleSystem = GetComponent<ParticleSystem>();
        if (_particleSystem == null) { Debug.LogError("No ParticleSystem found on the GameObject."); }
        
        InvokeRepeating("Trigger", 0.0f, 3.0f);
    }

    public void Trigger()
    {
        // Start the interpolation towards 1.0 when triggered
        _targetValue = 1.0f;
        _isInterpolating = true;
      //  Debug.Log("Trigger: Started interpolation towards 1.0");
        _durationTimer = 0.0f;
    }

    void Update()
    {
        if (_isInterpolating)
        {
            // Interpolate the rotationMask towards the target value (1.0f or 0.0f)
            _rotationMask = Mathf.MoveTowards(_rotationMask, _targetValue, MoveToSpeed * Time.deltaTime);

            // Apply the rotation to the Particle System
            var rotationOverLifetime = _particleSystem.rotationOverLifetime;
            if (rotationOverLifetime.enabled)
            {
                rotationOverLifetime.yMultiplier = _rotationMask;  // Apply the interpolation value
            }
            
            // // Debugging the interpolation progress
            // Debug.Log("Update: _rotationMask = " + _rotationMask);
            // Debug.Log("Update: _targetValue = " + _targetValue);
            // Debug.Log("Update: rotationOverLifetime.yMultiplier = " + rotationOverLifetime.yMultiplier);

            // If we reach the target value (1.0f or 0.0f), handle the next step
            if (Mathf.Approximately(_rotationMask, _targetValue))
            {
                if (_targetValue == 1.0f && _durationTimer == 0.0f) 
                {
                    // Hold at 1.0f for the specified duration
                    //Debug.Log("Update: Reached 1.0f, holding.");
                    StartCoroutine(HoldAtMaxValue());
                }
                else if (_targetValue == 0.0f)
                {
                    // Reset to start the process again if necessary
                    _isInterpolating = false;
                   // Debug.Log("Update: Reached 0.0f, interpolation completed.");
                }
            }
        }
    }

    private IEnumerator HoldAtMaxValue()
    {
        _durationTimer = 0.0f;

        // Hold the value at 1.0f for the specified duration
        while (_durationTimer < Duration)
        {
            _durationTimer += Time.deltaTime;
         //   Debug.Log("HoldAtMaxValue: _durationTimer = " + _durationTimer);
            yield return null;
        }

        // After holding, start interpolating back to 0.0f
        _targetValue = 0.0f;
        Debug.Log("HoldAtMaxValue: Duration reached, starting interpolation back to 0.0f.");
    }
}
