using UnityEngine;

public static class UtilityLibrary
{
    // Method to find a component in parents
    public static T FindObjectInParents<T>(Transform startTransform) where T : Component
    {
        Transform currentTransform = startTransform;

        while (currentTransform != null)
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
    
    
    // Add other utility functions here as needed
}