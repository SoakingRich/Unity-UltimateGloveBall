using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blockami.Scripts;
using Unity.Netcode;
using UnityEngine;

public class PlayerShotObject : NetworkBehaviour
{

    public NetworkVariable<int> NetColorID = new NetworkVariable<int>();
    public int ColorID => NetColorID.Value;
    public NetworkVariable<List<ulong>> AllPcs = new NetworkVariable<List<ulong>>();
    public NetworkVariable<bool> IsSuccess = new NetworkVariable<bool>();
    public NetworkVariable<bool> IsFailure = new NetworkVariable<bool>();
    public NetworkVariable<float> TotalScore = new NetworkVariable<float>();
    public NetworkVariable<bool> IsRight = new NetworkVariable<bool>();
   // public NetworkVariable<bool> IsRight = new NetworkVariable<bool>(writePerm: NetworkVariableWritePermission.Owner);
  public List<PlayerCubeScript> allPlayerCubeScripts = new List<PlayerCubeScript>(); 

   public bool HasFired;
    
    
    
    // public PlayerShotObject(BlockamiData.Instance.ColorType ct, List<ulong> allpcs, bool isSuccess, bool isFailure, float totalScore, bool isRight)
    // {
    //     MyColorType = ct;
    //     AllPcs.Value = allpcs;
    //     IsSuccess.Value = isSuccess;
    //     IsFailure.Value = isFailure;
    //     TotalScore.Value = totalScore;
    //     IsRight.Value = isRight;
    // }
    
    [ServerRpc(RequireOwnership = false)]
    public void FireShotServerRpc()
    {
        List<NetworkObject> siblingCubes = new List<NetworkObject>();
        
        foreach (var allPc in AllPcs.Value)
        {
            var AllObjs = NetworkManager.Singleton.SpawnManager.GetClientOwnedObjects(NetworkManager.Singleton.LocalClientId);
            foreach (var nObj in AllObjs)
            {
                if (nObj.NetworkObjectId == allPc)  // check if i playercube is present in AllClientOwnedObjects
                {
                    siblingCubes.Add(nObj);
                }
            }
                
        }

        foreach (var PlayerCube in siblingCubes)
        {
            PlayerCubeScript pcs = PlayerCube.GetComponent<PlayerCubeScript>();
            if (pcs)
            {
               // pcs.LaunchCubeServerRPC();     
                pcs.ShouldMove.Value = true;
                allPlayerCubeScripts.Add(pcs);
            }
        }

        HasFired = true;

    }
    
    
    public void SetLifeTime(float Lifetime)
    {
       Invoke("DestroyPlayerShot", Lifetime);
    }

    void DestroyPlayerShot()
    {
        if (!HasFired)
        {
            FireShotServerRpc();
        }
        
      //  GetNetworkObject(NetworkBehaviourId).Despawn(true);
        
    }
}
