using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public static class UtilityLibrary
{

    public static bool IsWithEditor()
    {
        
        
#if UNITY_EDITOR
        return true;
        
#endif
        return false;
    }

    public static bool IsClient()
    {
        return NetworkManager.Singleton.IsClient;
    }

    public static void DebugServerForClientID(string msg, ulong id, bool IsError = false)
    {
        if (!NetworkManager.Singleton.IsHost) return;
        if (NetworkManager.Singleton.LocalClientId == id) return;

        var message = "Blockami Log - SERVER for ID - " + msg;
        
        if (IsError)
        {
            Debug.LogError(message);
        }
        else
        {
            Debug.Log(message);
        }
         
    }
    
    
    public static void DebugLogClient(string msg, bool IsError = false)
    {
        if (!(NetworkManager.Singleton.IsClient & !NetworkManager.Singleton.IsHost)) return;

        var message = "Blockami Log - CLIENT - " + msg;
        
        if (IsError)
        {
            Debug.LogError(message);
        }
        else
        {
            Debug.Log(message);
        }
         
    }
    
  public  static bool SceneCubeMatchesID(int ColorID, SceneCubeNetworking scs, out bool WasError)
    {
        WasError = false;
        if (!scs) return false;

        if (scs.IsErrorCube)
        {
            WasError = true;
            return false;
        }
        
        if (scs.ColorID == ColorID)
        {
            return true;
        }
        
        if (scs.ColorID == 10 || scs.ColorID == 12)   // rainbow or heavy
        {
            return true;
        }

        return false;
    }
    
    
    
    // Method to find a component in parents
    public static T FindObjectInDirectParents<T>(Transform startTransform) where T : Component
    {
        Transform currentTransform = startTransform;

        while (currentTransform != null)      // if transform is null, we've hit root
        {
            T component = currentTransform.GetComponent<T>();
            if (component != null)
            {
                return component;
            }

            currentTransform = currentTransform.parent;
        }

        return null;  // Return null if the component is not found
    }
    
    public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        float t = Mathf.InverseLerp(fromMin, fromMax, value); // Normalize to 0-1 range
        return Mathf.Lerp(toMin, toMax, t); // Map to new range
    }

    public static Component GetNearestObject(Component[] components, Vector3 origin)
    {
        Component nearestComponent = components
            .Where(comp => comp.gameObject.activeInHierarchy) // Ensure the object is active
            .OrderBy(comp => Vector3.SqrMagnitude(comp.transform.position - origin)) // Use squared magnitude for efficiency
            .FirstOrDefault();
        
        return nearestComponent;
    }
    
    public static T GetNearestObjectFromList<T>(List<T> components, Vector3 origin) where T : Component
    {
        return components
            .Where(comp => comp.gameObject.activeInHierarchy) // Ensure the object is active
            .OrderBy(comp => Vector3.SqrMagnitude(comp.transform.position - origin)) // Use squared magnitude for efficiency
            .FirstOrDefault();
    }
}