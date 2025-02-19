// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using UltimateGloveBall.Design;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UltimateGloveBall.Arena.Balls
{
    /// <summary>
    /// Handles the ball spinning behaviour. We can change the state of the ball and this behaviour will handle
    /// the spining visual of the gameobject.
    /// </summary>
    public class BallSpinVisual : MonoBehaviour
    {
        public enum SpinState
        {
            Spawned,
            Holding,
            Thrown,
            Hit,
        }

        [SerializeField] private Transform m_visualRoot;

        private BallData m_ballData;

        private SpinState m_currentState;
        private Vector3 m_thrownRotation;

        public void Init(BallData ballData)    // ballNetworking calls init for visuals with info from balldata scriptable object         // not really clear why BallNetworking should handle this
        {
            m_ballData = ballData;
        }

        public void SetState(SpinState state, float forcePct = 0)          // ball is in a new state
        {
            m_currentState = state;
            switch (state)
            {
                case SpinState.Thrown:
                    m_thrownRotation = Vector3.Lerp(m_ballData.ThrownRotationPerSecMin, m_ballData.ThrownRotationPerSecMax,           // thrown rotation gets modified for some reason on Throw
                        forcePct);
                    break;
                case SpinState.Spawned:
                    // to avoid balls being in sync we set them at a random start point
                    ProcessSpawned(Random.Range(0, 10f));            // make them randomly rotated to begin with
                    break;
                case SpinState.Holding:
                    m_visualRoot.localRotation = Quaternion.identity;
                    break;
                case SpinState.Hit:
                    break;
                default:
                    break;
            }
        }

        private void Update()
        {
            switch (m_currentState)
            {
                case SpinState.Spawned:
                    ProcessSpawned(Time.deltaTime);     // while sitting spawned, rotate by delta time
                    break;
                case SpinState.Hit:
                case SpinState.Holding:
                    // we do no rotation on holding or after a hit
                    break;
                case SpinState.Thrown:
                    ProcessThrown(Time.deltaTime);    // while being thrown spin by delta time, also with m_throwRotation
                    break;
                default:
                    break;
            }
        }

        private void ProcessSpawned(float dt)
        {
            ApplyRotation(m_ballData.SpawnedRotationSpeedPerSec * dt);
        }

        private void ProcessThrown(float dt)
        {
            ApplyRotation(m_thrownRotation * dt);
        }

        private void ApplyRotation(Vector3 eulerAngle)
        {
            m_visualRoot.localRotation *= Quaternion.Euler(eulerAngle);
        }
    }
}