using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Blockami.Scripts
{
    [CreateAssetMenu(fileName = "BlockamiData", menuName = "ScriptableObjects/BlockamiData", order = 1)]
    public class BlockamiData : ScriptableObject
    {

        public NetworkObject PlayerCubePrefab;
        
        public float DefaultSpawnRate = 1.0f;
        public float FrenzySpawnRate = 0.5f;
        public int MaxCubes = 100;
        public float SpecialCubeSpawnChance = 0.1f; // 10% chance
        
        public float PlayerCubeMoveSpeed = 0.1f;
        public float PlayerCubeShrinkRate = 0.0001f;

        private Vector3 OriginalScale;


        [System.Serializable]
        public struct ColorType : INetworkSerializable
        {
            public Color color;
            public int ID;
            
            public static bool operator ==(ColorType left, ColorType right)
            {
                return left.Equals(right);
            }
            
            public static bool operator !=(ColorType left, ColorType right)
            {
                return !left.Equals(right);
            }


            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                // Serialize individual components of Color
                serializer.SerializeValue(ref color.r);
                serializer.SerializeValue(ref color.g);
                serializer.SerializeValue(ref color.b);
                serializer.SerializeValue(ref color.a);
                serializer.SerializeValue(ref ID);
            }
        }

        [SerializeField] public List<ColorType> m_ColorTypes; // array of all colortypes
        

        public ColorType GetRandomColor()
        {
            return m_ColorTypes[Random.Range(0, m_ColorTypes.Count)];
        }






        [Serializable]
        public struct CubeWeight // different scenecubeprefabs have different weights
        {
            public NetworkObject SceneCubePrefab;
            public int Weight;
        }

        [SerializeField] private List<CubeWeight> m_CubeWeights; // array of all weights



        [Header("Internal")] private int m_weightcount;
        private int m_totalWeight;






        private void Awake()
        {
         
            Initialize();
        }

        public void Initialize()
        {
            Rebuild();
        }

        [ContextMenu("Update Data")]
        private void Rebuild()
        {
            m_totalWeight = 0;
            foreach (var cubeweight in m_CubeWeights)
            {
                m_totalWeight +=
                    cubeweight.Weight; // set m_totalWeight to sum of all spawnable cubes weight. 
            }

            m_weightcount = m_CubeWeights.Count; // set m_ballCount to array Length
        }


        public NetworkObject GetRandomCube()
        {
            if (m_weightcount != m_CubeWeights.Count)
            {
                Rebuild();
            } // if ballcount is not equal to length of the array for some reason, rebuild

            var rng = Random.Range(0, m_totalWeight);
            var cumWeight = 0;

            for (var i = 0;
                 i < m_weightcount;
                 i++) // get a RandNum between 0 and 20,  loop through each array element and if accumulated weight has gone over RandNum, stop accumulating and return prefab
            {
                var cubeInfo = m_CubeWeights[i];
                cumWeight += cubeInfo.Weight; // 
                if (rng <= cumWeight)
                {
                    return cubeInfo.SceneCubePrefab;
                }
            }

            // if we didn't find a ball we take the default one
            return m_CubeWeights[0].SceneCubePrefab;
        }

    }
}




