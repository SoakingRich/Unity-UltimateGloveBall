using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blockami.Scripts;
using Meta.Utilities;
using ReadyPlayerMe.Samples.AvatarCreatorWizard;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class SceneCubeNetworking : NetworkBehaviour
{
   
    
    [Header("State")]
    public NetworkVariable<int> NetColorID;
    public int ColorID => NetColorID.Value;
    public NetworkVariable<bool> NetIsHealthCube;
    public bool IsHealthCube => NetIsHealthCube.Value;
    public SceneCubeData m_SCData;
    public Action OnIntialized;

    [Header("Internal")]
    public BlockamiData BlockamiData;
    public event System.Action<SceneCubeNetworking> SCDied;
    bool m_CubeisDead = true;
    [SerializeField, AutoSet] private Rigidbody m_rigidbody;
    private Material m_CubeMaterial;
    private static readonly int s_color = Shader.PropertyToID("_Color");
    public DrawingGrid OwningDrawingGrid;

    [Header("SquashStretch")]
    public FakeSoftCube m_fakeSoftCube;
    public squashStretch m_squashStretch;




    private void Awake()
    {
        BlockamiData[] allBlockamiData = Resources.LoadAll<BlockamiData>("");
        BlockamiData = System.Array.Find(allBlockamiData, data => data.name == "BlockamiData");
        
        m_CubeMaterial = GetComponentInChildren<MeshRenderer>().material;
        m_fakeSoftCube = GetComponentInChildren<FakeSoftCube>();
        m_squashStretch = GetComponentInChildren<squashStretch>();
        NetColorID.OnValueChanged += OnColorIDChanged;
        var GameManager = UltimateGloveBall.Arena.Gameplay.GameManager._instance;
        GameManager.OnEmojiCubeHitFloor += OnEmojiCubeHitFloor;
    }

    private void OnEmojiCubeHitFloor()
    {
        if (!NetworkManager.Singleton.IsServer && NetworkManager.Singleton.IsConnectedClient ) return;
        
        GameObject staging = GameObject.FindGameObjectWithTag("Staging");
        var box = staging.GetComponent<BoxCollider>();
        if ((transform.position.y - box.transform.position.y) - box.size.y < 1.0f)
        {
            KillSceneCubeServerRpc();
        }
        
    }

    private void OnColorIDChanged(int previousvalue, int newvalue)
    {
        Initialize();
    }

  
    public void Initialize()
    {
        m_SCData = BlockamiData.GetSceneCubeDataFromID(ColorID);
        
        m_CubeisDead = false;
        m_CubeMaterial.SetColor(s_color, m_SCData.myColor);    // currently reads color from cubedata,  should just use some color list somewhere
        m_fakeSoftCube.deactivated = false;

         SetPhysicsForSceneCube(!IsHealthCube);

        if (IsHealthCube)
        {
            var matchingHealthCubeTransform = FindObjectsOfType<HealthCubeTransform>()
                .FirstOrDefault(hct => Vector3.Distance(hct.transform.position, this.transform.position) < 0.1f); // Set tolerance threshold here


            if (matchingHealthCubeTransform != null)
            {
                OwningDrawingGrid = matchingHealthCubeTransform.OwningDrawingGrid;
                matchingHealthCubeTransform.IntializeWithHealthCube(this);
            }
        }
        
        OnIntialized?.Invoke();
        
    }

    private void SetPhysicsForSceneCube(bool EnablePhysics = true)
    {
        if (EnablePhysics)
        {
            m_rigidbody.velocity = Vector3.zero;
            m_rigidbody.isKinematic = false;
            m_rigidbody.useGravity = true;
        }
        else
        {
            m_rigidbody.isKinematic = true;
            m_rigidbody.useGravity = false;
        }
      
    }

    [ContextMenu("EditorKillSceneCube")]
    public void EditorKillSceneCube()
    {
        KillSceneCubeServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void KillSceneCubeServerRpc()
    {
        if (m_CubeisDead) return;
        
        m_CubeisDead = true;
        SCDied?.Invoke(this); // spawnmanager does something with this
        SetPhysicsForSceneCube(false);
    }

  
}


