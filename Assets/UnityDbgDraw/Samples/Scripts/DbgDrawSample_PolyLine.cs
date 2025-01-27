// DbgDraw for Unity. Copyright (c) 2019 Peter Schraut (www.console-dev.de). See LICENSE.md
// https://github.com/pschraut/UnityDbgDraw
using System.Collections.Generic;
using UnityEngine;
using Oddworm.Framework;
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0018 // Variable declaration can be inlined
#pragma warning disable IDE0017 // Object initialization can be simplified

namespace Oddworm.Samples
{
    class DbgDrawSample_PolyLine : DbgDrawSampleBase
    {
        List<Vector3> m_Points = new List<Vector3>();

        void Update()
        {
            if (!UpdateRobot())
                return;

            m_Points.Add(m_Robot.position);
            if (m_Points.Count > 256)
                m_Points.RemoveAt(0);

            DbgDraw.PolyLine(m_Points, Color.yellow);
        }
    }
}
