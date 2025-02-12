// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using UnityEngine;
#if !UNITY_EDITOR && !UNITY_STANDALONE_WIN
// using System.Threading.Tasks;
// using Oculus.Platform;
// using Meta.Utilities;
#endif
using System.Threading.Tasks;
using Oculus.Platform;
using Meta.Utilities;

namespace Meta.Multiplayer.Core
{
    /// <summary>
    /// Keeps track of the current group presence state and uses the GroupPresence API from Oculus Platform.
    /// Usage: Set the group presence of the player
    /// https://developer.oculus.com/documentation/unity/ps-group-presence-overview/
    /// </summary>
    public class GroupPresenceState                                  // i believe UGB only uses LobbySessionIDs and not MatchSessionIDs.  eg ("ArenaSoakingRich0537984")
    {
        public string Destination { get; private set; }
        public string LobbySessionID { get; private set; }
        public string MatchSessionID { get; private set; }
        public bool IsJoinable { get; private set; }

        public IEnumerator Set(string dest, string lobbyID, string matchID, bool joinable)
        {
            ///// I DONT KNOW IF THIS IS FUNCTIONAL I SHOULD PROBABLY COPY CODE FROM
            ///     https://github.com/oculus-samples/Unity-SharedSpaces/blob/main/Assets/SharedSpaces/Scripts/SharedSpacesGroupPresenceState.cs
            
#if !UNITY_EDITOR && !UNITY_STANDALONE_WIN
            return Impl().ToRoutine();
            
            async Task Impl()
            {
                var groupPresenceOptions = new GroupPresenceOptions();
                if (dest is not null)
                    groupPresenceOptions.SetDestinationApiName(dest);
                if (lobbyID is not null)
                    groupPresenceOptions.SetLobbySessionId(lobbyID);
                if (matchID is not null)
                    groupPresenceOptions.SetMatchSessionId(matchID);
                groupPresenceOptions.SetIsJoinable(joinable);

                GroupPresence.Set(groupPresenceOptions);
                OnSetComplete();

                // // temporary workaround until bug fix
                // // GroupPresence.Set() can sometimes fail. Wait until it is done, and if it
                // // failed, try again.
                // while (true)
                // {
                //     Debug.Log("Setting Group Presence...");
                //
                //     var request = lobbyID is null ?
                //         GroupPresence.Clear() :
                //         GroupPresence.Set(groupPresenceOptions);
                //     var message = await request.Gen();
                //
                //     if (message.IsError)
                //     {
                //         LogError("Failed to setup Group Presence", message.GetError());
                //         continue;
                //     }
                //
                //     OnSetComplete();
                //     break;
                // }
            }
#else
          
            OnSetComplete();
            yield break;
#endif

            void OnSetComplete()
            {
                Destination = dest;
                LobbySessionID = lobbyID;
                MatchSessionID = matchID;
                IsJoinable = joinable;

                Debug.Log("Group Presence set successfully");
                Print();
            }
        }

        public void Print()
        {
            Debug.Log(@$"------GROUP PRESENCE STATE------
Destination:      {Destination}
Lobby Session ID: {LobbySessionID}
Match Session ID: {MatchSessionID}
Joinable?:        {IsJoinable}
--------------------------------");
        }

        private void LogError(string message, Oculus.Platform.Models.Error error)
        {
            Debug.LogError(@"{message}
ERROR MESSAGE:   {error.Message}
ERROR CODE:      {error.Code}
ERROR HTTP CODE: {error.HttpCode}");
        }
    }
}