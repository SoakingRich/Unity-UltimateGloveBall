using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DownCubeBehavior : CubeBehavior
{
    private Transform lowestSnapzone;
    public float Tolerance = 0.02f;
    
    
    
   public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        var RandomDrawingGrid = FindObjectOfType<DrawingGrid>();
        lowestSnapzone = RandomDrawingGrid.AllSnapZones[0].transform;
        scs = GetComponent<SceneCubeNetworking>();


    }

    
    void Update()
    {
        if (NearlyEquals(transform.position.y,lowestSnapzone.position.y,Tolerance))
        {
           
                if (scs == null || !scs.IsSpawned) return;


                DoDestroyCubeLayer();


        }
    }
    
   


    public void DoDestroyCubeLayer()
    {
        var allScs = SpawnManager.Instance.m_AllScs;
        SpawnManager.Instance.OnDownCubeLayerDestroy?.Invoke();
        var cubesToDestroy = new List<SceneCubeNetworking>();
        
        
        foreach (var sceneCubeNetworking in allScs)
        {
            if (NearlyEquals(sceneCubeNetworking.transform.position.y , lowestSnapzone.position.y,Tolerance))
            {
                cubesToDestroy.Add(sceneCubeNetworking);
             //   sceneCubeNetworking.LocalKillSceneCube();
            }
        }
        
        // Destroy after iteration
        foreach (var cube in cubesToDestroy)
        {
            cube.LocalKillSceneCube();
        }
    }
    
    
    
    
    
    
    public static bool NearlyEquals(float a, float b, float tolerance)
    {
        return Mathf.Abs(a - b) <= tolerance;
    }
    
}
