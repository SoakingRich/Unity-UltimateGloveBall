using System.Collections;
using System.Collections.Generic;
using UltimateGloveBall.App;
using Unity.Netcode;
using UnityEngine;

[DefaultExecutionOrder(-1000000)]
public class DebugStartServer : MonoBehaviour
{
    private NetworkManager nm;


    void Awake()
    {
      if(UGBApplication.Instance != null)
      {
          Destroy(this);
      }
    }
    
    void Start()                         // Start is called before the first frame update
    {
        nm = GetComponent<NetworkManager>();
        nm.StartHost();
        StartCoroutine("DelayThis");

    }

    IEnumerator DelayThis()
    {
        yield return new WaitForSeconds(5);
        if (Application.isEditor)
        {
        
       // NetworkManager.Singleton.StartClient();               // Start the client to connect to the server
    }
    }
    
    void Update()                     // Update is called once per frame
    {
        
    }
}
