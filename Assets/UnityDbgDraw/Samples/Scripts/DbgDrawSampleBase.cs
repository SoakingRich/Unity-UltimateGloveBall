// DbgDraw for Unity. Copyright (c) 2019 Peter Schraut (www.console-dev.de). See LICENSE.md
// https://github.com/pschraut/UnityDbgDraw
using UnityEngine;
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0018 // Variable declaration can be inlined
#pragma warning disable IDE0017 // Object initialization can be simplified

namespace Oddworm.Samples
{
    class DbgDrawSampleBase : MonoBehaviour
    {
        [Tooltip("A reference to a Tranform that is moved around in a robot searching like pattern.")]
        [SerializeField] protected Transform m_Robot = null;

        protected virtual void Start()
        {
            if (m_Robot == null)
                Debug.LogError("The 'Robot' field has not been assigned in the Inspector.", this);

            UpdateRobot();
        }

        protected bool UpdateRobot()
        {
            if (m_Robot == null)
                return false;

            var oldPosition = m_Robot.position;
            var newPosition = Vector3.forward * (Mathf.PerlinNoise(Time.time * 0.5f, 1) * 2 - 1) +
                Vector3.right * (Mathf.PerlinNoise(Time.time, 0) * 2 - 1) +
                Quaternion.AngleAxis(Time.time * 30, Vector3.up) * Vector3.right * 5;

            var direction = (newPosition - oldPosition).normalized;
            m_Robot.LookAt(newPosition + direction);
            m_Robot.position = newPosition;

            return true;
        }
    }
}
