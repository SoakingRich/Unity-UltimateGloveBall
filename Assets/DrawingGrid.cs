using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Oculus.Interaction;
using UltimateGloveBall.Arena.Gameplay;
using UltimateGloveBall.Arena.Services;
using Unity.Netcode;
using UnityEngine;

public class DrawingGrid : NetworkBehaviour
{

    [Header("Settings")] 
    public int DrawingGridIndex;
    public Vector3 MoveDirection = Vector3.forward;
    public bool OnlyShowFirstLayerSnapZones = true;
    public bool AIPlayerIsActive;
    public AIPlayer m_AIPlayer;
    
    [Header("State")]
    public NetworkVariable<ulong> OwningPlayer;
    public bool _IsLocal;
    
    [Header("Internal")]
    public List<SnapZone> AllSnapZones = new List<SnapZone>();
    public DrawPointerUI PointerUI;
    public List<HealthCubeTransform> AllHealthCubeTransforms;
    public GameManager m_gameManager => GameManager.Instance;
    public GameManager.GamePhase _gamePhase => m_gameManager.CurrentPhase;
 



    
    
    
    
    private void Awake()
    {
      
        if (PointerUI) PointerUI.OwningDrawingGrid = this;
        
      MoveDirection = transform.forward;
        AllSnapZones = new List<SnapZone>(GetComponentsInChildren<SnapZone>());
        AllHealthCubeTransforms = new List<HealthCubeTransform>(GetComponentsInChildren<HealthCubeTransform>());
     
        
        if (OnlyShowFirstLayerSnapZones)
        {
            foreach (var sz in AllSnapZones)
            {
                if (sz.Coords.z > 0)
                {
                    sz.gameObject.SetActive(false);
                }
            }
        }
    }

    public void Start()
    {
        foreach (var hct in AllHealthCubeTransforms)
        {
            hct.SetShineUIActive(false);
        }
        
        InvokeRepeating("SlowUpdate",0.0f,0.2f);
    }

    public void SlowUpdate()
    {
        m_AIPlayer.gameObject.SetActive(AIPlayerIsActive);
        m_AIPlayer.enabled = AIPlayerIsActive;
        m_AIPlayer.m_AIPlayerIsActive = AIPlayerIsActive;
        
        UpdateAllShineUI();
    }
    
    public void Update()
    {
       
    }





    public void UpdateAllShineUI() // Shine UI
    {

        if (_gamePhase != GameManager.GamePhase.InGame) return;
        if (!IsOwner || AIPlayerIsActive || DrawingGridIndex>1) return;  // HARD CODING DISABLING


        foreach (var hct in AllHealthCubeTransforms)     // trace from healthcube transforms outward
        {
            bool Found = false;

            for (int i = 0; i < 2; i++) // line trace at 2 heights going down
            {
                Ray ray;
                Vector3 rayOrigin =
                    hct.transform.position - new Vector3(0, 0.1818183f * i, 0); // lower the trace by i amount
                Vector3 rayDirection = -hct.transform.forward;
                float rayDistance = 100.0f;

                ray = new Ray(hct.transform.position, rayDirection * rayDistance);

                int SceneCubeLayerMask = 1 << LayerMask.NameToLayer("Hitable");

                RaycastHit hitInfo;

                if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, SceneCubeLayerMask))
                {
                    GameObject hitObject = hitInfo.collider.gameObject;
                    var scs = hitObject.GetComponent<SceneCubeNetworking>();
                    if (!scs) scs = hitObject.transform.parent.GetComponent<SceneCubeNetworking>();

                    if (!scs)
                    {
                        Debug.Log("Hit something on the SceneCube layer! Not a scene cube: " + hitObject.name);
                    }
                    else
                    {
                        if (scs.IsHealthCube)
                        {
                            hct.SetShineUIActive(true,scs, rayOrigin);
                            Found = true;
                            break; // Exit the loop early since we found a HealthCube
                        }
                    }
                }
            }

            // Only set Shine UI inactive if no health cube was found after both raycasts
            if (!Found)
            {
                hct.SetShineUIActive(false);
            }
            else
            {
            //    Debug.Log("found was true");
            }
        }



    }



    public override void OnNetworkSpawn()
    {
        OwningPlayer.OnValueChanged += OnOwningPlayerChanged;
        
       
    }

    
    

    private void OnOwningPlayerChanged(ulong previousvalue, ulong newvalue)
    {
       
    }
    
    
    
    

}
