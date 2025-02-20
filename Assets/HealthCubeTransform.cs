using System;
using System.Collections;
using System.Collections.Generic;
using Blockami.Scripts;
using UnityEngine;
using DG.Tweening;
using UltimateGloveBall.Arena.Gameplay;
using UltimateGloveBall.Arena.Services;

public class HealthCubeTransform : MonoBehaviour, IGamePhaseListener
{
    
    [Header("Settings")]
    public int HealthCubeIndex = 0;
    [SerializeField] GameObject ShineUIObject;
    
    //[Header("State")]
    public bool HasHealthCube => OwningHealthCube != null;
    
    [Header("Internal")]
    public SceneCubeNetworking OwningHealthCube;
    public DrawingGrid OwningDrawingGrid;
    public float GameTimer = 0.0f;
    
    public Action<HealthCubeTransform> OnHealthTransformHit;
    public HealthPillar _HealthPillar;


    private bool HideShineUI = false;

   



    void Awake()
    {
        
        
        OwningDrawingGrid = UtilityLibrary.FindObjectInDirectParents<DrawingGrid>(transform);
        if (!OwningDrawingGrid)
        {
            Debug.LogError("No Drawing Grid for HealthCubeTransform!!");
        }
    }
    
    private void OnDestroy()
    {
        GameManager._instance.UnregisterPhaseListener(this);
    }
    
    
    void Start()
    {
        GameManager._instance.RegisterPhaseListener(this);
      

#if !UNITY_EDITOR
            GetComponent<MeshRenderer>().enabled = false;
#endif


    }

    
    
    
    

    public void InitializeWithHealthCube(SceneCubeNetworking scs)
    {
        if (OwningHealthCube)
        {
            scs.SCDied -= OnSceneCubeDied;        // get rid of previous one, if new one is being assigned
        }
        else
        {
    //       Debug.Log("Spawning health cube for" + this.name);
        }
        
        OwningHealthCube = scs;
        scs.SCDied += OnSceneCubeDied;
        
        OwningHealthCube.m_healthCubeTransform = this;
        OwningHealthCube.HCHit += OnHealthCubeHit;

        if (!UtilityLibrary.IsWithEditor())
        {
            var meshRend = OwningHealthCube.Visual.GetComponent<MeshRenderer>();
            meshRend.enabled = false;
        }

      //  OwningHealthCube.transform.SetParent(this.transform);  not allowed



    }

    public virtual void OnHealthCubeHit(SceneCubeNetworking obj)
    {
        OnHealthTransformHit?.Invoke(this);
    }

    public void  SetShineUIActive(bool Enable, SceneCubeNetworking TargetCube = default, Vector3 newPosition = default)   
    {
        
        
        if (Enable)         // change the Y of the object
        {
            Vector3 newPos = ShineUIObject.transform.position;
            newPos.y = newPosition.y;
            ShineUIObject.transform.position = newPos;
            
            var allShineUIRenderers = ShineUIObject.GetComponentsInChildren<Renderer>();
            foreach (var rend in allShineUIRenderers)
            {
                Color col = BlockamiData.Instance.GetColorFromColorID(TargetCube.ColorID);
                rend.material.SetColor("_Color",col);
            }
        }

     //   if (ShineUIObject.gameObject.activeSelf == false && Enable)    // new Enable for shineui 
        if (Enable)    // new Enable for shineui 
        {
            ShineUIObject.gameObject.transform.DOKill();
            
            //ShineUIObject.gameObject.transform.localScale = Vector3.zero;
            ShineUIObject.SetActive(Enable);
            ShineUIObject.gameObject.transform.DOScale(0.1f, 1.0f);
        }
        
     //   if (ShineUIObject.gameObject.activeSelf == true && !Enable)    // new disable for shineui 
        if (!Enable)    // new disable for shineui 
        {
            ShineUIObject.gameObject.transform.DOKill();
            
            ShineUIObject.gameObject.transform.DOScale(0.0f, 1.0f).OnComplete(() =>
            {
                ShineUIObject.SetActive(Enable);
            });
        }
        
        
    }
    
    
    private void OnSceneCubeDied(SceneCubeNetworking destroyedCube)
    {
        OwningHealthCube = null;
      //  OnHealthCubeHit?.Invoke(this);
    }


    public void OnPhaseTimeCounter(double timeCounter)
    {
        if (timeCounter < 5.0f) HideShineUI = true;
        if(SpawnManager.Instance?.m_AllScs?.Count < 10) HideShineUI = true;
    }
    
    public void OnPhaseChanged(GameManager.GamePhase phase)
    {
       
    }

    public void OnPhaseTimeUpdate(double timeLeft)
    {
        
      // nothing
    }

    public void OnTeamColorUpdated(TeamColor teamColorA, TeamColor teamColorB)
    {
        
       
    }
}
