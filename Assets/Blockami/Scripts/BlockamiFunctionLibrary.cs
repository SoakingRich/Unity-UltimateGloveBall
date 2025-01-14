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
    
    // Add other utility functions here as needed
}