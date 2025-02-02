using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class DrawDebugTransform : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool enableDebug = true;
    public float lineLength = 1.0f;
    public float lineThickness = 0.02f;
    public float duration = 0;
    public Vector3 DebugLineStartHeightOffset = new Vector3(0.0f, -0.1f, 0.0f);

    public LineDrawer lineDrawer;

    void Update()
    {
        if (enableDebug)
        {
            DrawDebugAxes();
        }
    }

    private void OnDisable()
    {
       // lineDrawer.lineo
    }

    private void DrawDebugAxes()
    {
        // Transform position
        Vector3 DebugLineStart = transform.position;
        
        // Draw axes
        lineDrawer.DrawLineInGameView(DebugLineStart + DebugLineStartHeightOffset, DebugLineStart + transform.right * lineLength, Color.red, lineThickness); // X-axis
        lineDrawer.DrawLineInGameView(DebugLineStart + DebugLineStartHeightOffset, DebugLineStart + transform.up * lineLength, Color.green, lineThickness);  // Y-axis
        lineDrawer.DrawLineInGameView(DebugLineStart + DebugLineStartHeightOffset, DebugLineStart + transform.forward * lineLength, Color.blue, lineThickness); // Z-axis
    }
}