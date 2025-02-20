using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction.Input;
using UltimateGloveBall.Arena.Player.Menu;
using UnityEngine;

public class PalmUpPinchEvents : MonoBehaviour
{
    public TriggerPinchEvents tpe;
    
   
    void Start()
    {
        
        tpe.TriggerPinchPressedEvent += TpeOnTriggerPinchPressedEvent;
        
        
      
    }

    private void TpeOnTriggerPinchPressedEvent(bool arg1, OVRHand arg2, Controller arg3)
    {
        if (OVRInput.activeControllerType == OVRInput.Controller.Hands)
        {
            if (arg2.IsSystemGestureInProgress) return;
            
            var v = transform.forward;
          
            var dot = Vector3.Dot(v, Vector3.up);
            if (dot > 0)
            {
                Debug.Log("Blockami Log - pinching while palm facing up ??");

                var m_playerMenu = FindObjectOfType<PlayerInGameMenu>();
                if(m_playerMenu) m_playerMenu.Toggle();
                
            }
        }
    }

    
}
