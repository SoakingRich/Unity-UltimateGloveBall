using UnityEngine;
using UnityEditor; // Needed for editor functionality

[ExecuteInEditMode]  // Ensures the script runs in the editor as well
public class DebugObject : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        EnsureOnlyOneInstance();
    }

    // Update is called once per frame
    void Update()
    {
        // If running in the editor, check on every frame
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EnsureOnlyOneInstance();
        }
#endif
    }

    private void EnsureOnlyOneInstance()
    {
        // Find all instances of DebugObject in the scene
        DebugObject[] debugObjects = FindObjectsOfType<DebugObject>();

        // If there is more than one instance of DebugObject
        if (debugObjects.Length > 1)
        {
            // Destroy this instance if there are others in the scene
            foreach (var obj in debugObjects)
            {
                if (obj != this) // Exclude this instance
                {
                    DestroyImmediate(obj); // Use DestroyImmediate in editor for immediate removal
                    Debug.Log("Removed duplicate DebugObject: " + obj.gameObject.name);
                }
            }
        }
    }
}