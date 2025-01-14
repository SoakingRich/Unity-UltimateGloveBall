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
        // var display = OVRManager.display;       
        // display.RecenteredPose += DisplayOnRecenteredPose;

        RecenterCount = OVRPlugin.GetLocalTrackingSpaceRecenterCount();
    }
    
    
    
    
    
    void Update()
    {
        
        int num = OVRPlugin.GetLocalTrackingSpaceRecenterCount();
        if (num > RecenterCount)
        {

            //   _ =  StartCoroutine(DisplayOnRecenteredPose());
        //    DisplayOnRecenteredPose();
          Invoke("DisplayOnRecenteredPose",0.1f);
       
        }

        RecenterCount = num;

    }




   public void DisplayOnRecenteredPose()
   {
      
        Debug.Log("Blockami Recenter Pose event occurred!!!");
        RecenterObj = LocalPlayerEntities.Instance?.LocalPlayerController?.OwnedDrawingGrid?.gameObject;


        // Find the object tagged "Recenter"
        GameObject recenterObject = RecenterObj;
        if (recenterObject == null)
        {
            Debug.LogWarning("No object with the tag 'Recenter' found.");
            return;
        }

        // Get the main camera
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("Main camera not found.");
            return;
        }


        OVRCameraRig CamRig = FindObjectOfType<OVRCameraRig>();

        // Extract the yaw rotation (world rotation around the Y-axis) for the camera
        Quaternion cameraYawRotation = Quaternion.Euler(0, mainCamera.transform.eulerAngles.y, 0);     // apparently Euler is always world yaw
        // Extract the yaw rotation for the recenter object
        Quaternion recenterYawRotation = Quaternion.Euler(0, recenterObject.transform.eulerAngles.y, 0);
        // Calculate the yaw rotation delta
        Quaternion yawRotationDelta = recenterYawRotation * Quaternion.Inverse(cameraYawRotation);
        // Apply the yaw rotation delta to the CamRig's rotation
        CamRig.transform.rotation = yawRotationDelta * transform.rotation;


        // Adjust the current object's position to account for the rotation change
        Vector3 positionDelta = recenterObject.transform.position - mainCamera.transform.position;
        positionDelta.y = 0;                                                                  // dont change height when recentering
     //   CamRig.transform.position += rotationDelta * positionDelta;
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
