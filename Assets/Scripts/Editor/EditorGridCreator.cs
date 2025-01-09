using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EditorGridCreator))]
public class EditorGridCreatorEditor : Editor

// E:\UnityProjectsE\Unity-UltimateGloveBall\Assets\EditorGridCreator.cs

{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Draws the default inspector

        // Add a button to create the grid
        if (GUILayout.Button("Create Grid"))
        {
            EditorGridCreator gridPrefabCreator = (EditorGridCreator)target;
            gridPrefabCreator.CreateGrid(); // Call CreateGrid when the button is pressed
        }
        
        // Add a button to clear the grid
        if (GUILayout.Button("Clear Grid"))
        {
            EditorGridCreator gridPrefabCreator = (EditorGridCreator)target;
            gridPrefabCreator.ClearGrid(); // Call ClearGrid when the button is pressed
        }
        
        // Add a button to clear the grid
        if (GUILayout.Button("Center Grid"))
        {
            EditorGridCreator gridPrefabCreator = (EditorGridCreator)target;
            gridPrefabCreator.CenterGrid(gridPrefabCreator.instantiatedPrefabs); // Call ClearGrid when the button is pressed
        }
    }
}