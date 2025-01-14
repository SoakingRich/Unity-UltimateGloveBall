using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CB_HealthPickup : CubeBehavior
{
    private GameObject contentObj;
    private float speed = 1.0f;
    
    
    void Update()
    {
        contentObj.transform.localEulerAngles = new Vector3(contentObj.transform.localEulerAngles.x,contentObj.transform.localEulerAngles.y + Time.deltaTime * speed, contentObj.transform.localEulerAngles.z);
    }

    public override void ScsOnSCDied(SceneCubeNetworking obj)
    {
        
    }

    public override void OnIntialized()
    {
        
    }
   
}
