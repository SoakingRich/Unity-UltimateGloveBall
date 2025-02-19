// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using ExitGames.Client.Photon;
using Meta.Utilities;
using Netcode.Transports.PhotonRealtime;
using Photon.Realtime;
using UnityEngine;

namespace UltimateGloveBall.App
{
    /// <summary>
    /// Implements functions used on Photon connection. Setting the right room options based on the application state.
    /// Exposes room properties for player slots open and spectator slots open.
    /// </summary>
    public class PhotonConnectionHandler : MonoBehaviour                            // add accessor functions for Photon info like GetHostRoomOptions
    {
        public const string SPECTATOR_SLOT_OPEN = "spec";
        public const string PLAYER_SLOT_OPEN = "ps";
        private const string OPEN_ROOM = "vis";

        private static bool IsSpectator => LocalPlayerState.Instance.IsSpectator;        // getter readonly

        [SerializeField, AutoSet] private PhotonRealtimeTransport m_photonRealtimeTransport;

        private void Start()
        {
            // here we customize the HostNewRoom function for our app
            m_photonRealtimeTransport.GetHostRoomOptionsFunc = GetHostRoomOptions;    // GetHostRoomOptionsFunc is expected to return RoomOptions with 2 params,  
            // here we customize join random room
            m_photonRealtimeTransport.GetRandomRoomParamsFunc = GetRandomRoomParams;   // GetRandomRoomParamsFunc is expected to return OpJoinRandomRoomParams with 1 param
        }

        private void OnDestroy()
        {
            m_photonRealtimeTransport.GetHostRoomOptionsFunc = null;           // remove bounded functions
            m_photonRealtimeTransport.GetRandomRoomParamsFunc = null;
        }

        // usePrivateRoom, maxPlayers arguments are likely to be called with internally by NetworkManager ?? 
        private RoomOptions GetHostRoomOptions(bool usePrivateRoom, byte maxPlayers)          // at top of script, we define keywords for room properties like Open_Room etc.  GetHostRoomOptions constructs itself here with our preferences
        {
            var roomOptions = new RoomOptions
            {
                CustomRoomPropertiesForLobby =
                        new[] { PLAYER_SLOT_OPEN, SPECTATOR_SLOT_OPEN, OPEN_ROOM },
                CustomRoomProperties = new Hashtable
                    {
                        { PLAYER_SLOT_OPEN, 1 },
                        { SPECTATOR_SLOT_OPEN, 1 },
                        { OPEN_ROOM, usePrivateRoom ? 0 : 1 }
                    },
                MaxPlayers = maxPlayers,
            };

            return roomOptions;
        }

        // custom RandomRoom func implementation
        private OpJoinRandomRoomParams GetRandomRoomParams(byte maxPlayers)
        {
            var opJoinRandomRoomParams = new OpJoinRandomRoomParams();
            if (IsSpectator)
            {
                var expectedCustomRoomProperties = new Hashtable { { SPECTATOR_SLOT_OPEN, 1 }, { OPEN_ROOM, 1 } };
                opJoinRandomRoomParams.ExpectedMaxPlayers = maxPlayers;
                opJoinRandomRoomParams.ExpectedCustomRoomProperties = expectedCustomRoomProperties;
            }
            else
            {
                var expectedCustomRoomProperties = new Hashtable { { PLAYER_SLOT_OPEN, 1 }, { OPEN_ROOM, 1 } };
                opJoinRandomRoomParams.ExpectedMaxPlayers = maxPlayers;
                opJoinRandomRoomParams.ExpectedCustomRoomProperties = expectedCustomRoomProperties;
            }

            return opJoinRandomRoomParams;
        }
    }
}