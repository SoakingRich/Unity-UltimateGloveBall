using System;
using System.Collections.Generic;
using Blockami.Scripts;
using UnityEngine;
using Unity.Netcode;

[System.Serializable]
public struct SceneCubeData /*: INetworkSerializable*/
{

   public Color myColor;
   public NetworkObject SceneCubePrefab;
    

    public static readonly SceneCubeData Default = new SceneCubeData();
   
    public SceneCubeData( Color c, NetworkObject SCPrefab)
    {
       myColor = c;
       SceneCubePrefab = SCPrefab;

    }
    
    
    public static bool operator ==(SceneCubeData left, SceneCubeData right)
    {
   
        return left.Equals(right);
    }

    public static bool operator !=(SceneCubeData left, SceneCubeData right)
    {
        return !(left == right); // Use the == operator
    }

    public override bool Equals(object obj)
    {
        if (obj is SceneCubeData)
        {
            SceneCubeData other = (SceneCubeData)obj;
            return this.myColor == other.myColor &&
                   SceneCubePrefab == other.SceneCubePrefab;
        }
        return false;
    }
    
    
}





[System.Serializable]
public struct PlayerCubeData : INetworkSerializable
{
    public int ColorID;
    public ulong OwningPlayerId;
    public int AIPlayerNum;

    public static readonly PlayerCubeData Default = new PlayerCubeData();
    
    public PlayerCubeData(int ColID, ulong owningPlayerId,int AIPlayerNum)
    {
        ColorID = ColID;
        OwningPlayerId = owningPlayerId;
        this.AIPlayerNum = AIPlayerNum;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ColorID);
        serializer.SerializeValue(ref OwningPlayerId);
        serializer.SerializeValue(ref AIPlayerNum);
    }
    
    public static bool operator ==(PlayerCubeData left, PlayerCubeData right)
    {
        // Compare fields for equality (adjust as necessary for your struct)
        return left.Equals(right);
    }

    public static bool operator !=(PlayerCubeData left, PlayerCubeData right)
    {
        return !(left == right); // Use the == operator
    }

    public override bool Equals(object obj)
    {
        if (obj is PlayerCubeData)
        {
            PlayerCubeData other = (PlayerCubeData)obj;

            // Compare all fields of the struct
            return this.ColorID == other.ColorID &&
                   this.OwningPlayerId == other.OwningPlayerId &&
                   this.AIPlayerNum == other.AIPlayerNum;
        }
        return false;
    }

}







[System.Serializable]
public class PlayerShot : INetworkSerializable, IEquatable<PlayerShot>
{

   
    public List<ulong> AllPcs; 
    public bool IsSuccess;
    public bool IsFailure;
    public float TotalScore;
    public bool IsRight;

    public PlayerShot(List<ulong> allpcs, bool isSuccess, bool isFailure, float totalScore, bool isRight)
    {
      //  MyColorType = ct;
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

        return Equals(AllPcs, other.AllPcs) && IsSuccess == other.IsSuccess && IsFailure == other.IsFailure && TotalScore.Equals(other.TotalScore) && IsRight == other.IsRight;
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

    public override int GetHashCode() => HashCode.Combine(AllPcs, IsSuccess, IsFailure, TotalScore, IsRight);
}



