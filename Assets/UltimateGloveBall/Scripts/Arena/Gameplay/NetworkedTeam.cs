// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using UltimateGloveBall.Arena.Player;
using Unity.Netcode;

namespace UltimateGloveBall.Arena.Gameplay
{
    /// <summary>
    /// Component that keeps the team information of a network object in sync between clients.
    /// When the team changes we also update the movement limits of the local player.
    /// </summary>
    public class NetworkedTeam : NetworkBehaviour             // a container for a Team value, that gets replicated,   but also takes action on PlayerMovement according to team
    {
        public enum Team    // define a team
        {
            NoTeam,
            TeamA,
            TeamB,
            TeamC,
            TeamD,
        }
        public Team MyTeam { get => m_team.Value; set => m_team.Value = value; }    // give this script a public accessible team variable that references a private one next

        private NetworkVariable<Team> m_team = new();        // private team variable, this is also networked. It only gets set to anything via the public var 


        
        
        
        
        
        
        public override void OnNetworkSpawn()         // on any client connect
        {
            if (IsOwner)
            {
                m_team.OnValueChanged += OnTeamChanged;     
                OnTeamChanged(m_team.Value, m_team.Value);       // run intial OnTeamChanged function
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                m_team.OnValueChanged -= OnTeamChanged;         //  remove   this.OnTeamChanged() function when private replicated variable changes
            }
        }

        
        
        private void OnTeamChanged(Team previousvalue, Team newvalue)
        {
            // Update movement limits when we switch teams on local player
            if (newvalue == Team.TeamA)
            {
                PlayerMovement.Instance.SetLimits(-4.5f, 4.5f, -9, -1);             // get the player movement instance and limit its movement to hardcoded position limits
            }
            else if (newvalue == Team.TeamB)
            {
                PlayerMovement.Instance.SetLimits(-4.5f, 4.5f, 1, 9);
            }
            else
            {
                PlayerMovement.Instance.SetLimits(-4.5f, 4.5f, -9, 9);
            }
        }
    }
}