using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CubeBehavior : NetworkBehaviour
{
 
    
    //BallBehaviour
    //
    // [Header("Settings")]
    // [Header("State")]
    [Header("Internal")] 
    public SceneCubeNetworking scs;
    
    public virtual void ResetCube()
    {

    }


    private void OnEnable()
    {
        scs = GetComponent<SceneCubeNetworking>();
        scs.SCDied += ScsOnSCDied;
        scs.OnIntialized += OnIntialized;
    }

    public virtual void OnIntialized()
    {
       
    }

    void Start()
    {
       
    }

    public virtual void ScsOnSCDied(SceneCubeNetworking obj)
    {
      
    }
}
