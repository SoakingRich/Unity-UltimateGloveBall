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
   [Header("Settings")]
    public Material ErrorMaterial;
    
    [Header("State")]
    public NetworkVariable<int> NetColorID;
    public int ColorID => NetColorID.Value;
    public NetworkVariable<bool> NetIsHealthCube;
    public bool IsHealthCube => NetIsHealthCube.Value;
    public SceneCubeData m_SCData;
    public Action OnIntialized;

    public bool IsErrorCube
    {
        get => m_NetIsErrorCube.Value;
        set => m_NetIsErrorCube.Value = value;
    }
    public NetworkVariable<bool> m_NetIsErrorCube;

    [Header("Internal")]
    public HealthCubeTransform m_healthCubeTransform;
    public BlockamiData BlockamiData;
    public event System.Action<SceneCubeNetworking> SCDied;
    public event System.Action<SceneCubeNetworking> HCHit;
    public event System.Action<SceneCubeNetworking,ulong> SCDiedByPlayerCube;
    bool m_CubeisDead = true;
    [SerializeField, AutoSet] private Rigidbody m_rigidbody;
    private Material m_CubeMaterial;
   
    private static readonly int s_color = Shader.PropertyToID("_Color");
    public DrawingGrid OwningDrawingGrid;
    public GameObject Visual;
    public MeshRenderer rend;

    [Header("SquashStretch")]
    public FakeSoftCube m_fakeSoftCube;
    public squashStretch m_squashStretch;

[ContextMenu("InvokeSetErrorCube")]
    public void InvokedSetErrorCube()
    {
        TrySetErrorCube(!IsErrorCube);
    }
    
    public void TrySetErrorCube(bool Enable)
    {
        if (IsHealthCube || GetComponent<CubeBehavior>() )
        if (IsHealthCube )
        {
            return;    // health cubes or pickups cant be Error // actually they have to be included, otherwise spamming still works
        }
        
        
        IsErrorCube = Enable;

        if (IsErrorCube)
        {
            rend.material = ErrorMaterial;
            rend.material.SetColor(s_color, m_SCData.myColor);
            
            CancelInvoke("InvokedSetErrorCube");
            Invoke("InvokedSetErrorCube",5.0f);    // rest ErrorCube after 5 seconds
        }
        else
        {
            rend.material = m_CubeMaterial;
            m_CubeMaterial.SetColor(s_color, m_SCData.myColor); 
        }
        
        
    }

    private void OnEnable()
    {
        m_NetIsErrorCube.OnValueChanged += IsErrorCubeChanged;
    }

    private void OnDisable()
    {
        m_NetIsErrorCube.OnValueChanged -= IsErrorCubeChanged;
    }

    private void IsErrorCubeChanged(bool previousvalue, bool newvalue)
    {
      TrySetErrorCube(newvalue);
    }

    private void Awake()
    {
        // BlockamiData[] allBlockamiData = Resources.LoadAll<BlockamiData>("");
        // BlockamiData = System.Array.Find(allBlockamiData, data => data.name == "BlockamiData");
        BlockamiData = BlockamiData.Instance;

        rend = GetComponentInChildren<MeshRenderer>();
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

    public void Update()
    {
        // if (transform.position.y > 1.2)
        // {
        //     if (SpawnManager.Instance.OverflowingCube == null)
        //     {
        //         SpawnManager.Instance.OverflowingCube = this.gameObject;
        //     }
        // }
        // else
        // {
        //     if (SpawnManager.Instance.OverflowingCube == this.gameObject)
        //     {
        //         SpawnManager.Instance.OverflowingCube = null;
        //     }
        // }
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
                matchingHealthCubeTransform.InitializeWithHealthCube(this);
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

    
    
    
    
    [ContextMenu("LocalKillSceneCube")]
    public void LocalKillSceneCube()                       // non NetworkBehaviors need to destroy scene cubes sometimes, this function is called by them
    {
        KillSceneCubeServerRpc();
    }


    

    [ServerRpc(RequireOwnership = false)]
    public void KillSceneCubeServerRpc(ulong InstigatingPlayer = default)
    {
        if (IsHealthCube)
        {
     //     Debug.LogError("health cube being destroyed (by HealthPillar??)" );
    
        }
        
        if (m_CubeisDead) return;
        
        m_CubeisDead = true;
        SCDied?.Invoke(this); // spawnmanager does something with this
        SetPhysicsForSceneCube(false);

        if (InstigatingPlayer != default)
        {
            SCDiedByPlayerCube?.Invoke(this, InstigatingPlayer);
        }
    }

    
    
    
    public void HealthCubeHit()
    {
        HCHit?.Invoke(this);
    }
    
  
}


