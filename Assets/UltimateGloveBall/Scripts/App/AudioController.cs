// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using Meta.Utilities;
using UnityEngine;
using UnityEngine.Audio;

namespace UltimateGloveBall.App
{
    /// <summary>
    /// Controls the game audio, we can set the volume for Music, SFX and Crowd.
    /// It references the AudioMixer and apply changes to its exposed properties for each volume sliders.
    /// It gets and set each volume property to the the settings to save the state between application launches.
    /// Dynamically generated SFX audio source can be assigned to the SFX Mixer group.
    /// </summary>
    public class AudioController : Singleton<AudioController>
    {
        private const string MUSIC_VOL = "MusicVol";
        private const string SFX_VOL = "SfxVol";
        private const string CROWD_VOL = "CrowdVol";
        [SerializeField] private AudioMixer m_audioMixer;
        [SerializeField] private AudioMixerGroup m_sfxGroup;

        public AudioMixerGroup SfxGroup => m_sfxGroup;

        public float MusicVolume => GameSettings.Instance.MusicVolume;
        public int MusicVolumePct => Mathf.RoundToInt(MusicVolume * 100);
        public float SfxVolume => GameSettings.Instance.SfxVolume;
        public int SfxVolumePct => Mathf.RoundToInt(SfxVolume * 100);
        public float CrowdVolume => GameSettings.Instance.CrowdVolume;
        public int CrowdVolumePct => Mathf.RoundToInt(CrowdVolume * 100);

        public AudioSource m_audioSource;
        public AudioClip failsound;
        public AudioClip overflowsound;
        
        private void Start()
        {
            SetMusicVolume(GameSettings.Instance.MusicVolume);            // set music volume to what is in the GameSettings singleton, 
            SetSfxVolume(GameSettings.Instance.SfxVolume);               //  GameSettings is a UGP class that is just Gets and Sets for Unity PlayerPrefs key/values
            SetCrowdVolume(GameSettings.Instance.CrowdVolume);
        }

        public void SetMusicVolume(float val)
        {
            GameSettings.Instance.MusicVolume = val;
            _ = m_audioMixer.SetFloat(MUSIC_VOL, Mathf.Log10(val) * 20);
        }

        public void SetSfxVolume(float val)
        {
            GameSettings.Instance.SfxVolume = val;
            _ = m_audioMixer.SetFloat(SFX_VOL, Mathf.Log10(val) * 20);
        }

        public void SetCrowdVolume(float val)
        {
            GameSettings.Instance.CrowdVolume = val;
            _ = m_audioMixer.SetFloat(CROWD_VOL, Mathf.Log10(val) * 20);
        }


        public void PlaySound(string name = null, AudioClip clip = null)
        {
            m_audioSource.Stop();

            AudioClip audioToPlay = null;

            if (clip == null)
            {
                switch (name)
                {
                    case "": break;
                    case null: break;
                    
                    case "fail" : audioToPlay = failsound; break;
                    case "overflow" : audioToPlay = overflowsound; break;
                }
            }
            else
            {
                audioToPlay = clip;
            }

            if (!audioToPlay) return;
            
            m_audioSource.PlayOneShot(audioToPlay);

        }
    }
}