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
    class DbgDrawSample_Lines : DbgDrawSampleBase
    {
        List<Vector3> m_Lines = new List<Vector3>();

        void Update()
        {
            var oldPosition = m_Robot.position;

            if (!UpdateRobot())
                return;

            m_Lines.Add(oldPosition);
            m_Lines.Add(m_Robot.position);
            if (m_Lines.Count > 256)
                m_Lines.RemoveRange(0, 2);

            // draws a list of lines
            DbgDraw.Lines(m_Lines, Color.yellow);
        }
    }
}
