using System;
using System.Collections.Generic;
using Blockami.Scripts;
using UnityEngine;
using Unity.Netcode;

[System.Serializable]
public struct SceneCubeData : INetworkSerializable
{

    public bool ContainsPickup;
    public bool IsHealthCube;
    
    public BlockamiData.ColorType MyColorType;

    public SceneCubeData(BlockamiData.ColorType ct, bool containsPickup, bool IsHealthCube)
    {
        MyColorType = ct;
        ContainsPickup = containsPickup;
        this.IsHealthCube = IsHealthCube;
       
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        // // Convert the Color to a Vector4 for serialization
        // BlockamiData.ColorType  ct = MyColorType;
        // serializer.SerializeValue(ref ct);
        // MyColorType = ct; // Convert back to Color after deserialization
    
        // Serialize the boolean
        serializer.SerializeValue(ref ContainsPickup);
    }
}



[System.Serializable]
public struct PlayerCubeData : INetworkSerializable
{

    public BlockamiData.ColorType MyColorType;
    public ulong OwningPlayerId;
    public int AIPlayerNum;

    public PlayerCubeData(BlockamiData.ColorType ct, ulong owningPlayerId,int AIPlayerNum)
    {
        MyColorType = ct;
        OwningPlayerId = owningPlayerId;
        this.AIPlayerNum = AIPlayerNum;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref MyColorType);
        serializer.SerializeValue(ref OwningPlayerId);
        serializer.SerializeValue(ref AIPlayerNum);
    }

}


[System.Serializable]
public class PlayerShot : INetworkSerializable, IEquatable<PlayerShot>
{

    public BlockamiData.ColorType MyColorType;
    public List<ulong> AllPcs; 
    public bool IsSuccess;
    public bool IsFailure;
    public float TotalScore;
    public bool IsRight;

    public PlayerShot(BlockamiData.ColorType ct, List<ulong> allpcs, bool isSuccess, bool isFailure, float totalScore, bool isRight)
    {
        MyColorType = ct;
        AllPcs = allpcs;
        IsSuccess = isSuccess;
        IsFailure = isFailure;
        TotalScore = totalScore;
        IsRight = isRight;
    }
    
    public PlayerShot()
    {
        AllPcs = new List<ulong>();
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        // serializer.SerializeValue(ref MyColorType);
        // serializer.SerializeValue(ref IsSuccess);
        // serializer.SerializeValue(ref IsFailure);
        // serializer.SerializeValue(ref TotalScore);
        //
        // // Serialize List<ulong>
        // int listCount = AllPcs.Count;
        // serializer.SerializeValue(ref listCount); // Serialize the list length first
        //
        // // Now manually serialize each element
        // for (int i = 0; i < listCount; i++)
        // {
        //     ulong item = AllPcs[i];  // Get the current item
        //     serializer.SerializeValue(ref item);  // Serialize it
        //     AllPcs[i] = item;  // Update the list with the serialized value (if necessary)
        // }
    }

    public void FireShot()
    {
        List<NetworkObject> siblingCubes = new List<NetworkObject>();
        
            foreach (var allPc in AllPcs)
            {
                var AllObjs = NetworkManager.Singleton.SpawnManager.GetClientOwnedObjects(NetworkManager.Singleton.LocalClientId);
                foreach (var nObj in AllObjs)
                {
                    if (nObj.NetworkObjectId == allPc)
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
                    pcs.LaunchCubeServerRPC();                           // launch cubes
                  
                }
            }
            
    }

    
    
    
    

    public bool Equals(PlayerShot other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return MyColorType.Equals(other.MyColorType) && Equals(AllPcs, other.AllPcs) && IsSuccess == other.IsSuccess && IsFailure == other.IsFailure && TotalScore.Equals(other.TotalScore) && IsRight == other.IsRight;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != this.GetType())
        {
            return false;
        }

        return Equals((PlayerShot)obj);
    }

    public override int GetHashCode() => HashCode.Combine(MyColorType, AllPcs, IsSuccess, IsFailure, TotalScore, IsRight);
}



