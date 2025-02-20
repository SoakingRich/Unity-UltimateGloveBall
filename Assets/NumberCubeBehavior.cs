using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

public class NumberCubeBehavior : CubeBehavior
{

    public NetworkVariable<int> net_NumberRequirement = new NetworkVariable<int>(2);

    public int NumberRequirement
    {
        get => net_NumberRequirement.Value;
        set => net_NumberRequirement.Value = value;
    }
    
    public Texture2D number2Texture;
    public Texture2D number3Texture;
    public Texture2D number4Texture;
    
    
    
    
     protected override void OnEnable()
    {
        base.OnEnable();
        scs.HCHit += ScsOnHCHit;
        net_NumberRequirement.OnValueChanged += OnNumberRequirementChanged;
        
        
       

    }

    private void OnNumberRequirementChanged(int previousvalue, int newvalue)
    {
       // set a material, init its texture, and color

       Texture2D TextureToUse;

       switch (newvalue)  
       {
           case 2:
               TextureToUse = number2Texture;
               break;
           case 3:
               TextureToUse = number3Texture;
               break;
           case 4:
               TextureToUse = number4Texture;
               break;
           case 5:
               return;
           default:
               return; // Optional: Handle unexpected values
               break;
       }
       
       Material mat = GetComponentInChildren<MeshRenderer>().material;
       mat.SetTexture("_Texture2D", TextureToUse);
       mat.EnableKeyword("_USETEXTURE");
       mat.SetFloat("_USETEXTURE", 1); // Assuming _USETEXTURE is a float property in the shader
    }

    protected override  void OnDisable()
    {
      base.OnDisable();
      scs.HCHit -= ScsOnHCHit;
    }
    
    
    private void ScsOnHCHit(SceneCubeNetworking obj)
    {
       
    }
    
    public override void OnIntialized()
    {
        float randomValue = Random.value; // Generates a float between 0.0 and 1.0

        if (randomValue < 0.6f) 
            NumberRequirement = 2; // 60% chance
        else if (randomValue < 0.9f) 
            NumberRequirement = 3; // 30% chance (0.6 - 0.9)
        else 
            NumberRequirement = 4; // 10% chance (0.9 - 1.0)
        
        OnNumberRequirementChanged(-1, NumberRequirement);
    }
    
     
    public override void ResetCube()
    {
      
    }
    
    public override void ScsOnSCDied(SceneCubeNetworking obj)
    {
      
    }
    
    public override void ScsOnSCDiedByPlayerCube(SceneCubeNetworking obj,ulong clientID)
    {
      
        
        
    }
}
