// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using Unity.Netcode;

namespace UltimateGloveBall.Arena.Services
{
    /// <summary>
    /// Handles client disconnect from the arena.
    /// </summary>
    public class ArenaServerHandler : NetworkBehaviour             // this class only exists to run DisconnectClient on ArenaSessionManager when Server themselves disconnects
    {
        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;         // only server
            }
        }
        
        
        

        public override void OnNetworkDespawn()
        {
            if (NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }
        
        
        
        

        private void OnClientDisconnected(ulong clientId)
        {
            if (clientId == OwnerClientId)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;           // If the server themselves is the disconnecting client, dont listen to OnClientDisconnect anymore
            }
            else
            {
                ArenaSessionManager.Instance.DisconnectClient(clientId);               // else run ArenaSessionManager disconnect client
            }
        }
    }
}