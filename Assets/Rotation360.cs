using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
public class Rotation360 : MonoBehaviour
{

    public float AnglePerFrame = 1.0f;


    private void Update()
    {
        
        transform.Rotate(Vector3.up, AnglePerFrame);
    }
    
}
