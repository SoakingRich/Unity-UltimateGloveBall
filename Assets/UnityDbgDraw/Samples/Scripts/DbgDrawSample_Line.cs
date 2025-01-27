// DbgDraw for Unity. Copyright (c) 2019 Peter Schraut (www.console-dev.de). See LICENSE.md
// https://github.com/pschraut/UnityDbgDraw
using UnityEngine;
using Oddworm.Framework;
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0018 // Variable declaration can be inlined
#pragma warning disable IDE0017 // Object initialization can be simplified

namespace Oddworm.Samples
{
    class DbgDrawSample_Line : DbgDrawSampleBase
    {
        void Update()
        {
            var oldPosition = m_Robot.position;

            if (!UpdateRobot())
                return;

            // draws a line from the old position to the new position and keeps it alive for 3 seconds
            DbgDraw.Line(oldPosition, m_Robot.position, Color.yellow, 3);
        }
    }
}
