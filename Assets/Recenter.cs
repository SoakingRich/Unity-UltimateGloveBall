using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction.MoveFast;
using UltimateGloveBall.Arena.Services;
using UnityEngine;

public class Recenter : MonoBehaviour
{
    private int RecenterCount = 1;


    public GameObject RecenterObj;
    
    
    
    void Start()
    {
        RecenterCount = OVRPlugin.GetLocalTrackingSpaceRecenterCount();
    }
    
    
    
    
    
    void Update()
    {
        int num = OVRPlugin.GetLocalTrackingSpaceRecenterCount();
        if (num > RecenterCount)
        {
          Invoke("DisplayOnRecenteredPose",0.1f);
        }

        RecenterCount = num;

    }




   public void DisplayOnRecenteredPose()
   {
      
        Debug.Log("Blockami Log - Recenter Pose event occurred!!!");
        RecenterObj = LocalPlayerEntities.Instance?.LocalPlayerController?.OwnedDrawingGrid?.gameObject;   // recenter object is players OwningGrid
        
        GameObject recenterObject = RecenterObj;
        if (recenterObject == null)
        {
            Debug.LogWarning("No object with the tag 'Recenter' found."); return;
        }
       
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("Main camera not found."); return;
        }


        OVRCameraRig CamRig = FindObjectOfType<OVRCameraRig>();

     //    // Extract the yaw rotation (world rotation around the Y-axis) for the camera
     //    Quaternion cameraYawRotation = Quaternion.Euler(0, mainCamera.transform.eulerAngles.y, 0);     // apparently Euler is always world yaw
     //    // Extract the yaw rotation for the recenter object
     //    Quaternion recenterYawRotation = Quaternion.Euler(0, recenterObject.transform.eulerAngles.y, 0);
     //    // Calculate the yaw rotation delta
     //    Quaternion yawRotationDelta = recenterYawRotation * Quaternion.Inverse(cameraYawRotation);
     //    // Apply the yaw rotation delta to the CamRig's rotation
     //    CamRig.transform.rotation = yawRotationDelta * transform.rotation;
     //
     //
     //    // Adjust the current object's position to account for the rotation change
     //    Vector3 positionDelta = recenterObject.transform.position - mainCamera.transform.position;
     //    positionDelta.y = 0;                                                                  // dont change height when recentering
     // //   CamRig.transform.position += rotationDelta * positionDelta;
     //    CamRig.transform.position += positionDelta;
     //
     //    Debug.Log("Object position and rotation adjusted to account for recenter.");
     //
     //
     //    var height = FindObjectOfType<HeightAdjustment>();
     //    if (height)
     //    {
     //        height.SetHeight();
     //        Debug.Log("Set Height on Recenter");
     //    }
     //    
        
        
        // Corrected logic: always set rotation explicitly relative to recenterObject's forward
        Vector3 recenterForward = new Vector3(RecenterObj.transform.forward.x, 0, RecenterObj.transform.forward.z);
        Quaternion targetRotation = Quaternion.LookRotation(recenterForward, Vector3.up);
    
        CamRig.transform.rotation = targetRotation;

        // Adjust position to maintain alignment
        Vector3 positionDelta = RecenterObj.transform.position - mainCamera.transform.position;
        positionDelta.y = 0; // Don't modify height
        CamRig.transform.position += positionDelta;

        Debug.Log("Object position and rotation adjusted to account for recenter.");

        var height = FindObjectOfType<HeightAdjustment>();
        if (height)
        {
            height.SetHeight();
            Debug.Log("Set Height on Recenter");
        }
    }



}
