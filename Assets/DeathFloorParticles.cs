using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[ExecuteInEditMode]
public class DeathFloorParticles : MonoBehaviour
{
     [SerializeField] public ParticleSystem PSystem;
    [SerializeField]  public int NumOfParticles = 1; // Number of particles to emit (1 to 6)
    [SerializeField] public float spacing = 0.5f; // Distance between particles

    private ParticleSystem.Particle[] particles;
    [SerializeField]  public float m_startSize;
    [SerializeField]  public float m_rotation;
    public Vector3 m_rotation3D;

    private void Start()
    {
        if (PSystem == null)
            PSystem = GetComponent<ParticleSystem>();
    }

    public void SetParticleState(int NewNumOfParticles)
    {
        NumOfParticles = NewNumOfParticles;
        EmitParticles();
    }

    [ContextMenu("DebugEmitParticles")]
    private void EmitParticles()
    {
        return;   // too hard
        
        if (PSystem == null) return;

        int maxParticles = PSystem.main.maxParticles;
        if (particles == null || particles.Length < maxParticles)
            particles = new ParticleSystem.Particle[maxParticles];

        int particleCount = PSystem.GetParticles(particles);
        Vector3 startPos = transform.position;
        Quaternion rotation = transform.rotation;
        Vector3 direction = rotation * Vector3.forward;

        for (int i = 0; i < NumOfParticles; i++)
        {
            Vector3 targetPosition = startPos + direction * (i * spacing);
            targetPosition = PSystem.transform.InverseTransformPoint(targetPosition);
            
            bool canEmit = true;

            for (int j = 0; j < particleCount; j++)
            {
                if (Vector3.Distance(particles[j].position, targetPosition) < 0.1f)
                {
                    canEmit = false;
                    break;
                }
            }

            if (canEmit)
            {
               
                ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
                {
                    position = targetPosition,
                    applyShapeToPosition = true,
                    startSize = m_startSize,
                    rotation = m_rotation,
                   rotation3D = m_rotation3D, // Ensure particle faces world up
                    startLifetime = 9999f,
                   velocity = Vector3.up // Make particle face world up
                };
                PSystem.Emit(emitParams, 1);
            }
        }
    }
    
    
    
    
    [ContextMenu("DebugKillAllParticles")]
    public void KillAllParticles()
    {
        if (PSystem == null) return;
        PSystem.Clear();
    }
}
