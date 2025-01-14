using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CB_EmojiCube : CubeBehavior
{
     
    
    
    void Update()
    {
        
    }

    public override void ScsOnSCDied(SceneCubeNetworking obj)
    {
        
    }

    public override void OnIntialized()
    {
        
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Staging"))
        {
            scs.KillSceneCubeServerRpc();
        }
    }
}
