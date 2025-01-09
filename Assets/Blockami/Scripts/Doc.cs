using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Doc : MonoBehaviour
{
    

    [TextArea(5, 20)] // The first number is the minimum height; the second is the maximum height.
    [Tooltip("Write your documentation notes here.")]
    public string documentation;
    
    

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
