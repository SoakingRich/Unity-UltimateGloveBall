using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorOnly : MonoBehaviour
{
    // Start is called before the first frame update

    [SerializeField] public  bool Invert;
    
    void Awake()
    {
        if (!Invert)
        {
            if (!UnityEngine.Application.isEditor)
            {
                Destroy(this.gameObject);
            }
        }
        else
        {
            if (UnityEngine.Application.isEditor)
            {
                Destroy(this.gameObject);
            }
        }

    }
    
    

}
