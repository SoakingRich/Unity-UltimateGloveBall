using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;


public class SnapZonesSnapPoseDelegate : MonoBehaviour, ISnapPoseDelegate
{
    
    public void TrackElement(int id, Pose p)
    {
     
    }

    public void UntrackElement(int id)
    {
      
    }

    public void SnapElement(int id, Pose pose)
    {
      
    }

    public void UnsnapElement(int id)
    {
      
    }

    public void MoveTrackedElement(int id, Pose p)
    {
     
    }

    public bool SnapPoseForElement(int id, Pose pose, out Pose result)
    {
        result = new Pose();
        return true;
    }
}
