// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System;
using System.Collections;
using System.Collections.Generic;
using Meta.Multiplayer.Core;
using Oculus.Platform;
using UnityEngine;

namespace UltimateGloveBall.App
{
    /// <summary>
    /// Handles the current user presence.
    /// Loads and keeps track of the Destinations through the RichPresence API and extracts the deeplink message.
    /// Generate and exposes the current group presence state.
    /// </summary>
    public class PlayerPresenceHandler
    {
        private bool m_destinationReceived;
        private readonly Dictionary<string, string> m_destinationsAPIToDisplayName = new();
        private readonly Dictionary<string, string> m_destinationsAPIToRegion = new();
        private readonly Dictionary<string, string> m_regionToDestinationAPI = new();

        public GroupPresenceState GroupPresenceState { get; private set; }

        public IEnumerator Init()
        {
            _ = RichPresence.GetDestinations().OnComplete(OnGetDestinations);       // do an ovrrequest call to GetDestinations, and simultaneously bind OnGetDestinations to onComplete
            Debug.Log("wait for Destinations to be recieved");
            yield return new WaitUntil(() => m_destinationReceived);               // wait for a m_bool to be set true   in  OnGetDestinations
        }


        // a new Group Presence should be made everytime the player has moved to a new place which may or may not be joinable
        public IEnumerator GenerateNewGroupPresence(string dest, string roomName = null)                 // a group presence is a string name ie.  'MainMenu' and a Photon Room Name,  this is all we need to invite our friends places
        {
            GroupPresenceState ??= new GroupPresenceState();          // if null, create new one, otherwise do nothing
            var lobbyId = string.Empty;
            var joinable = false;              // by default make the players grouppresense unjoinable, and with no lobby id to PhotonConnect to 
            if (dest != "MainMenu")   // if dest is Arena
            {
                lobbyId = roomName ?? $"Arena-{LocalPlayerState.Instance.Username}-{(uint)(UnityEngine.Random.value * uint.MaxValue)}";           //    null-coalescing operator -  set lobbyID to this expression only if roomName is null
                                                                                                                                     // set it to Arena-<myusername>-randomInt
                joinable = true;                     // if its Arena, make it joinable            
            }
            return GroupPresenceState.Set(
                dest,
                lobbyId,
                string.Empty,
                joinable
            );
        }

        // Based on the region we are connected we use the right Arena Destination API
        public string GetArenaDestinationAPI(string region)
        {
            return !m_regionToDestinationAPI.TryGetValue(region, out var destAPI) ? "Arena" : destAPI;        // given a Region arg,  this func returns either "Arena" or  destinationAPI matching the arg region ie "Arena-Aus"
        }

        public string GetDestinationDisplayName(string destinationAPI)
        {
            if (!m_destinationsAPIToDisplayName.TryGetValue(destinationAPI, out var displayName))
            {
                displayName = destinationAPI;
            }

            return displayName;
        }

        public string GetRegionFromDestination(string destinationAPI)
        {
            if (!m_destinationsAPIToRegion.TryGetValue(destinationAPI, out var region))
            {
                region = "usw";             // if destination string to Region mapping has not been set, count it as USA
            }
            return region;
        }

        private void OnGetDestinations(Message<Oculus.Platform.Models.DestinationList> message)     // this func is bound to OvrRequest for RichPresence.GetDestinations()
        {
            try
            {
                Debug.Log("OnGetDestinations callback ");
                if (message.IsError)
                {
                    LogError("Could not get the list of destinations!", message.GetError());
                }
                else
                {
                    foreach (var destination in message.Data)
                    {
                        if (string.IsNullOrEmpty(destination.DeeplinkMessage))
                        {
                            Debug.Log("destination DeeplinkMessage is empty");
                            //  continue;
                        }


                        var apiName = destination.ApiName;           // for each destination, get the API name for the destination ( as it will have been entered on Oculus backend ) ( for this project it will be Arena_Aus etc.)
                        m_destinationsAPIToDisplayName[apiName] = destination.DisplayName;          //enter Destination.ApiName as key, and Destination.Display name as value
                                                                                                    // For Arenas we detect what region they are in by betting the region in the deeplink message        

                        // i think this is checking the Destinations of the app to see if one matches with some other deeplink message that the player launched with??
                        if (apiName.StartsWith("Arena"))
                        {
                            var msg = JsonUtility.FromJson<ArenaDeepLinkMessage>(destination.DeeplinkMessage);       // we must read the DeepLinkMessage of the destination  ??
                            m_destinationsAPIToRegion[apiName] = msg.Region;                // set key as API name, set value as Region, extracted from DeepLinkMessage   ( but i thought the region was embedded in the API name anyway??)
                            if (!string.IsNullOrEmpty(msg.Region))
                            {
                                m_regionToDestinationAPI[msg.Region] = apiName;             // set key as Region, and value as API name
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Exception occured in OnGetDestinations: " + e.Message);

            }
            finally
            {
                m_destinationReceived = true;
                Debug.Log("completed OnGetDestinations");
            }
        }

        private void LogError(string message, Oculus.Platform.Models.Error error)
        {
            Debug.LogError($"{message}" +
                           $"ERROR MESSAGE:   {error.Message}" +
                           $"ERROR CODE:      {error.Code}" +
                           $"ERROR HTTP CODE: {error.HttpCode}");
        }
    }
}