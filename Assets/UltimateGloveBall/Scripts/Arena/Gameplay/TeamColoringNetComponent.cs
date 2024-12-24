// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System.Collections.Generic;
using UltimateGloveBall.Arena.Services;
using Unity.Netcode;
using UnityEngine;

namespace UltimateGloveBall.Arena.Gameplay
{
    /// <summary>
    /// Sync team color for colored renderers to all clients. This component will color all renderers listed with the
    /// associated TeamColor.
    /// </summary>
    public class TeamColoringNetComponent : NetworkBehaviour           // this script changes a material param _BaseColor on all renderers in an public array on owning gO
    {
        private static readonly int s_colorProperty = Shader.PropertyToID("_BaseColor");

        [SerializeField] private List<Renderer> m_renderers;

        private NetworkVariable<TeamColor> m_teamColor = new();       // replicated variable

        public TeamColor TeamColor            // the value of this will be set by GameManager, TeamColor is a local accessor for networked var m_teamColor above, and it will trigger rep notify func here in Setter
        {
            get => m_teamColor.Value;
            set => m_teamColor.Value = value;
        }

        public override void OnNetworkSpawn()
        {
            UpdateColor(TeamColor);                            // i cant see anywhere TeamColor could ever have been set by Spawn time, but rep notify function should work correctly to set color later i guess
            m_teamColor.OnValueChanged += OnTeamColorChanged;    // bind the rep notify func for m_teamColor
        }

        private void OnTeamColorChanged(TeamColor previousvalue, TeamColor newvalue)
        {
            UpdateColor(newvalue);
        }

        private void UpdateColor(TeamColor color)          // TeamColor is an enum defined in its own script for some reason
        {
            foreach (var rend in m_renderers)               // set color on materials on a bunch of renderers,   without the need for a material block for some reason?? 
            {
                rend.material.SetColor(s_colorProperty,
                    TeamColorProfiles.Instance.GetColorForKey(color));    // convert TeamColor enum to an actual color
            }
        }
    }
}