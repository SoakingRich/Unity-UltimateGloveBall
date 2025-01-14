using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIAvatarBlockami : MonoBehaviour
{

    [Header("Settings")] 
    [HideInInspector] public AIPlayer OwningAIPlayer;
    [HideInInspector] public Animator m_animator;
    public float LerpSpeed = 5.0f;
    [SerializeField] private GameObject RightshoulderTransform;
    [SerializeField] private GameObject LeftshoulderTransform;
    
    [Header("State")]
    public bool ikActive = false;

    public bool FromNetCurrentDrawingHandIsRight;
    public Vector3 FromNetLookAtLocation;
    public Vector3 FromNetLeftHandPos;
    public Vector3 FromNetRightHandPos;
    
    [Header("Internal")] 
    private Vector3 LastLookAtPosition;
    private Vector3 LastRightHandPosition;
    private float LastRightHandPositionWeight;
    private Vector3 LastLeftHandPosition;
    private float LastLeftHandPositionWeight;
    private Vector3 m_finalRightElbowPos;
    private Vector3 m_finalLeftElowPosition;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;  // You can change the color as needed
        Gizmos.DrawSphere(m_finalRightElbowPos, 0.1f);  // The second parameter controls the size of the sphere
        Gizmos.DrawSphere(m_finalLeftElowPosition, 0.1f);  // The second parameter controls the size of the sphere

    }


    void Start()
    {
        OwningAIPlayer = GetComponentInParent<AIPlayer>();
        m_animator = GetComponent<Animator>();
       
    }


    void OnAnimatorIK()
    {
        if (!ikActive || !OwningAIPlayer || !m_animator) return;

        MoveAvatarInFrontOfDraw();

        // m_animator.SetLookAtWeight(1);
            //
            // Vector3 finalPos = Vector3.Lerp(LastLookAtPosition,FromNetLookAtLocation, Time.deltaTime*LerpSpeed);
            // m_animator.SetLookAtPosition(finalPos);
            // LastLookAtPosition = finalPos;
        
            ///// :::::::::::::::::::::::::::::::DO RIGHT HAND::::::::::::::::::::::::::::::::::::::::
                        if (FromNetRightHandPos.magnitude != 0.0f)
                        {
                            float finalPosRightWeight = Mathf.Lerp(LastRightHandPositionWeight, 1.0f, Time.deltaTime * LerpSpeed);
                            m_animator.SetIKPositionWeight(AvatarIKGoal.RightHand, finalPosRightWeight);
                            LastRightHandPositionWeight = finalPosRightWeight;

                            Vector3 finalPosRight = Vector3.Lerp(LastRightHandPosition,FromNetRightHandPos, Time.deltaTime*LerpSpeed);
                            m_animator.SetIKPosition(AvatarIKGoal.RightHand, finalPosRight);
                            LastRightHandPosition = finalPosRight;

                            
                            Vector3 shoulderPosition = RightshoulderTransform.transform.position;
                            Vector3 direction = (LastRightHandPosition - shoulderPosition).normalized;
                            Vector3 localDirection = RightshoulderTransform.transform.InverseTransformDirection(direction);
                            Vector3 localPerpendicular = Quaternion.Euler(0, 0, -90) * localDirection;
                            m_finalRightElbowPos = RightshoulderTransform.transform.position +
                                                   RightshoulderTransform.transform.TransformDirection(localPerpendicular) *
                                                   0.3f;
                            m_animator.SetIKHintPosition(AvatarIKHint.RightElbow, m_finalRightElbowPos);
                            m_animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow,LastRightHandPositionWeight);

                            
                            
                            //m_animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
                            //    m_animator.SetIKRotation(AvatarIKGoal.RightHand,rightHandObj.rotation);
                        }
                        else
                        {
                            float finalPosRightWeight = Mathf.Lerp(LastRightHandPositionWeight, 0.0f, Time.deltaTime * LerpSpeed);
                            m_animator.SetIKPositionWeight(AvatarIKGoal.RightHand, finalPosRightWeight);
                            LastRightHandPositionWeight = finalPosRightWeight;
                            m_animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow,LastRightHandPositionWeight);
                        }
        
                        
                        
                        
                        
                        ///// :::::::::::::::::::::::::::::::DO LEFT HAND::::::::::::::::::::::::::::::::::::::::
                        if (FromNetLeftHandPos.magnitude != 0.0f)
                        {
                            // Set the left hand target position and weight, if one has been assigned
                            float finalPosLeftWeight = Mathf.Lerp(LastLeftHandPositionWeight, 1.0f, Time.deltaTime * LerpSpeed);
                            m_animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, finalPosLeftWeight);
                            LastLeftHandPositionWeight = finalPosLeftWeight;

                            Vector3 finalPosLeft = Vector3.Lerp(LastLeftHandPosition, FromNetLeftHandPos, Time.deltaTime * LerpSpeed);
                            m_animator.SetIKPosition(AvatarIKGoal.LeftHand, finalPosLeft);
                            LastLeftHandPosition = finalPosLeft;
                            
                            Vector3 leftShoulderPosition = LeftshoulderTransform.transform.position;
                            Vector3 leftDirection = (LastLeftHandPosition - leftShoulderPosition).normalized;
                            Vector3 leftLocalDirection = LeftshoulderTransform.transform.InverseTransformDirection(leftDirection);
                            Vector3 leftLocalPerpendicular = Quaternion.Euler(0, 0, 90) * leftLocalDirection;  // 90-degree rotation in Y axis
                            Vector3 leftWorldPerpendicular = LeftshoulderTransform.transform.TransformDirection(leftLocalPerpendicular);
                            m_finalLeftElowPosition = LeftshoulderTransform.transform.position + leftWorldPerpendicular * 0.3f;
                            m_animator.SetIKHintPosition(AvatarIKHint.LeftElbow, m_finalLeftElowPosition);
                            m_animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow,LastLeftHandPositionWeight);
                        }
                        else
                        {
                            float finalPosLeftWeight = Mathf.Lerp(LastLeftHandPositionWeight, 0.0f, Time.deltaTime * LerpSpeed);
                            m_animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, finalPosLeftWeight);
                            LastLeftHandPositionWeight = finalPosLeftWeight;
                            m_animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow,LastLeftHandPositionWeight);
                        }
        
          
    }

    private void MoveAvatarInFrontOfDraw()
    {
        ////// :::::::::::::::::: TRY TO MOVE AVATAR INFRONT OF DRAW LOCATION

        if (FromNetLeftHandPos == Vector3.zero && FromNetRightHandPos == Vector3.zero) return;
        
        Vector3 pos1 = FromNetRightHandPos == Vector3.zero ? FromNetLeftHandPos : FromNetRightHandPos;
        
        Vector3 forward = transform.right;
        Vector3 currentPosition = transform.position;
        Vector3 pos2 = currentPosition + Vector3.Project(pos1 - currentPosition, forward);

        transform.position = Vector3.MoveTowards(transform.position, pos2, Time.deltaTime * LerpSpeed * 0.3f);
    }

    void Update()
    {
        
    }
    
    
}
