using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
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

    
    
    
    public void Update()
    {
        m_AIPlayer.gameObject.SetActive(AIPlayerIsActive);
        m_AIPlayer.enabled = AIPlayerIsActive;
        m_AIPlayer.AIPlayerIsActive = AIPlayerIsActive;
    }


    
    
    
    public void UpdateAllShineUI()
    {
       

        
        
        foreach (var hct in AllHealthCubeTransforms)
        {
            hct.SetShineUIActive(false);    // assume false to begin with
            
            if (!IsOwner) return;
            
            for (int i = 0; i < 2; i++)        // line trace at 2 heights going down
            {
                
                Ray ray;
                Vector3 rayOrigin = hct.transform.position - new Vector3(0, 0.1818183f * i, 0);    // lower the trace by i amount
                Vector3 rayDirection = hct.transform.forward;
                float rayDistance = 100.0f;
                
                
                ray = new Ray(hct.transform.position, rayDirection * rayDistance);

                int SceneCubeLayerMask = 1 << LayerMask.NameToLayer("Hitable");

                RaycastHit hitInfo;

                if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, SceneCubeLayerMask))
                {
                    GameObject hitObject = hitInfo.collider.gameObject;
                    var scs = hitObject.GetComponent<SceneCubeNetworking>();

                    if (!scs)
                    {

                        Debug.Log("Hit something on the SceneCube layer! not a scene cube " + hitObject.name);
                    }
                    else
                    {
                        if (scs.IsHealthCube)
                        {
                            hct.SetShineUIActive(true,rayOrigin);

                            break;
                        }
                    }
                }
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
