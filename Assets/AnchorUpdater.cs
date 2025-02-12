using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Meta.XR.MRUtilityKit;
using UnityEngine;

public class AnchorUpdater : MonoBehaviour
{


    public MRUK _mruk;
    public OVRCameraRig _cameraRig;
    
    
    void Start()
    {
        _mruk = FindObjectOfType<MRUK>();
        _cameraRig = FindObjectOfType<OVRCameraRig>();



    }

   
    void Update()
    {
        UpdateAnchors();
        
    }

    private void UpdateAnchors()
    {
        var MRUKRoom = _mruk.GetCurrentRoom();
        if (!MRUKRoom) return;

        foreach (var anch in MRUKRoom.Anchors)
        {
            var ovranch = anch.Anchor;

            if (!(ovranch.TryGetComponent(out OVRLocatable locatable) && locatable.IsEnabled))
            {
           //     Debug.Log("locatable is missing");
            }
            else
            {
                if (!locatable.IsEnabled)
                {
          //         Debug.Log("locatable is not enabled");
                }
                else
                {

                    
                    if (locatable.TryGetSceneAnchorPose(out var pose))
                    {
                        var position = pose.ComputeWorldPosition(_cameraRig.trackingSpace);
                        var rotation = pose.ComputeWorldRotation(_cameraRig.trackingSpace);
                        if (position.HasValue && rotation.HasValue)
                        {

                            anch.transform.position = position.Value;
                            anch.transform.rotation = rotation.Value;
                        }
                    }
                }

            }
        }
    }



    

    void TryThis()
    {
        // var MRUKRoom = _mruk.GetCurrentRoom();
        // var floorAnchor = MRUKRoom.FloorAnchor;
        // transform.position = floorAnchor.transform.position + Vector3.up * 0.5f;
        //
        // _mruk.SceneLoadedEvent += () =>
        // {
        //     var MRUKRoom = _mruk.GetCurrentRoom();
        //     var floorAnchor = MRUKRoom.FloorAnchor;
        //     transform.position = floorAnchor.transform.position + Vector3.up * 0.5f;
        // };
    }
        
        
        
        
      
    }

