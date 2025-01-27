// DbgDraw for Unity. Copyright (c) 2019 Peter Schraut (www.console-dev.de). See LICENSE.md
// https://github.com/pschraut/UnityDbgDraw
using UnityEngine;
using Oddworm.Framework;
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0018 // Variable declaration can be inlined
#pragma warning disable IDE0017 // Object initialization can be simplified

namespace Oddworm.Samples
{
    class DbgDrawSample_Arc : DbgDrawSampleBase
    {
        [SerializeField] float m_InnerRadius = 1;
        [SerializeField] float m_OuterRadius = 10;
        [Tooltip("Visualize the field of vision, Angle specified in degrees.")]
        [SerializeField] float m_Angle = 60;

        void Update()
        {
            if (!UpdateRobot())
                return;

            // Draws a circular arc in 3D space to visualize the robot field of vision
            DbgDraw.Arc(m_Robot.position, // The center of the circle
                m_Robot.rotation, // The rotation of the circle
                m_Robot.forward,  // The direction of the point on the circle circumference, relative to the center, where the arc begins.
                m_Angle * -0.5f,  // The starting angle of the arc, relative to 'from', in degrees.
                m_Angle * +0.5f,  // The ending angle of the arc, relative to 'from', in degrees.
                m_InnerRadius,    // The inner radius of the circle.
                m_OuterRadius,    // The outer radius of the circle
                new Color(1, 0, 0, 0.5f)); // The color

            DbgDraw.WireArc(m_Robot.position, m_Robot.rotation, m_Robot.forward, m_Angle * -0.5f, m_Angle * +0.5f, m_InnerRadius, m_OuterRadius, new Color(0, 0, 0, 0.5f));
        }
    }
}
