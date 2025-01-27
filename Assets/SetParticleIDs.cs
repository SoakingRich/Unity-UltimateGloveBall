using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SetParticleIDs : MonoBehaviour {

    private ParticleSystem ps;
    private List<Vector4> customData = new List<Vector4>();
    private int uniqueID;

    void Start() {

        ps = GetComponent<ParticleSystem>();
    }

    void Update()
    {
        uniqueID = 0;
       
        ps.GetCustomParticleData(customData, ParticleSystemCustomData.Custom1);

       
        for (int i = 0; i < customData.Count; i++)
        {
          
                customData[i] = new Vector4(++uniqueID, 0, 0, 0);
            
        }

        ps.SetCustomParticleData(customData, ParticleSystemCustomData.Custom1);
    }
}