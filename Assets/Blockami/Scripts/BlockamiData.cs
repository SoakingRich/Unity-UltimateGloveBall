using System;
using System.Collections.Generic;
using System.Linq;
using AYellowpaper.SerializedCollections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Blockami.Scripts
{
    [CreateAssetMenu(fileName = "BlockamiData", menuName = "ScriptableObjects/BlockamiData", order = 1)]
    public class BlockamiData : ScriptableObject
    {
        private static BlockamiData _instance;

        public static BlockamiData Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Load from Resources
                  //  _instance = Resources.Load<BlockamiData>("GameData");
                    BlockamiData[] allBlockamiData = Resources.LoadAll<BlockamiData>("");
                    _instance = System.Array.Find(allBlockamiData, data => data.name == "BlockamiData");
                    
                    // Create a runtime instance if it's null (optional fallback)
                    if (_instance == null)
                    {
                        _instance = CreateInstance<BlockamiData>();
                    }
                }
                return _instance;
            }
        }
        
        
        
        [Header("Prefabs")]
        public NetworkObject PlayerCubePrefab;
        
        [Header("Game")]
        public float DefaultSpawnRate = 1.0f;
        public float FrenzySpawnRate = 0.5f;
        public int MaxCubes = 100;
        public float FrenzyTimeDuration = 7.0f;
        public bool CycleColorsOnDraw = true;
        
        [Header("PlayerCube")]
        public float PlayerCubeMoveSpeed = 0.1f;
        public float PlayerCubeShrinkRate = 0.0001f;
        public bool ShootCubesOnPinchTriggerRelease = true;
        public bool LetIncorrectPlayerCubesBounceBack = false;
        public bool HideSnapDots = false;
        
        [Header("CubeTypes")]
        [SerializedDictionary("ID", "Data")]
        public SerializedDictionary< int, SceneCubeData> AllCubeTypes;
        [SerializeField] public int MaxNormalColorID = 6;
        
        
        [Header("SquashStretch")] 
        public  float MoveTowardsMax =  0.005f;
        public  Vector3 halfExtents = new Vector3(0.2f, 0.105f, 0.2f);
        public   float springDamper = 0.1f;
        public   float springStrength = 12500f;
        public   float m_timeBeforeEnsureDeactivate = 3.0f;
        public   float m_timeBeforeDeactivate = 1.0f;
        public   float speed = 10.0f;
        
        
        private Vector3 OriginalScale;

        [Header("Debug")]
        public static bool LockPlayerLocationToEditorObj = false;
      


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

        public int GetRandomColorID()
        {
            int rand = UnityEngine.Random.Range(0,MaxNormalColorID);
            return rand;
        }

        public SceneCubeData GetSceneCubeDataFromID(int ID)
        {
            SceneCubeData scd = new SceneCubeData();
            if (AllCubeTypes.TryGetValue(ID, out SceneCubeData foundData))
            {
                // If found, assign the found data to scd
                scd = foundData;
            }
            return scd;
        }

        public Color GetColorFromColorID(int ID)
        {
            SceneCubeData scd = new SceneCubeData();
            if (AllCubeTypes.TryGetValue(ID, out SceneCubeData foundData))
            {
                // If found, assign the found data to scd
                scd = foundData;
            }
            return scd.myColor;
        }
    }
}





