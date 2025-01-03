// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace UltimateGloveBall.Arena.Gameplay
{
    /// <summary>
    /// Handles the countdown view at the beginning of the game, including visual and audio when the number changes.
    /// It also has a callback when the countdown is complete.
    /// </summary>
    public class CountdownView : MonoBehaviour           // countdown before the game starts
    {
        [SerializeField] private TMP_Text m_text;
        [SerializeField] private AudioSource m_audioSource;
        [SerializeField] private AudioClip m_beep1;
        [SerializeField] private AudioClip m_beep2;
        private double m_endTime;
        private Action m_onComplete;
        private int m_previous = -1;
        private bool m_showing;

        private void Update()
        {
            if (m_showing)
            {
                var time = m_endTime - NetworkManager.Singleton.ServerTime.Time;        // server time gets reset to zero at beginning of match ?? 
                var seconds = Math.Max(0, (int)Math.Floor(time));              // clamp seconds at zero,    floor to a whole number of seconds
                m_text.text = seconds == 0 ? "GO" : seconds.ToString();        // countdown numbers and then say Go!

                if (m_previous != seconds)     // if seconds int is a new value, trigger a beep
                {
                    TriggerBeep(seconds);
                }

                m_previous = seconds;

                if (time < 0)
                {
                    Hide();
                    m_onComplete?.Invoke();         // countdown is complete,   Invoke
                }
            }
        }

        public void Show(double endTime, Action onComplete = null)       // start the countdown
        {
            m_text.gameObject.SetActive(true);
            m_showing = true;
            m_endTime = endTime;
            if (onComplete != null)
            {
                m_onComplete = onComplete;
            }
        }

        public void Hide()          // hide it and stop counting down
        {
            m_text.gameObject.SetActive(false);
            m_showing = false;
            m_previous = -1;
        }

        private void TriggerBeep(int val)
        {
            if (val == 0)
            {
                m_audioSource.PlayOneShot(m_beep2);
            }
            else
            {
                m_audioSource.PlayOneShot(m_beep1);
            }
        }
    }
}