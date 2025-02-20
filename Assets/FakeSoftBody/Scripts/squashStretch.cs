using System;
using System.Collections;
using System.Collections.Generic;
using Blockami.Scripts;
using UnityEngine;
using UnityEngine.Serialization;


// this visual gO is constantly scaling





public class squashStretch : MonoBehaviour
{
     private BlockamiData BlockamiData;

    public FakeSoftCube parent;
    [FormerlySerializedAs("squashAmount")] public float squashAmountTarget;
    public float speed = 1.0f;
    public BoxCollider InnerBox;

    public bool Ignore => parent.Ignore;
    public bool deactivated => parent.deactivated;

    
    
    
    
    
    
    
    void Start()
    {
       
        parent = transform.parent.GetComponent<FakeSoftCube>();
        InnerBox = GetComponent<BoxCollider>();
    }
    
    void TrackBlockamiData()
    {
        speed = BlockamiData.Instance.speed;
   
    }
    
    void Update()
    {
        TrackBlockamiData();
        
        if (Ignore)
        {
            InnerBox.size = Vector3.one;
            return;
        }
        
       
        
        var desiredScale =  deactivated ? Vector3.one :  new Vector3(1-squashAmountTarget, 1 + squashAmountTarget, 1- squashAmountTarget);
        
        transform.localScale = Vector3.Slerp(transform.localScale,desiredScale, Time.deltaTime * speed);   // the object to have its Scale transformed
     //   transform.localScale = new Vector3(1-squashAmount, 1 + squashAmount, 1- squashAmount);   // the object to have its Scale transformed
      transform.rotation = Quaternion.Euler(0.0f,transform.rotation.y,0.0f);        // for some reason i need to constantly update rotation otherwise cubes go at weird angles????

     if (deactivated)
     {
         InnerBox.size = Vector3.Slerp(InnerBox.size, Vector3.one, Time.deltaTime * speed);
         return;
     }
    
    }
}
