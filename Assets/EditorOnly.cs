using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorOnly : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if (UnityEngine.Application.isEditor) // editor only
        {
            
        }
        else
        {
            Destroy(this);  // lol this is just destroying the script
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
