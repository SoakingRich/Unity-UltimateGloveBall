// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System;
using TMPro;
using UltimateGloveBall.Arena.Gameplay;
using UltimateGloveBall.Arena.Services;
using UnityEngine;

namespace UltimateGloveBall.Arena.Environment
{
    /// <summary>
    /// The scoreboard visual is handled by this class. It keeps a reference of the dynamic text and handles the
    /// rendering of the scoreboard when it changes to the render texture. This listens to the game phase changes from
    /// the game manager to handle the different phase messages.
    /// </summary>
    public class ScoreBoard : MonoBehaviour, IGamePhaseListener
    {
        [SerializeField] private GameManager m_gameManager;
        [SerializeField] private Camera m_scoreCamera;
        [SerializeField] private Camera m_phaseCamera;
        [SerializeField] private TMP_Text m_teamATitle;
        [SerializeField] private TMP_Text m_teamAScore;
        [SerializeField] private TMP_Text m_teamBTitle;
        [SerializeField] private TMP_Text m_teamBScore;

        [SerializeField] private TMP_Text m_stateText;           // public field for a text field on a TextMeshPro component ,   the timer count down

        private long m_lastTimeLeftShown = 0;

        private void Start()
        {
            GameState.Instance.Score.OnScoreUpdated += OnScoreUpdated;
            m_gameManager.RegisterPhaseListener(this);

            m_scoreCamera.enabled = m_phaseCamera.enabled = false;          // PhaseCamera is countdown timer.  It is a UI Canvas in the game world with an Orthographic camera pointing at it, rendering to a render texture
            // it also has a UniversalAdditionalCameraData script on it.   Using UI via a render texture works well given the ScoreBoard is on a big curved screen
        }

        private void OnDestroy()
        {
            m_gameManager.UnregisterPhaseListener(this);
            GameState.Instance.Score.OnScoreUpdated -= OnScoreUpdated;
        }

        public void OnTeamColorUpdated(TeamColor teamAColor, TeamColor teamBColor)
        {
            var colorA = TeamColorProfiles.Instance.GetColorForKey(teamAColor);
            var colorB = TeamColorProfiles.Instance.GetColorForKey(teamBColor);
            m_teamAScore.color = colorA;
            m_teamATitle.color = colorA;
            m_teamBScore.color = colorB;
            m_teamBTitle.color = colorB;

            m_scoreCamera.Render();         // update the scoreboard ui   if the TeamColors have changed, as the colors of the scores should be changed
        }

        public void OnPhaseChanged(GameManager.GamePhase phase)    // timer shows a message at times when the time isnt counting down
        {
            string msg = null;
            switch (phase)
            {
                case GameManager.GamePhase.PreGame:
                    msg = "Host press start";
                    break;
                case GameManager.GamePhase.CountDown:
                    msg = "Ready!";
                    break;
                case GameManager.GamePhase.InGame:
                    msg = "PLAY!";
                    break;
                case GameManager.GamePhase.PostGame:
                    msg = "Game Over";
                    break;
                default:
                    break;
            }

            m_stateText.text = msg;

            m_phaseCamera.Render();
        }

        public void OnPhaseTimeCounter(double timeCounter)
        {
            //nothing
        }
        
        public void OnPhaseTimeUpdate(double timeLeft)    // render the time update once per second
        {
            var timeFloored = (long)Math.Floor(timeLeft);
            if (m_lastTimeLeftShown == timeFloored)
            {
                return;
            }
            var time = TimeSpan.FromSeconds(timeFloored);
            var timeStr = time.ToString("mm':'ss");
            if (timeStr == m_stateText.text)
            {
                return;
            }

            m_stateText.text = timeStr;
            m_lastTimeLeftShown = timeFloored;
            m_phaseCamera.Render();
        }

        private void OnScoreUpdated(int teamA, int teamB)     // update the score ui evertime a score is changed
        {
            m_teamAScore.text = teamA.ToString();
            m_teamBScore.text = teamB.ToString();

            m_scoreCamera.Render();
        }
    }
}