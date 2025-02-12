using System;
using System.Collections;
using System.Collections.Generic;
using UltimateGloveBall.Arena.Player;
using UnityEngine;

public class ShieldEnabler : MonoBehaviour
{
   

    private void OnEnable()
    {
        Debug.Log("ShieldEnabled by ActivateStateTracker");

        PlayerInputController pic = FindObjectOfType<PlayerInputController>();
        if (pic)
        {
            pic.OnShield(Glove.GloveSide.Right, true);
        }
        
    }

    private void OnDisable()
    {
        Debug.Log("ShieldDisable by ActivateStateTracker");

        PlayerInputController pic = FindObjectOfType<PlayerInputController>();
        if (pic)
        {
            pic.OnShield(Glove.GloveSide.Right, false);
        }
    }
}
