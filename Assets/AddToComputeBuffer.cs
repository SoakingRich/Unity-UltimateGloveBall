using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class AddToComputeBuffer : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        StructuredBufferNoCompute sbnc = FindObjectOfType<StructuredBufferNoCompute>();
        if (sbnc) sbnc.AddTrackedObject(this.transform);
    }

    
    void Update()
    {

        if (!Application.isPlaying)
        {
            StructuredBufferNoCompute sbnc = FindObjectOfType<StructuredBufferNoCompute>();
            if (sbnc) sbnc.AddTrackedObject(this.transform);
        }
    }

    private void OnDestroy()
    {
        StructuredBufferNoCompute sbnc = FindObjectOfType<StructuredBufferNoCompute>();
        if(sbnc) sbnc.RemoveTrackedObject(this.transform);
    }
}
