using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthCubeTransform : MonoBehaviour
{
    
    [Header("Settings")]
    public int HealthCubeIndex = 0;
    [SerializeField] GameObject ShineUIObject;
    
    //[Header("State")]
    public bool HasHealthCube => OwningHealthCube != null;
    
    [Header("Internal")]
    public SceneCubeNetworking OwningHealthCube;
    public DrawingGrid OwningDrawingGrid;
   
    
    public  Action<HealthCubeTransform> OnHealthCubeDied;
    
    




    void Awake()
    {
        OwningDrawingGrid = UtilityLibrary.FindObjectInParents<DrawingGrid>(transform);
        if (!OwningDrawingGrid)
        {
            Debug.LogError("No Drawing Grid for HealthCubeTransform!!");
        }
    }
    
    
    void Start()
    {

#if !UNITY_EDITOR
            GetComponent<MeshRenderer>().enabled = false;
#endif
        
        
    }

    
    
    
    

    public void IntializeWithHealthCube(SceneCubeNetworking scs)
    {
        if (OwningHealthCube)
        {
            scs.SCDied -= OnSceneCubeDied;
        }
        else
        {
    //       Debug.Log("Spawning health cube for" + this.name);
        }
        
        OwningHealthCube = scs;
        scs.SCDied += OnSceneCubeDied;
        
    }

    public void  SetShineUIActive(bool Enable, Vector3 newPosition = default)
    {
        ShineUIObject.SetActive(Enable);
        
        if (Enable)         // change the Y of the object
        {
            Vector3 newPos = ShineUIObject.transform.position;
            newPos.y = newPosition.y;
            ShineUIObject.transform.position = newPos;
        }
    }
    
    
    private void OnSceneCubeDied(SceneCubeNetworking destroyedCube)
    {
        OwningHealthCube = null;
        OnHealthCubeDied?.Invoke(this);
    }
    

    
}
