using System;
using System.Collections;
using System.Collections.Generic;
using Meta.Utilities;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class SceneCubeNetworking : NetworkBehaviour
{
    public NetworkVariable<SceneCubeData> SCData;

    public event System.Action<SceneCubeNetworking> SCDied;
    bool m_CubeisDead = true;

    [SerializeField, AutoSet] private Rigidbody m_rigidbody;

    private Material m_CubeMaterial;
    private static readonly int s_color = Shader.PropertyToID("_Color");

    public bool isHealthCube => SCData.Value.IsHealthCube;
    public int ColorID => SCData.Value.MyColorType.ID;







    private void Awake()
    {
        m_CubeMaterial = GetComponent<MeshRenderer>().material;
        SCData.OnValueChanged += OnSCDataChanged;
    }

    private void OnSCDataChanged(SceneCubeData previousvalue, SceneCubeData newvalue)
    {
        Initialize();
    }

    public void Initialize()
    {

        m_CubeisDead = false;
        m_CubeMaterial.SetColor(s_color, SCData.Value.MyColorType.color);

        m_rigidbody.isKinematic = false;
        m_rigidbody.useGravity = true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void KillSceneCubeServerRpc()
    {
        m_CubeisDead = true;
        SCDied?.Invoke(this); // spawnmanager
        m_rigidbody.isKinematic = true;
        m_rigidbody.useGravity = false;
    }

    private void UpdateVisuals(bool isDead)
    {
        // foreach (var ball in m_ballRenderers)                                                        // ball may have multiple ballRenderers such as the TripleBall
        //     ball.sharedMaterial = isDead ? m_deadMaterial : m_defaultMaterial;
    }

}


