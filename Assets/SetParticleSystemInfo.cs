using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[ExecuteAlways]
public class SetParticleSystemInfo : MonoBehaviour
{

    [SerializeField]
    public ParticleSystem ps;
    private List<Vector4> customData1 = new List<Vector4>();
    private List<Vector4> customData2 = new List<Vector4>();
   

    void Start() {

        ps = GetComponent<ParticleSystem>();
    }

    void Update()
    {
        ps.GetCustomParticleData(customData1, ParticleSystemCustomData.Custom1);
        ps.GetCustomParticleData(customData2, ParticleSystemCustomData.Custom2);

        Vector3 rectangleScale = Vector3.zero;
        
        var shapeModule = ps.shape;
        if (shapeModule.shapeType == ParticleSystemShapeType.Rectangle)
        {
            // Get the scale of the rectangle
            rectangleScale = shapeModule.scale;
        }


        for (int i = 0; i < customData1.Count; i++)
        {
          
            customData1[i] = new Vector4(ps.transform.position.x, ps.transform.position.y, ps.transform.position.z, 0);
         //   customData2[i] = new Vector4(ps.transform.rotation.eulerAngles.x, ps.transform.rotation.eulerAngles.y, ps.transform.rotation.eulerAngles.z, 0);
            customData2[i] = new Vector4(rectangleScale.x, rectangleScale.y, rectangleScale.z, 0);
            
        }

        ps.SetCustomParticleData(customData1, ParticleSystemCustomData.Custom1);
        ps.SetCustomParticleData(customData2, ParticleSystemCustomData.Custom2);
    }
}