// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using Meta.Utilities;
using Meta.Multiplayer.Core;
using Unity.Netcode;
using UnityEngine;
using static Oculus.Avatar2.OvrAvatarEntity;

using Stopwatch = System.Diagnostics.Stopwatch;

namespace Meta.Multiplayer.Avatar
{
    /// <summary>
    /// Handles the networking of the Avatar.
    /// Local avatars will send their state to other users through rpcs.
    /// For remote avatars we receive the state rpc and apply it to the avatar entity.
    /// </summary>
    public class AvatarNetworking : NetworkBehaviour
    {
        private const float PLAYBACK_SMOOTH_FACTOR = 0.25f;

        [Serializable]
        private struct LodFrequency                        // update each quality level LOD at a different frequency
        {
            public StreamLOD LOD;
            public float UpdateFrequency;
        }
        [SerializeField] private List<LodFrequency> m_updateFrequenySecondsByLodList;
        [SerializeField] private float m_streamDelayMultiplier = 0.5f;

        private NetworkVariable<ulong> m_userId = new(ulong.MaxValue, writePerm: NetworkVariableWritePermission.Owner);        // replicate userID for this avatar
        public ulong UserId                                                                                                             // accessor for UserID
        {
            get => m_userId.Value;
            set => m_userId.Value = value;
        }
        
        private Stopwatch m_streamDelayWatch = new();                      // stopwatch to count down to when to update stream??
        private float m_currentStreamDelay;

        private Dictionary<StreamLOD, float> m_updateFrequencySecondsByLod;          // dictionary of all LOD levels and update frequencies
        private Dictionary<StreamLOD, double> m_lastUpdateTime = new();
        
       
        
        [SerializeField, AutoSet] private AvatarEntity m_entity;

        
        
        
        
        
        
        
      

        public void Init()                                                                   // called by AvatarEntity.cs
        {
            m_updateFrequencySecondsByLod = new Dictionary<StreamLOD, float>();
            
            foreach (var val in m_updateFrequenySecondsByLodList)        // LOD Frequencies are a list in the inspector, convert here to a dictionary
            {
                m_updateFrequencySecondsByLod[val.LOD] = val.UpdateFrequency;
                m_lastUpdateTime[val.LOD] = 0;
            }
            
            if (!m_entity.IsLocal)                                   // if its a proxy avatar
            {
                m_userId.OnValueChanged += OnUserIdChanged;                  // do something when UserID changes

                if (m_userId.Value != ulong.MaxValue)
                {
                    OnUserIdChanged(ulong.MaxValue, m_userId.Value);        // if UserID is already non-default, do OnUserIdChanged
                }
            }
        }
        
        

        private void OnUserIdChanged(ulong previousValue, ulong newValue)
        {
            m_entity.LoadUser(newValue);                                                         // on network change of ID, get Avatar to loaduser
        }
        
        

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            m_userId.OnValueChanged?.Invoke(ulong.MaxValue, m_userId.Value);          // assume Init has already run i guess??
            
            m_entity.Initialize();                                                               // do main init for Avatar
        }
        
        
        

        private void Update()                                          // track the Avatar exactly to the CameraRig transform
        {
            if (m_entity && m_entity.IsLocal)
            {
                var rigTransform = CameraRigRef.Instance.transform;
                transform.SetPositionAndRotation(
                    rigTransform.position,
                    rigTransform.rotation);

                UpdateDataStream();                                   // update streaming data
            }
        }
        
        
        

        private void UpdateDataStream()
        {
            if (isActiveAndEnabled)
            {
                if (m_entity.IsCreated && m_entity.HasJoints && NetworkObject?.IsSpawned is true)
                {
                    var now = Time.unscaledTimeAsDouble;
                    var lod = StreamLOD.Low;
                    double timeSinceLastUpdate = default;                  //  set timeSinceLastUpdate to default
                    foreach (var lastUpdateKvp in m_lastUpdateTime)
                    {
                        var lastLod = lastUpdateKvp.Key;
                        var time = now - lastUpdateKvp.Value;
                        var frequency = m_updateFrequencySecondsByLod[lastLod];
                        if (time >= frequency)
                        {
                            if (time > timeSinceLastUpdate)
                            {
                                timeSinceLastUpdate = time;                 // record the time if an update is happening
                                lod = lastLod;
                            }
                        }
                    }

                    if (timeSinceLastUpdate != default)                 // an update is happening this frame
                    {
                        // act like every lower frequency lod got updated too
                        var lodFrequency = m_updateFrequencySecondsByLod[lod];
                        foreach (var lodFreqKvp in m_updateFrequencySecondsByLod)
                        {
                            if (lodFreqKvp.Value <= lodFrequency)
                            {
                                m_lastUpdateTime[lodFreqKvp.Key] = now;          // mark an update for every LOD even thought theyre not all currently being used
                            }
                        }

                        SendAvatarData(lod);                           // send a batch of data
                    }
                }
            }
        }

        private void SendAvatarData(StreamLOD lod)
        {
            var bytes = m_entity.RecordStreamData(lod);     // record data to be sent, Snapshot
            SendAvatarData_ServerRpc(bytes);                        // send unreliable RPC
        }
        
        

        [ServerRpc(Delivery = RpcDelivery.Unreliable)]
        private void SendAvatarData_ServerRpc(byte[] data, ServerRpcParams args = default)
        {
            var allClients = NetworkManager.Singleton.ConnectedClientsIds;
            var targetClients = allClients.Except(args.Receive.SenderClientId).ToTempArray(allClients.Count - 1);
            SendAvatarData_ClientRpc(data, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIdsNativeArray = targetClients } });           // RPC to all clients
        }

        [ClientRpc(Delivery = RpcDelivery.Unreliable)]
        private void SendAvatarData_ClientRpc(byte[] data, ClientRpcParams args)
        {
            ReceiveAvatarData(data);              // receive the data on the client
        }

        
        
        
        private void ReceiveAvatarData(byte[] data)
        {
            if (!m_entity)
            {
                return;
            }

            var latency = (float)m_streamDelayWatch.Elapsed.TotalSeconds;

            m_entity.ApplyStreamData(data);          // apply byte data by Avatar

            var delay = Mathf.Clamp01(latency * m_streamDelayMultiplier);
            m_currentStreamDelay = Mathf.LerpUnclamped(m_currentStreamDelay, delay, PLAYBACK_SMOOTH_FACTOR);
            m_entity.SetPlaybackTimeDelay(m_currentStreamDelay);
            m_streamDelayWatch.Restart();
        }
    }
}
