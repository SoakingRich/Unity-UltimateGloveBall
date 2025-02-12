using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct LineDrawer
{
    private LineRenderer lineRenderer;
    public float lineSize;
    public GameObject lineObj;

    public LineDrawer(float lineSize = 0.01f)
    {
        lineObj = new GameObject("LineObj");
        lineRenderer = lineObj.AddComponent<LineRenderer>();
        //Particles/Additive
        lineRenderer.material = new Material(Shader.Find("Hidden/Internal-Colored"));

        this.lineSize = lineSize;
        
        lineObj.gameObject.SetActive(false);
    }

    private void init(float lineSize = 0.2f)
    {
        if (lineRenderer == null)
        {
            lineObj = new GameObject("LineObj");
            lineRenderer = lineObj.AddComponent<LineRenderer>();
            //Particles/Additive
            lineRenderer.material = new Material(Shader.Find("Hidden/Internal-Colored"));

            this.lineSize = lineSize;
        }

        lineObj.gameObject.SetActive(false);
    }

    private void Start()
    {
        if (lineObj)
        {
            lineObj.gameObject.SetActive(false);
        }
    }

    //Draws lines through the provided vertices
    public void DrawLineInGameView(Vector3 start, Vector3 end, Color color, float LineSize = 0.0f)
    {
        if (lineRenderer == null)
        {
            init(0.2f);
        }
        
        lineObj.gameObject.SetActive(true);

        //Set color
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;

        //Set width
        lineRenderer.startWidth = LineSize == 0.0f? lineSize : LineSize;
        lineRenderer.endWidth =  LineSize == 0.0f? lineSize : LineSize;
        
        //Set line count which is 2
        lineRenderer.positionCount = 2;

        //Set the postion of both two lines
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }

    public void Destroy()
    {
        if (lineRenderer != null)
        {
            UnityEngine.Object.Destroy(lineRenderer.gameObject);
        }
    }
}

