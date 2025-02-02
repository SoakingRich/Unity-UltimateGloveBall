using System;
using System.Collections;
using System.Collections.Generic;
using Blockami.Scripts;
using UnityEngine;




// this visual gO is constantly scaling





public class squashStretch : MonoBehaviour
{
    [SerializeField] public BlockamiData BlockamiData;   
    
    public float squashAmount;
    public float speed = 1.0f;
    public bool deactivated;
    public BoxCollider box;

    public bool Ignore;

    
    
    
    
    
    
    
    void Start()
    {
        box = GetComponent<BoxCollider>();
    }
    
    void TrackBlockamiData()
    {
        speed = BlockamiData.speed;
   
    }
    
    void Update()
    {
        TrackBlockamiData();
        
        if (Ignore)
        {
            enabled = false;
        }
        
        var desiredScale =  new Vector3(1-squashAmount, 1 + squashAmount, 1- squashAmount);
        transform.localScale = Vector3.Slerp(transform.localScale,desiredScale, Time.deltaTime * speed);   // the object to have its Scale transformed
     //   transform.localScale = new Vector3(1-squashAmount, 1 + squashAmount, 1- squashAmount);   // the object to have its Scale transformed
     transform.rotation = Quaternion.Euler(0.0f,transform.rotation.y,0.0f);

     if (deactivated)
     {
         box.size = Vector3.Slerp(box.size, Vector3.one, Time.deltaTime * speed);
     }
    }
}
