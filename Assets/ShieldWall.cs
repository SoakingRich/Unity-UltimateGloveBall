using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UltimateGloveBall.Arena.Gameplay;
using UltimateGloveBall.Arena.Services;
using UnityEngine;

public class ShieldWall : MonoBehaviour, IGamePhaseListener
{

    public GameObject visual;
    

    void Start()
    {
        SpawnManager.Instance.OnTimeThresholdPassed += OnTimeThresholdPassed;
    }

    private void OnTimeThresholdPassed()
    {
        SetWallActive(false);
    }


    private void OnEnable()
    {
        GameManager.Instance.RegisterPhaseListener(this);
    }

    private void OnDisable()
    {
        GameManager.Instance.UnregisterPhaseListener(this);
    }

    void SetWallActive(bool enable)
    {
        if (enable)
        {
            visual.transform.DOKill();
           visual.transform.localScale = new Vector3(1.0f, 0.0f, 1.0f);
           // visual.transform.localScale = Vector3.zero;

            visual.SetActive(true);
            visual.transform.DOScaleY(1.0f, 1.0f);
          //  visual.transform.DOScale(1.0f, 1.0f);
        }
        else
        {
            visual.transform.DOKill();
            visual.transform.DOScaleY(0.0f, 1.0f).OnComplete(() =>
        //    visual.transform.DOScale(0.0f, 1.0f).OnComplete(() =>
            {
                visual.SetActive(false);
            });
            
        }
    }
    
    
    public void OnPhaseChanged(GameManager.GamePhase phase)
    {
        switch (phase)
        {
            case GameManager.GamePhase.PostGame:
                SetWallActive(false);
             
             
                break;
            case GameManager.GamePhase.InGame:
                SetWallActive(true);
                break;
            
            case GameManager.GamePhase.PreGame:
                SetWallActive(false);
                break;
            
            case GameManager.GamePhase.CountDown:
                SetWallActive(false);
                break;
        }
    }

    public void OnPhaseTimeUpdate(double timeLeft)
    {
     
    }

    public void OnPhaseTimeCounter(double timeCounter)
    {
       
    }

    public void OnTeamColorUpdated(TeamColor teamColorA, TeamColor teamColorB)
    {
      
    }
}
