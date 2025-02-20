using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorOnlySettings : MonoBehaviour
{
    public int m_instanceMaxSceneCubes;

    private void Start()
    {
        if (!enabled) return;
        SpawnManager.Instance.maxSceneCubes = m_instanceMaxSceneCubes;
    }
}
