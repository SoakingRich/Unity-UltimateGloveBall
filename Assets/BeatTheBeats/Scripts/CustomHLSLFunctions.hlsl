
//The same struct in Shader
    struct myObjectStruct
    {
        float3 objPosition;
    };

StructuredBuffer<myObjectStruct> _objectStructs;         // the instance name of this var is important, and should be reused in C# using SetBuffer


// float rand(float2 Seed)
// {
//     return frac(sin(dot(Seed,float2(12.9898, 78.233)))*43758.5453);
// }



float CalculateParticleInfluence(float3 pos, float3 otherpos, float innerDist, float outerDist)
{
    if (length(otherpos) == 0.0f)  // Check if otherpos is the zero vector
    {
        return 0.0f;
    }
    
    float distance = length(pos - otherpos);  // Compute Euclidean distance between particle center and bullet

    // Smooth transition from 0 to 1 between outerDist and innerDist
    return smoothstep(outerDist, innerDist, distance);
}

// ACTUAL FUNCTION TO BE EXPOSED IN SHADER GRAPH
void CalculateCombinedInfluence_float(float3 particleCenter, float MaxPositions, float innerRange, float outerRange, out float OutInfluence)
{
    float influence = 0.0;

    for (int i = 0; i < MaxPositions; i++)
    {
        influence += CalculateParticleInfluence(particleCenter, _objectStructs[i].objPosition, innerRange, outerRange);
    }


    OutInfluence = saturate(influence);
}










float CalculateParticleInfluenceIgnoreY(float3 pos, float3 otherpos, float innerDist, float outerDist)
{
    pos.y = 0;
    otherpos.y = 0;

    if (length(otherpos) == 0.0f)  // Check if otherpos is the zero vector
    {
        return 0.0f;
    }
    
    float distance = length(pos - otherpos);  // Compute Euclidean distance between particle center and bullet

    // Smooth transition from 0 to 1 between outerDist and innerDist
    return smoothstep(outerDist, innerDist, distance);
}

// ACTUAL FUNCTION TO BE EXPOSED IN SHADER GRAPH
void CalculateCombinedInfluenceIgnoreY_float(float3 particleCenter, float MaxPositions, float innerRange, float outerRange, out float OutInfluence)
{
    float influence = 0.0;

    for (int i = 0; i < MaxPositions; i++)
    {
        influence += CalculateParticleInfluenceIgnoreY(particleCenter, _objectStructs[i].objPosition, innerRange, outerRange);
    }

    // Clamp influence to 1.0 (any object influence will mask the particle)
    OutInfluence = saturate(influence);
}
