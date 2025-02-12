// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System.Collections;
using UltimateGloveBall.Arena.Gameplay;
using UltimateGloveBall.Arena.Services;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UltimateGloveBall.Arena.Crowd
{
    /// <summary>
    /// Controls the crowd npc in the bleachers. Initializing the crowd member and setting their team colors.
    /// It also handles the crowd audio based on the game score and the game phase. 
    /// </summary>
    public class CrowdController : NetworkBehaviour, IGamePhaseListener
    {
        private static readonly int s_attachmentColorID = Shader.PropertyToID("_Attachment_Color");     // gets a shader property ("_Attachment_Color") as an Int ID for use with material m_teamAAccessoriesAndItemsMat.SetColor

        public enum CrowdLevel
        {
            Full,
            Pct75,
            Half,
            Quarter,
            None,
        }

        [SerializeField] private CrowdNPC[] m_teamACrowd;
        [SerializeField] private CrowdNPC[] m_teamBCrowd;
        [SerializeField] private Material m_teamAAccessoriesAndItemsMat;
        [SerializeField] private Material m_teamBAccessoriesAndItemsMat;

        [SerializeField] private AudioSource m_crowdAAudioSource;
        [SerializeField] private AudioSource m_crowdBAudioSource;

        [SerializeField] private AudioClip[] m_idleSounds;

        [SerializeField] private AudioClip[] m_hitReactionSounds;
        [SerializeField] private AudioClip m_booSound;
        [SerializeField] private AudioClip m_chantSound;

        [SerializeField] private int m_booingDifferential = 3;

        [SerializeField] private GameManager m_gameManager;

        private float m_nextBooTimeTeamA = 0;
        private float m_nextBooTimeTeamB = 0;

        private float m_nextChantTimeTeamA = 0;
        private float m_nextChantTimeTeamB = 0;

        // keep track of score
        private int m_scoreA = -1;
        private int m_scoreB = -1;

        private void Start()
        {
            Initialize(m_teamACrowd, m_teamAAccessoriesAndItemsMat);    // give all npcs random faces and anim offsets
            Initialize(m_teamBCrowd, m_teamBAccessoriesAndItemsMat);
            m_gameManager.RegisterPhaseListener(this);        // by registering as a PhaseListener with game manager, game manager will call IGamePhaseListener interface functions on us

            StartIdleSound();
        }

        private void Update()
        {
            if (m_nextChantTimeTeamA > 0 && Time.realtimeSinceStartup >= m_nextChantTimeTeamA)          // do cheers at random intervals
            {
                PlayChant(0);
                m_nextChantTimeTeamA += Random.Range(30f, 50f);
            }

            if (m_nextChantTimeTeamB > 0 && Time.realtimeSinceStartup >= m_nextChantTimeTeamB)
            {
                PlayChant(1);
                m_nextChantTimeTeamB += Random.Range(30f, 50f);
            }
        }

        public override void OnDestroy()
        {
            m_gameManager.UnregisterPhaseListener(this);
            base.OnDestroy();
        }

        public void OnPhaseChanged(GameManager.GamePhase phase)
        {
            if (phase == GameManager.GamePhase.PostGame)
            {
                m_nextChantTimeTeamA = 0;
                m_nextChantTimeTeamB = 0;
            }
        }

        public void OnPhaseTimeUpdate(double timeLeft)
        {
            // Nothing   
        }

        public void OnPhaseTimeCounter(double timeCounter)
        {
            //nothing
        }
        
        public void OnTeamColorUpdated(TeamColor teamColorA, TeamColor teamColorB)     // when colors for both teams have been set to a new pair
        {
            SetAttachmentColor(teamColorA, teamColorB);    // attachment refers to hats/jerseys/baloons?
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                GameState.Instance.Score.OnScoreUpdated += OnScoreUpdated;
            }
        }

        public override void OnNetworkDespawn()
        {
            GameState.Instance.Score.OnScoreUpdated -= OnScoreUpdated;
            base.OnNetworkDespawn();
        }

        public void SetAttachmentColor(TeamColor teamAColor, TeamColor teamBColor)
        {
            m_teamAAccessoriesAndItemsMat.SetColor(s_attachmentColorID, TeamColorProfiles.Instance.GetColorForKey(teamAColor));    // set material param name by int ID,  get color for team from Singleton dictionary
            m_teamBAccessoriesAndItemsMat.SetColor(s_attachmentColorID, TeamColorProfiles.Instance.GetColorForKey(teamBColor));
        }

        public void SetCrowdLevel(CrowdLevel crowdLevel)        // not clear when this is ever called,  maybe in a UnityEvent somewhere??  Im guessing the crowd is only half full when theres less spectators present?
        {
            var pct = 100;
            switch (crowdLevel)
            {
                case CrowdLevel.Full:
                    pct = 100;
                    break;
                case CrowdLevel.Pct75:
                    pct = 75;
                    break;
                case CrowdLevel.Half:
                    pct = 50;
                    break;
                case CrowdLevel.Quarter:
                    pct = 25;
                    break;
                case CrowdLevel.None:
                    pct = 0;
                    break;
                default:
                    break;
            }

            UpdateCrowdLevel(m_teamACrowd, pct);
            UpdateCrowdLevel(m_teamBCrowd, pct);
        }

        private void UpdateCrowdLevel(CrowdNPC[] crowd, int pct)
        {
            var activeCount = pct >= 100 ? crowd.Length :
                pct <= 0 ? 0 : Mathf.FloorToInt(crowd.Length * pct / 100f);        // set  activeCount to either Crowd.Length or  ( zero or crowd.length divide by 100 * percent arg ) 
            for (var i = 0; i < crowd.Length; ++i)
            {
                crowd[i].gameObject.SetActive(i < activeCount);              // set a percentage of the crowd npcs to be Inactive
            }
        }

        private void Initialize(CrowdNPC[] crowd, Material accessoryAndItemsMat)    // CrowdNPC is an array of CrowdNPC.cs scripts,    // give all npcs random faces and anim offsets
        {
            foreach (var npc in crowd)
            {
                // 3x3 faces
                var faceSwap = new Vector2(Random.Range(0, 3), Random.Range(0, 3));
                npc.Init(Random.Range(0f, 1f), Random.Range(0.9f, 1.1f), faceSwap);  // give a random face, random anim speed, random anim offset
            }
        }

        private void SetTeamColor(CrowdNPC[] crowd, TeamColor teamColor)        // set team color for a crowd array
        {
            var color = TeamColorProfiles.Instance.GetColorForKey(teamColor);
            foreach (var npc in crowd)
            {
                npc.SetColor(color);
            }
        }

        private void OnScoreUpdated(int teamAScore, int teamBScore)
        {
            if (m_scoreA < 0 || m_scoreB < 0)
            {
                m_scoreA = teamAScore;
                m_scoreB = teamBScore;
                return;            // dont do anything
            }

            var scoredA = teamAScore > m_scoreA;       // check if TeamA score increased?
            var scoredB = teamBScore > m_scoreB;        // check if TeamB score increased?

            if (scoredA)
            {
                PlayHitReaction(0);                            // A player got hit, therefore score went up for opposite team, do cheering when a player gets hit, 
                if (teamAScore > teamBScore && m_nextChantTimeTeamA <= 0)
                {
                    m_nextChantTimeTeamA = Time.realtimeSinceStartup + Random.Range(0, 10);            // if teamA hasnt had a chant in a while, and they are winning,   schedule a cheer in the next 10 seconds or so 
                }

                if (teamAScore >= teamBScore + m_booingDifferential)               // we seem to check for if TeamA are only very-close to winning, and if so, the other team boos?
                {
                    if (m_nextBooTimeTeamA <= Time.realtimeSinceStartup)
                    {
                        PlayBoo(1);
                        m_nextBooTimeTeamA = Time.realtimeSinceStartup + Random.Range(12f, 20f); //     schedule more booing sometime 12-20 secs from now
                    }
                }
            }

            if (scoredB)
            {
                PlayHitReaction(1);
                if (teamBScore > teamAScore && m_nextChantTimeTeamA <= 0)
                {
                    m_nextChantTimeTeamB = Time.realtimeSinceStartup + Random.Range(0, 10);
                }

                if (teamBScore >= teamAScore + m_booingDifferential)
                {
                    if (m_nextBooTimeTeamB <= Time.realtimeSinceStartup)
                    {
                        PlayBoo(0);
                        m_nextBooTimeTeamB = Time.realtimeSinceStartup + Random.Range(12f, 20f);
                    }
                }
            }

            m_scoreA = teamAScore;
            m_scoreB = teamBScore;
        }

        private void PlayHitReaction(int team)        // play crowd audio reaction to a player getting hit
        {
            PlaySoundClientRpc(new SoundParametersMessage(                                         // a SoundParametersMessage is a ISerializable that is defined at bottom of script
                team, AudioEvents.HitReaction, Random.Range(0, m_hitReactionSounds.Length)));        // its constructed with 3 optional params, Team, enumType,  an index (array index)
        }

        private void PlayBoo(int team)                            // it seems odd that a ISerializable needed to be created just to call an RPC on clients with params
        {
            PlaySoundClientRpc(new SoundParametersMessage(
                team, AudioEvents.Boo));
        }

        private void PlayChant(int team)
        {
            PlaySoundClientRpc(new SoundParametersMessage(
                team, AudioEvents.Chant));
        }

        [ClientRpc]
        private void PlaySoundClientRpc(SoundParametersMessage soundMsg)
        {
            var audioSource = soundMsg.Team == 0 ? m_crowdAAudioSource : m_crowdBAudioSource;
            switch (soundMsg.AudioEvent)
            {
                case AudioEvents.HitReaction:
                    audioSource.PlayOneShot(m_hitReactionSounds[soundMsg.AudioEventIndex]);
                    break;

                case AudioEvents.Boo:
                    audioSource.PlayOneShot(m_booSound);
                    break;

                case AudioEvents.Chant:
                    audioSource.PlayOneShot(m_chantSound);
                    break;
                case AudioEvents.Idle:
                    break;
                default:
                    break;
            }
        }

        private void StartIdleSound()
        {
            _ = StartCoroutine(PlayIdleCoroutine(0));
            _ = StartCoroutine(PlayIdleCoroutine(1));
        }

        private IEnumerator PlayIdleCoroutine(int team)
        {
            var audioSource = team == 0 ? m_crowdAAudioSource : m_crowdBAudioSource;

            while (true)
            {
                var idleIndex = Random.Range(0, m_idleSounds.Length);
                var clip = m_idleSounds[idleIndex];
                audioSource.clip = clip;
                audioSource.loop = true;
                audioSource.time = Random.Range(0, clip.length);
                audioSource.Play();
                // wait a full audio loop
                yield return new WaitForSeconds(clip.length);            // constantly play idle sounds while enumerator is running
            }
        }

        internal enum AudioEvents                // enum that is only used in this class, thus Internal
        {
            Idle,
            HitReaction,
            Boo,
            Chant,
        }

        private struct SoundParametersMessage : INetworkSerializable               // looks like we override the class of INetworkSerializable that gets used for this class
        {                              // allows us to override NetworkSerialize to serialize less properties than what would otherwise   ( by default, all public member variables are serialized??)
                                       // we override the constructor of this INetworkSerializable to populate the variables to sync
            public int Team;
            public AudioEvents AudioEvent;
            public int AudioEventIndex;

            internal SoundParametersMessage(int team, AudioEvents audioEvent, int index = 0)        // the constructor for   SoundParametersMessage  is internal, as it is not to be used by any other class
            {
                Team = team;
                AudioEvent = audioEvent;
                AudioEventIndex = index;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref Team);
                serializer.SerializeValue(ref AudioEvent);
                serializer.SerializeValue(ref AudioEventIndex);
            }
        }
    }
}

