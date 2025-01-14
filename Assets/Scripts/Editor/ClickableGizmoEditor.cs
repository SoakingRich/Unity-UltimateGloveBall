using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ClickableGizmo))]
public class ClickableGizmoEditor : Editor
{
    // This function is called when the scene is being rendered
    private void OnSceneGUI()
    {
        // // Get the object this editor script is attached to
        // ClickableGizmo clickableObject = (ClickableGizmo)target;
        //
        // // Draw a clickable handle at the object's position
        // Handles.color = Color.green; // Handle color
        // if (Handles.Button(clickableObject.transform.position, Quaternion.identity, 1f, 1f, Handles.SphereHandleCap))
        // {
        //     // The button was clicked (this acts like a gizmo click)
        //     Debug.Log("Gizmo clicked!");
        //
        //     // Optionally, perform any other actions here
        // }
        //
        // // Optionally, draw other custom gizmos for visualization
        // Handles.color = Color.red;
        // Handles.DrawWireDisc(clickableObject.transform.position, Vector3.up, 1f); // Draw a red disc
    }
}