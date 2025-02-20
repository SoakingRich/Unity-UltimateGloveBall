using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HeavyCubeBehavior : CubeBehavior
{
    
    protected override void OnEnable()
    {
        base.OnEnable();
        scs.HCHit += ScsOnHCHit;
    }
    protected override  void OnDisable()
    {
      base.OnDisable();
      scs.HCHit -= ScsOnHCHit;
    }
    
    private HashSet<ulong> trackedObjectIDs = new HashSet<ulong>();               // Store NetworkObject IDs

    
    private void ScsOnHCHit(SceneCubeNetworking obj)
    {
        RaycastHit[] hitinfo = Physics.RaycastAll(transform.position, Vector3.down);

        if (hitinfo.Length < 0) return;
        
        foreach (var hit in hitinfo)
        {
            var networkObject = hit.collider.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                trackedObjectIDs.Add(networkObject.NetworkObjectId); // Store ID
            }
        }
        
        var scs = hitinfo[0].collider.gameObject.GetComponent<SceneCubeNetworking>();
        if (scs != null)
        {
            scs.KillSceneCubeServerRpc();      // kill the first cube underneath so it can begin to fall
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        NetworkObject networkObject = collision.gameObject.GetComponent<NetworkObject>();
        if (networkObject != null && trackedObjectIDs.Contains(networkObject.NetworkObjectId))
        {
            Debug.Log("HeavyCube does CollisionEnter with a SCS ");
            SceneCubeNetworking cube = collision.gameObject.GetComponent<SceneCubeNetworking>();
            if (cube != null)
            {
                Debug.Log("HeavyCube kill SceneCube ");
                cube.KillSceneCubeServerRpc();
            }
        }
    }
    
    
    public override void OnIntialized()
    {
        scs.AvoidDestroyByPlayerCube = true;
    }
    
     
    public override void ResetCube()
    {
        trackedObjectIDs = new HashSet<ulong>();   
    }
    
    public override void ScsOnSCDied(SceneCubeNetworking obj)
    {
      
    }
    
    public override void ScsOnSCDiedByPlayerCube(SceneCubeNetworking obj,ulong clientID)
    {
      
        
        
    }
    
   

    
  
    
    
    
    
    
}
