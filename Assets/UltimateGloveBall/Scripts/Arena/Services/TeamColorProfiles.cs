// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System;
using System.Collections.Generic;
using Meta.Utilities;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UltimateGloveBall.Arena.Services
{
    /// <summary>
    /// Setup of the different colors for all the profiles. Each profiled is paired for team A and B.
    /// This helps simplify getting a random profile for the teams color and synchronize the colors for all clients. 
    /// </summary>
    public class TeamColorProfiles : Singleton<TeamColorProfiles>
    {
        [Serializable]
        private struct ColorProfile         // ColorProfile is a Tmap for an enum and a color
        {
            public TeamColor ColorKey;
            public Color Color;
        }

        [SerializeField] private List<ColorProfile> m_colorProfiles;          // a list of ColorProfile tmaps

        private readonly Dictionary<TeamColor, Color> m_colors = new();          // dunno why this is needed,  m_colorProfiles should already contain all this data 
        protected override void InternalAwake()
        {
            foreach (var colorProfile in m_colorProfiles)
            {
                m_colors[colorProfile.ColorKey] = colorProfile.Color;        // add to the Dictionary for each ColorProfile in list of ColorProfiles
            }
        }

        public Color GetColorForKey(TeamColor teamColor)     // color getter function for a TeamColor
        {
            return m_colors[teamColor];
        }

        public void GetRandomProfile(out TeamColor teamColorA, out TeamColor teamColorB)    // set var by ref,   get random team colors x2
        {
            var profileCount = (int)TeamColor.Count / 2;            // theres 8 possible colors,   therefore theres 4 profiles???
            var selectedProfile = Random.Range(0, profileCount);
            teamColorA = (TeamColor)(selectedProfile * 2);           // teamColorA gets random index in the second half of all colors
            teamColorB = teamColorA + 1;                               // TeamColorB becomes one color after??
        }
    }
}