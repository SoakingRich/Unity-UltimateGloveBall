using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

public class CircleMovement : MonoBehaviour
{
   
    public GameObject centerObject; // The object around which this object will move.
    public float radius = 2f; // The radius of the circle.
    public float speed = 1f; // The speed at which the object moves.


    public float FalloffDistance; // at what distance does transition start
  // public Vector2 remapCircularSpeed;
    public float StartEndTimeOffset;  // Complete circular motion with this timeoffset on start and end
    public float StartingDistanceFromSurface;  // StartingDistanceFromSurface
    
    [Header("Private")]
    public float alpha;   // accumulated time


  
    
    
    Vector3 CurrentLinearPosition()
    {
      var  startpos =   centerObject.transform.position + centerObject.transform.right * (radius +  StartingDistanceFromSurface);
      var  endpos =   centerObject.transform.position - centerObject.transform.right * (radius +  StartingDistanceFromSurface);
      
      var currentPos = Vector3.Lerp(startpos, endpos, alpha);

      return currentPos;
    }
    
    
    Vector3 CurrentCircularPosition()
    {
     
        var t = math.remap(StartEndTimeOffset, 1.0f - StartEndTimeOffset, 0.0f, 1.0f, alpha);
        t = Mathf.Clamp(t, 0.0f, 1.0f);
        
        var currentAngle = t * Mathf.PI;
        
        float x = centerObject.transform.position.x + Mathf.Cos(currentAngle) * radius;
        float z = centerObject.transform.position.z + Mathf.Sin(currentAngle) * radius;

        return new Vector3(x, transform.position.y, z);
    }

    
    
    Vector3 GetBlendedPosition()
    {
        var clp = CurrentLinearPosition();
        var ccp = CurrentCircularPosition();
        
        
        var dist = Vector3.Distance(clp, centerObject.transform.position);
        
        var distanceFromSurface = dist - radius;

        float blendamount = Mathf.Clamp(distanceFromSurface / FalloffDistance,0.0f,1.0f);   // what percent of falloffdistance is distanceFromSurface,  blendamount must be clamped

        var finalpos = Vector3.Lerp(ccp, clp, blendamount);

        return finalpos;
        
    }
    
    
    
    void Update()
    {
        alpha += Time.deltaTime * speed;
        alpha %= 1.0f;
       transform.position = GetBlendedPosition();
    }


    
    
    
    void InitWithComputeBuffer()
    {
        StructuredBufferNoCompute sbnc = FindObjectOfType<StructuredBufferNoCompute>();
        if(sbnc) sbnc.AddTrackedObject(this.transform);
    }

    void OnDestroy()
    {
        StructuredBufferNoCompute sbnc = FindObjectOfType<StructuredBufferNoCompute>();
        if(sbnc) sbnc.RemoveTrackedObject(this.transform);
    }
}