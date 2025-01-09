using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DebugUtils
{
    public static void DrawSphere(Vector3 position, float radius = 1.0f, Color color = default, int segments = 12)
    {
          // Set color to Color.red if it's the default (i.e., Color.clear)
        if (color == default)
        {
            color = Color.red;
        }
        
        
        float angleStep = 360f / segments;

        // Draw circles in XZ, XY, and YZ planes
        for (int i = 0; i < segments; i++)
        {
            float angle1 = Mathf.Deg2Rad * angleStep * i;
            float angle2 = Mathf.Deg2Rad * angleStep * (i + 1);

            // XZ Plane
            Debug.DrawLine(
                position + new Vector3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius),
                position + new Vector3(Mathf.Cos(angle2) * radius, 0, Mathf.Sin(angle2) * radius),
                color
            );

            // XY Plane
            Debug.DrawLine(
                position + new Vector3(Mathf.Cos(angle1) * radius, Mathf.Sin(angle1) * radius, 0),
                position + new Vector3(Mathf.Cos(angle2) * radius, Mathf.Sin(angle2) * radius, 0),
                color
            );

            // YZ Plane
            Debug.DrawLine(
                position + new Vector3(0, Mathf.Cos(angle1) * radius, Mathf.Sin(angle1) * radius),
                position + new Vector3(0, Mathf.Cos(angle2) * radius, Mathf.Sin(angle2) * radius),
                color
            );
        }
    }
}

