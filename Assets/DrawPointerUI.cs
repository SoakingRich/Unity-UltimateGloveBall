using System.Collections;
using System.Collections.Generic;
using Blockami.Scripts;
using Oculus.Interaction;
using UltimateGloveBall.Arena.Player;
using UltimateGloveBall.Arena.Services;
using Unity.Netcode;
using UnityEngine;

public class DrawPointerUI : MonoBehaviour
{
    private BlockamiData BlockamiData;
    private PlayerControllerNetwork pcn;
    private MaterialPropertyBlockEditor _materialPropertyBlockEditor;
    private Renderer rend;
    // private static readonly int s_interiorColor = Shader.PropertyToID("InteriorColor");
    // private static readonly int s_fresnelColor = Shader.PropertyToID("FresnelColor");
    
    private static readonly int s_interiorColor = Shader.PropertyToID("Color_DA10CA39");
    private static readonly int s_fresnelColor = Shader.PropertyToID("Color_CD9DA168");

    void Start()
    {
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
            //  pcn.OnCyclePlayerColor += OnCyclePlayerColor;
            return;
        }




        if (pcn.OwnerClientId != NetworkManager.Singleton.LocalClientId)
        {
            enabled = false;
        }

    }

    
    public void Move(SnapZone s)
    {
        transform.position = s.transform.position;
    }
    
    
    
    
    private void OnCyclePlayerColor(PlayerControllerNetwork obj)
    {
        if (obj == null) return;
      
    
        var col = BlockamiData.GetColorFromColorID(obj.ColorID);
        
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

