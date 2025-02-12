using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Oddworm.Framework;
using ReadyPlayerMe;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DefaultExecutionOrder(2)]
public class EyeTracking : MonoBehaviour
{



    [Header("Settings")]
    public bool AllowAdvancedEyeTracking = true;
    public float DebugLineHeightOffset;
    public float ActualHeightOffset;
    public bool ShowLine;
    public bool inverse;
    public float DrawLineLength;
    public OVREyeGaze OVREyeRight;
    public OVREyeGaze OVREyeLeft;
    public Transform CenterEyeAnchor;
    [FormerlySerializedAs("rotatedObject")] public GameObject CentreEyeObject;
    
    [Header("State")]
    private bool ShouldUseAdvancedEyeTracking = false;
   [SerializeField] public SnapZone CurrentEyetrackedSnapZone;
    
    [Header("Internal")]
    public GameObject MeshRenderer;
    LineDrawer lineDrawer;


    private void Awake()
    {
        bool eyeTrackingSupported = OVRPlugin.eyeTrackingSupported;
        bool eyeTrackingEnabled = OVRPlugin.eyeTrackingEnabled;
        
        ShouldUseAdvancedEyeTracking = (eyeTrackingSupported&&eyeTrackingEnabled);
    }


    void Start()
    {

        #if UNITY_EDITOR
                
                SceneView sceneView = SceneView.lastActiveSceneView;     
                if (sceneView != null)
                {
                    sceneView.drawGizmos = true;
                }
        #endif

       lineDrawer = new LineDrawer(0.01f);


      if(!CentreEyeObject)  CentreEyeObject = new GameObject("RotatedObject");

        CentreEyeObject.transform.position = transform.position;
        CentreEyeObject.transform.rotation = Quaternion.identity;
        CentreEyeObject.transform.localScale = Vector3.one;

    }

    
    
    
    
    void Update()
    {
        if (!CentreEyeObject) return;

        Vector3 newPositionForCentreEye = new Vector3();
        var newRotationForCentreEye = Quaternion.identity;
        
        if (ShouldUseAdvancedEyeTracking)
        {
            Vector3 localPosition = OVREyeLeft.transform.localPosition;
            Quaternion localRotation = OVREyeLeft.transform.localRotation;

            Vector3 worldPosition = CenterEyeAnchor.TransformPoint(localPosition);
            Quaternion worldRotation = CenterEyeAnchor.transform.rotation * localRotation;

            Vector3 localPosition2 = OVREyeRight.transform.localPosition;
            Quaternion localRotation2 = OVREyeRight.transform.localRotation;

            Vector3 worldPosition2 = CenterEyeAnchor.TransformPoint(localPosition2);
            Quaternion worldRotation2 = CenterEyeAnchor.transform.rotation * localRotation2;


            newPositionForCentreEye = (worldPosition + worldPosition2) / 2;
            newPositionForCentreEye.y += ActualHeightOffset;
            newRotationForCentreEye = Quaternion.Slerp(worldRotation, worldRotation2, 0.5f);

        }
        else
        {
            newPositionForCentreEye = CenterEyeAnchor.position;
            newRotationForCentreEye = CenterEyeAnchor.rotation;
        }


       
        
        CentreEyeObject.transform.position = newPositionForCentreEye;     // Set positioned as per new calculated position

        CentreEyeObject.transform.rotation = newRotationForCentreEye;

        if (ShowLine)
        {
           var DebugLineStart = newPositionForCentreEye + new Vector3(0, DebugLineHeightOffset, 0);    // add the debug offset
            lineDrawer.DrawLineInGameView(DebugLineStart, CentreEyeObject.transform.position + CentreEyeObject.transform.forward * DrawLineLength, Color.blue, 0.01f);
        }
        

        //// LETS TRY TO AUTO HIGHLIGHT A SNAPZONE BASED ON WHAT SCENE CUBE PLAYER IS LOOKING AT

        Ray ray;
        Vector3 rayDirection = CentreEyeObject.transform.forward;
        float rayDistance = 100.0f; 
        ray = new Ray(CentreEyeObject.transform.position, rayDirection * rayDistance);
        
        int SceneCubeLayerMask = 1 << LayerMask.NameToLayer("Hitable");
        
        RaycastHit hitInfo;
        
        if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, SceneCubeLayerMask))
        {
            GameObject hitObject = hitInfo.collider.gameObject;
            var scs = hitObject.GetComponent<SceneCubeNetworking>();
            if (!scs)
            {
            Debug.Log("Hit something on the SceneCube layer!" + hitObject.name);
            }
        
        
            if( MeshRenderer!= null) MeshRenderer.transform.position = hitObject.transform.position;
            
            if(hitObject!=null) DoTraceFromTracedSceneCube(hitObject);
        
        }
    }








    void DoTraceFromTracedSceneCube(GameObject SceneCube) /// TRACE BACK TO A SCENE CUBE
    {

        SceneCubeNetworking sCs = SceneCube.GetComponent<SceneCubeNetworking>();
        if (sCs == null) sCs = SceneCube.transform.parent.GetComponent<SceneCubeNetworking>();
        if (sCs == null)
        {
            //     Debug.Log("tracing back from object on SceneCube layer " + obj.name);
            return;
        }

        Vector3 dirToPlayerFromSceneCube =
            (CenterEyeAnchor.transform.position - SceneCube.transform.position)
            .normalized; // dir to Player from SceneCube

        Vector3 dirCardinalDirSnapped =
            GetClosestCardinalDirection(dirToPlayerFromSceneCube,
                SceneCube); // snap dirToPlayerFromSceneCube to a CardinalDirection

        if (ShowLine)
        {
            lineDrawer.DrawLineInGameView(SceneCube.transform.position,
                SceneCube.transform.position + dirCardinalDirSnapped * DrawLineLength, Color.green, 0.01f);
        }

        Ray ray;
        float sphereRadius = 0.05f; 
        float rayDistance = 100.0f; 

        
        ray = new Ray(SceneCube.transform.position, dirCardinalDirSnapped * rayDistance);

        int snapZoneLayerMask = 1 << LayerMask.NameToLayer("Default"); 

       
        RaycastHit[] hitArray = Physics.SphereCastAll(ray, sphereRadius, rayDistance, snapZoneLayerMask);
        var snapzoneHits = hitArray.Where(s => s.collider.gameObject.GetComponent<SnapZone>() == true).ToArray();
       
        if (snapzoneHits.Length > 0)
        {
            GameObject FinalHitGameObject = snapzoneHits[0].collider.gameObject;
            SnapZone szs = FinalHitGameObject.GetComponentInParent<SnapZone>();

            if (szs != null)
            {
                CurrentEyetrackedSnapZone = szs;
                
                Vector3 size = Vector3.one * 0.05f;
                DbgDraw.WireSphere(CurrentEyetrackedSnapZone.transform.position, CurrentEyetrackedSnapZone.transform.rotation,size, Color.red);

                // if (!szs.ChildCubeSpawned && !Player.DrawingIsBlocked)
                // {
                //
                //  //   szs.SpawnChildCube();
                //
                //     lineDrawer.DrawLineInGameView(szs.transform.position, CenterEyeAnchor.transform.position, Color.red);
                // }
            }
        }

    }


    Vector3 GetClosestCardinalDirection(Vector3 direction, GameObject Obj)
    {
        Vector3[] cardinalDirections = { Obj.transform.forward, -Obj.transform.forward, -Obj.transform.right, Obj.transform.right };
        Vector3 closestDirection = Vector3.zero;
        float minAngle = float.MaxValue;

        foreach (Vector3 cardinalDir in cardinalDirections)
        {
            float angle = Vector3.Angle(direction, cardinalDir);
            if (angle < minAngle)
            {
                minAngle = angle;
                closestDirection = cardinalDir;
            }
        }

        return closestDirection;
    }
    
    
    
    
    
    // void FireByInvoke()
    // {
    //    Player.playerShootwithHandsMethod();
    // }
    //
    // void PunchOccurred()
    // {
    //     if(Player.currentlyDrawnObject.Count ==0)
    //     {
    //         Player.DrawByHandMethod();
    //  
    //     }
    //
    //     if (!CurrentEyetrackedSnapZone.ChildCubeSpawned)
    //     {
    //         // Spawn the child cube
    //         CurrentEyetrackedSnapZone.SpawnChildCube();
    //     }
    // }


    // void UpOrSidePunchOccurred()
    // {
    //     Player.playerShootwithHandsMethod();
    // }
    //
    // void OnDrawGizmos()
    // {
    //     //Gizmos.color = Color.red;
    //     //Gizmos.DrawSphere(transform.position, 1f);
    // }

}

