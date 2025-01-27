// DbgDraw for Unity. Copyright (c) 2019 Peter Schraut (www.console-dev.de). See LICENSE.md
// https://github.com/pschraut/UnityDbgDraw
using UnityEngine;
using Oddworm.Framework;
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0018 // Variable declaration can be inlined
#pragma warning disable IDE0017 // Object initialization can be simplified

namespace Oddworm.Samples
{
    public class DbgDrawSample_Everything : MonoBehaviour
    {
        [SerializeField] bool m_DrawMatrix = false;

        [SerializeField] bool m_DrawArc = false;
        [SerializeField] Color m_ArcColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        [SerializeField] float m_ArcFromValue = 0;
        [SerializeField] float m_ArcToValue = 90;
        [SerializeField] float m_ArcInnerRadius = 0.5f;
        [SerializeField] float m_ArcOuterRadius = 2.0f;

        [SerializeField] bool m_DrawWireArc = false;
        [SerializeField] Color m_WireArcColor = new Color(0, 0, 0, 1);

        [SerializeField] bool m_DrawWireArrow = false;
        [SerializeField] Color m_WireArrowColor = new Color(0, 0, 0, 1);

        //[SerializeField] bool m_DrawCapsule = false;
        //[SerializeField] Color m_CapsuleColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        [SerializeField] float m_CapsuleRadius = 0.5f;
        [SerializeField] float m_CapsuleHeight = 1;

        [SerializeField] bool m_DrawWireCapsule = false;
        [SerializeField] Color m_WireCapsuleColor = new Color(0, 0, 0, 1);

        [SerializeField] bool m_DrawTube = false;
        [SerializeField] Color m_TubeColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

        [SerializeField] bool m_DrawWireTube = false;
        [SerializeField] Color m_WireTubeColor = new Color(0, 0, 0, 1);

        [SerializeField] bool m_DrawQuad = false;
        [SerializeField] Color m_QuadColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

        [SerializeField] bool m_DrawWireQuad = false;
        [SerializeField] Color m_WireQuadColor = new Color(0, 0, 0, 1);

        [SerializeField] bool m_DrawSphere = false;
        [SerializeField] Color m_SphereColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

        [SerializeField] bool m_DrawWireSphere = false;
        [SerializeField] Color m_WireSphereColor = new Color(0, 0, 0, 1);

        [SerializeField] bool m_DrawWireHemiphere = false;
        [SerializeField] Color m_WireHemisphereColor = new Color(0, 0, 0, 1);

        [SerializeField] bool m_DrawLine = false;
        [SerializeField] Color m_LineColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

        [SerializeField] bool m_DrawDisc = false;
        [SerializeField] Color m_DiscColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        [SerializeField] float m_DiscRadius = 0.5f;

        [SerializeField] bool m_DrawWireDisc = false;
        [SerializeField] Color m_WireDiscColor = new Color(0, 0, 0, 1);

        [SerializeField] bool m_DrawCube = false;
        [SerializeField] Color m_CubeColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

        [SerializeField] bool m_DrawWireCube = false;
        [SerializeField] Color m_WireCubeColor = new Color(0, 0, 0, 1);

        [SerializeField] bool m_DrawPyramid = false;
        [SerializeField] Color m_PyramidColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

        [SerializeField] bool m_DrawWirePyramid = false;
        [SerializeField] Color m_WirePyramidColor = new Color(0, 0, 0, 1);

        [SerializeField] bool m_DrawPlane = false;
        [SerializeField] Color m_PlaneColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

        void Update()
        {
            if (m_DrawMatrix)
                DbgDraw.Matrix(transform.localToWorldMatrix);

            if (m_DrawQuad)
                DbgDraw.Quad(transform.position, transform.rotation, transform.lossyScale, m_QuadColor);

            if (m_DrawWireQuad)
                DbgDraw.WireQuad(transform.position, transform.rotation, transform.lossyScale, m_WireQuadColor);

            if (m_DrawCube)
                DbgDraw.Cube(transform.position, transform.rotation, transform.lossyScale, m_CubeColor);

            if (m_DrawWireCube)
                DbgDraw.WireCube(transform.position, transform.rotation, transform.lossyScale, m_WireCubeColor);

            if (m_DrawPyramid)
                DbgDraw.Pyramid(transform.position, transform.rotation, transform.lossyScale, m_PyramidColor);

            if (m_DrawWirePyramid)
                DbgDraw.WirePyramid(transform.position, transform.rotation, transform.lossyScale, m_WirePyramidColor);

            if (m_DrawSphere)
                DbgDraw.Sphere(transform.position, transform.rotation, transform.lossyScale, m_SphereColor);

            if (m_DrawWireSphere)
                DbgDraw.WireSphere(transform.position, transform.rotation, transform.localScale, m_WireSphereColor);

            if (m_DrawWireHemiphere)
                DbgDraw.WireHemisphere(transform.position, transform.rotation, transform.localScale, m_WireHemisphereColor);

            if (m_DrawDisc)
                DbgDraw.Disc(transform.position, transform.rotation, m_DiscRadius, m_DiscColor);

            if (m_DrawWireDisc)
                DbgDraw.WireDisc(transform.position, transform.rotation, m_DiscRadius, m_WireDiscColor);

            if (m_DrawArc)
                DbgDraw.Arc(transform.position, transform.rotation, transform.forward, m_ArcFromValue, m_ArcToValue, m_ArcInnerRadius, m_ArcOuterRadius, m_ArcColor);

            if (m_DrawWireArc)
                DbgDraw.WireArc(transform.position, transform.rotation, transform.forward, m_ArcFromValue, m_ArcToValue, m_ArcInnerRadius, m_ArcOuterRadius, m_WireArcColor);

            if (m_DrawTube)
                DbgDraw.Tube(transform.position, transform.rotation, transform.lossyScale, m_TubeColor);

            if (m_DrawWireTube)
                DbgDraw.WireTube(transform.position, transform.rotation, transform.lossyScale, m_WireTubeColor);

            //if (m_DrawCapsule)
            //    DebugDraw.Capsule(transform.position, transform.rotation, transform.lossyScale, m_CapsuleColor);

            if (m_DrawWireCapsule)
                DbgDraw.WireCapsule(transform.position, transform.rotation, m_CapsuleRadius, m_CapsuleHeight, m_WireCapsuleColor);

            if (m_DrawWireArrow)
                DbgDraw.WireArrow(transform.position, transform.rotation, transform.lossyScale, m_WireArrowColor);

            if (m_DrawLine)
                DbgDraw.Line(transform.position, transform.position + transform.forward, m_LineColor);

            if (m_DrawPlane)
                DbgDraw.Plane(new Plane(transform.forward, 0), transform.position, transform.lossyScale, m_PlaneColor);
        }



        private void OnDrawGizmos()
        {
            //Gizmos.color = new Color(1, 1, 1, 1);
            //Gizmos.matrix = transform.localToWorldMatrix;
            //Gizmos.DrawCube(Vector3.zero, Vector3.one);
            //if (m_SphereMesh != null)
            //Gizmos.DrawMesh(m_SphereMesh, 0);

            //Gizmos.DrawSphere(Vector3.zero, 1*0.5f);

            //UnityEditor.Handles.color = new Color(1, 1, 1, 1);
            //UnityEditor.Handles.DrawSolidArc(transform.position, transform.up, transform.forward, m_ArcToValue, m_ArcOuterRadius);

            //UnityEditor.Handles.color = new Color(1, 0, 0, 1);
            //UnityEditor.Handles.DrawWireArc(transform.position, transform.up, transform.forward, m_ArcValue, m_OuterRadius);

            //UnityEditor.Handles.color = new Color(1, 0, 0, 1);
            //UnityEditor.Handles.DotHandleCap(-1, transform.position, transform.rotation, m_OuterRadius, EventType.Repaint);
            //UnityEditor.Handles.DrawWireDisc(transform.position, transform.up, m_OuterRadius);
            //UnityEditor.Handles.CircleHandleCap(-1, transform.position, transform.rotation, m_OuterRadius, EventType.Repaint);
        }
    }
}
