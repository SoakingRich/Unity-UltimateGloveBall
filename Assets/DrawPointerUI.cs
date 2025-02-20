using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blockami.Scripts;
using Oculus.Interaction;
using UltimateGloveBall.Arena.Player;
using UltimateGloveBall.Arena.Services;
using Unity.Netcode;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Serialization;

public class DrawPointerUI : MonoBehaviour
{
    public DrawingGrid OwningDrawingGrid;
    private BlockamiData BlockamiData;
    private PlayerControllerNetwork pcn;
    public MaterialPropertyBlockEditor _materialPropertyBlockEditor;
    private Renderer rend;
    public float LerpSpeed = 10.0f;
    public bool FPSControlled = false;
    
    public Vector3 LerpTargetPosition;
    // private static readonly int s_interiorColor = Shader.PropertyToID("InteriorColor");
    // private static readonly int s_fresnelColor = Shader.PropertyToID("FresnelColor");
    
    private static readonly int s_interiorColor = Shader.PropertyToID("Color_DA10CA39");
    private static readonly int s_fresnelColor = Shader.PropertyToID("Color_CD9DA168");


    private TriggerPinchEvents[] allTPE;

    private void Awake()
    {
       
    }

    void Start()
    {
        
        
        /// SETUP MATERIAL FOR LINE RENDERER
   
        
        
        _materialPropertyBlockEditor = GetComponent<MaterialPropertyBlockEditor>();
        rend = GetComponent<Renderer>();
        
        
        List<Material> matlist = new List<Material>();
        rend.GetMaterials(matlist);
        var mat = matlist[0];
        
        var defaultCol = mat.GetColor(s_interiorColor);
        _materialPropertyBlockEditor.MaterialPropertyBlock.SetColor(s_interiorColor, defaultCol);
    
        var defaultCol2 = mat.GetColor(s_fresnelColor);
        _materialPropertyBlockEditor.MaterialPropertyBlock.SetColor(s_fresnelColor, defaultCol2);
    }


    
    
    


    void Update()
    {
        if (!LocalPlayerEntities.Instance) return;

        if (!pcn)
        {
            pcn = LocalPlayerEntities.Instance.LocalPlayerController;
             if(pcn) pcn.OnCyclePlayerColor += OnCyclePlayerColor;
            return;
        }
        
        if (pcn.OwnerClientId != NetworkManager.Singleton.LocalClientId ||
            pcn.OwnedDrawingGrid != OwningDrawingGrid)
        {
           // enabled = false;
           rend.enabled = false;
            return;
        }
        
        rend.enabled = true;


        if (FPSControlled)
        {
            LerpPosition();
            return;       // use fpscontrolled value
        }
        
        
        var eyetrack = FindObjectOfType<EyeTracking>();
        if (eyetrack)
        {
            if (eyetrack.CurrentEyetrackedSnapZone)
            {
                LerpTargetPosition = eyetrack.CurrentEyetrackedSnapZone.transform.position;
                LerpPosition();
                return;
            }
        }
        
        
        
        if(allTPE == null) { 
            allTPE = FindObjectsOfType<TriggerPinchEvents>();
            return;
        }
       
            GameObject nearestObject = null;
            float closestDistance = Mathf.Infinity;
            
            if (allTPE.Length < 1) { return; }

            foreach (var tpe in allTPE)            // find the TriggerPinchEvents closest to the grids surface
            {
                // project point of each hand onto Grid Normal
                Vector3 projectedPoint = tpe.transform.position - Vector3.Dot(tpe.transform.position - OwningDrawingGrid.transform.position, OwningDrawingGrid.transform.forward) * OwningDrawingGrid.transform.forward;
                float distance = Vector3.Distance(projectedPoint, tpe.transform.position);
                

                
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    nearestObject = tpe.gameObject;
                }
            }

            if (nearestObject == null) return;

            var allSnaps = OwningDrawingGrid.AllSnapZones;
            var nearestSnapzone = UtilityLibrary.GetNearestObjectFromList(allSnaps, nearestObject.transform.position);        // Lerp to nearest Snapzone to the hand
            LerpTargetPosition = nearestSnapzone.gameObject.transform.position;
            
                // Plane drawingPlane = new Plane(OwningDrawingGrid.transform.rotation * Vector3.forward,
                //     OwningDrawingGrid.transform.position);
                //
                // // Project the nearest object's position onto the plane
                // Vector3 projectedPosition = drawingPlane.ClosestPointOnPlane(nearestObject.transform.position);

             //   transform.position = projectedPosition;
            //    LerpTargetPositoon = projectedPosition;

            
            
                LerpPosition();


    }


    void LerpPosition()
    {
       
        
        transform.position = Vector3.Lerp(transform.position, LerpTargetPosition, Time.deltaTime * LerpSpeed);
    }








    public void Move(SnapZone s)
    {
    //    transform.position = s.transform.position;
    LerpTargetPosition = s.transform.position;
    }
    
    
    
    
    private void OnCyclePlayerColor(PlayerControllerNetwork obj)
    {
       
        
        if (obj == null) return;
      
    
        var col = BlockamiData.Instance.GetColorFromColorID(obj.ColorID);
        
        _materialPropertyBlockEditor.MaterialPropertyBlock.SetColor(s_interiorColor, col);
       var col2 = HueShift(col,0.2f);
        _materialPropertyBlockEditor.MaterialPropertyBlock.SetColor(s_fresnelColor, col2);
        
    }
    
    
    
    
    // A simple function to shift the hue of a given color
    public Color HueShift(Color color, float shift)
    {
        // Convert from RGB to HSV
        Color.RGBToHSV(color, out float h, out float s, out float v);

        // Shift the hue
        h = (h + shift) % 1f; // Keep the hue in the range [0, 1]
        if (h < 0) h += 1f;   // Handle negative wrapping

        // Convert back to RGB
        return Color.HSVToRGB(h, s, v);
    }
}

