using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;

public class ActiveStateDebug : MonoBehaviour
{
    
    
    
    
    public enum ActiveStateInstance
    {
        IsTrue,
        IsFalse,
        None
        
    }
    
   [SerializeField] public ActiveStateInstance[] AllActiveStateInstance;
   [SerializeField] public string[] AllActiveStateInstanceName;
    
   
    

    void Update()
    {
        if (!UnityEngine.Application.isEditor) return;

        var num = GetComponents<IActiveState>().Length;
        
        AllActiveStateInstance = new ActiveStateInstance[num];
        AllActiveStateInstanceName = new string[num];
        
        var allActiveStates = GetComponents<IActiveState>();
        for (int i = 0; i < allActiveStates.Length; i++)
        {
            IActiveState elem = allActiveStates[i];
            AllActiveStateInstance[i] = elem.Active ? ActiveStateInstance.IsTrue : ActiveStateInstance.IsFalse;
            AllActiveStateInstanceName[i] = elem.GetType().Name;
        }

    }
}
