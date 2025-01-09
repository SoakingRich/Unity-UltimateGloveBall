using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// E:\UnityProjectsE\Unity-UltimateGloveBall\Assets\Scripts\Editor\EditorGridCreator.cs

public class EditorGridCreator : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private int width = 6;
    [SerializeField] private int height = 5;
    [SerializeField] private int depth = 3;
    [SerializeField] private float spacing = 2f;

    public enum GridDirection { LocalZ, LocalY, LocalX }

    [SerializeField] private GridDirection gridDirection = GridDirection.LocalZ;
    [SerializeField] public List<GameObject> instantiatedPrefabs;

    
    
    
    

    public void CreateGrid()
    {

        // Validate prefab assignment
        if (prefab == null)
        {
            Debug.LogError("Prefab is not assigned.");
            return;
        }

        // Calculate grid center offset
        Vector3 startPosition = transform.position;

        // Initialize index for naming instances
        int index = 1;

        // Loop through grid dimensions
        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector3 finalPosition = startPosition;

                    switch (gridDirection)
                    {
                        case GridDirection.LocalZ:
                            finalPosition += transform.up * (y * spacing) + transform.right * (x * spacing) + transform.forward * (z * spacing);
                            break;
                        case GridDirection.LocalY:
                            finalPosition += transform.right * (x * spacing) + transform.forward * (y * spacing) + transform.up * (z * spacing);
                            break;
                        case GridDirection.LocalX:
                            finalPosition += transform.forward * (x * spacing) + transform.right * (y * spacing) + transform.up * (z * spacing);
                            break;
                    }


                    // Instantiate prefab and apply position
                    if (PrefabUtility.IsPartOfPrefabAsset(prefab))
                    {
                        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                        if (instance != null)
                        {
                            instance.transform.SetParent(transform);
                            instance.transform.position = finalPosition;
                            instance.name = $"{prefab.name}_{index}";
                            Debug.Log($"Instantiating prefab {prefab.name} at {finalPosition}");

                            index++;
                            instantiatedPrefabs.Add(instance);
                           var sZ = instance.GetComponent<SnapZone>();
                           if (sZ)
                           {
                               sZ.Coords = new Vector3(x, y, z);
                           }
                            
                            EditorSceneManager.MarkSceneDirty(instance.scene);
                        }
                    }
                    else
                    {
                        Debug.LogError("Prefab is not a valid prefab asset.");
                    }
                }
            }
        }

    



// Now, adjust the positions to center the grid
        CenterGrid(instantiatedPrefabs);
    }


    public void ClearGrid()
    {
        if (prefab == null)
        {
            Debug.LogError("Prefab is not assigned.");
            return;
        }

        List<GameObject> childrenToDestroy = new List<GameObject>();

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child != null && PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject) == prefab)
            {
                childrenToDestroy.Add(child.gameObject);
            }
        }

        foreach (GameObject child in childrenToDestroy)
        {

            DestroyImmediate(child);

        }

        instantiatedPrefabs.Clear();
    }

    public void CenterGrid(List<GameObject> instantiatedPrefabs)
    {
    
        if (instantiatedPrefabs.Count == 0)
            return;

        // Calculate the bounding box of the grid
        Vector3 minPosition = instantiatedPrefabs[0].transform.position;
        Vector3 maxPosition = instantiatedPrefabs[0].transform.position;

        foreach (var prefab in instantiatedPrefabs)
        {
            Vector3 prefabPosition = prefab.transform.position;
            minPosition = Vector3.Min(minPosition, prefabPosition);
            maxPosition = Vector3.Max(maxPosition, prefabPosition);
        }

        // Find the center of the bounding box
        Vector3 center = (minPosition + maxPosition) / 2;

        // Calculate the offset to center the grid around the starting position
        Vector3 offset = transform.position - center;

        // Apply the offset to each instantiated prefab
        foreach (var prefab in instantiatedPrefabs)
        {
            prefab.transform.position += offset;
        }
    }
}