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
    

    protected  virtual void OnEnable()
    {
        scs = GetComponent<SceneCubeNetworking>();
        scs.SCDied += ScsOnSCDied;
        scs.OnIntialized += OnIntialized;
        scs.SCDiedByPlayerCube += ScsOnSCDiedByPlayerCube;
    }
    
    protected  virtual void OnDisable()
    {
        scs = GetComponent<SceneCubeNetworking>();
        scs.SCDied -= ScsOnSCDied;
        scs.OnIntialized -= OnIntialized;
        scs.SCDiedByPlayerCube -= ScsOnSCDiedByPlayerCube;
    }
    
    
    
    

    public virtual void OnIntialized()
    {
       
    }
    
    public virtual void ScsOnSCDied(SceneCubeNetworking obj)
    {
      
    }
    
    public virtual void ScsOnSCDiedByPlayerCube(SceneCubeNetworking obj,ulong clientID)
    {
      
        
        
    }
    
    
    public virtual void ResetCube()
    {

    }
    
   
}
