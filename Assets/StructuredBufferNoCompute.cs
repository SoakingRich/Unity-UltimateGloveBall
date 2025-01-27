using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class StructuredBufferNoCompute : MonoBehaviour
{
    // The same struct in Shader
    struct MyObjectStruct
    {
        public Vector3 objPosition;
    };

    private ComputeBuffer _computeBuffer;
    private List<Transform> _objectsToTrack = new List<Transform>();
    private List<MyObjectStruct> _allMyObjectStructs = new List<MyObjectStruct>();
    private int _maxObjects;

    public List<GameObject> ListOfObjectsToRetrieveMatFrom;  // List of GameObjects to search for renderers

    [Header("Internal")]
    private Material Mat;


    private void Start()
    {
        InitiateBuffer();
    }

    private void InitiateBuffer()
    {
        _computeBuffer?.Release(); // Release old buffer if it exists

        _maxObjects = _objectsToTrack.Count;
        if (_maxObjects <= 0) return;
        
        _allMyObjectStructs = new List<MyObjectStruct>(_maxObjects);

        // Initialize buffer
        _computeBuffer = new ComputeBuffer(_maxObjects, sizeof(float) * 3); // 3 floats for Vector3
    }

    public void AddTrackedObject(Transform newObj)
    {
        if (!_objectsToTrack.Contains(newObj))
        {
            _objectsToTrack.Add(newObj);
            Debug.Log($"Added new tracked object: {newObj.name}");
            InitiateBuffer(); // Reinitialize buffer with the updated list
        }
    }

    public void RemoveTrackedObject(Transform obj)
    {
        if (_objectsToTrack.Contains(obj))
        {
            _objectsToTrack.Remove(obj);
            Debug.Log($"Removed tracked object: {obj.name}");
            InitiateBuffer(); // Reinitialize buffer with the updated list
        }
    }

    private void Update()
    {
        if (Mat == null) InitiateBuffer();
        
        SendDataToMat();
    }

//[ContextMenuItem("SendDataToMat")]
    private void SendDataToMat()
    {
        if (_objectsToTrack.Count == 0 || ListOfObjectsToRetrieveMatFrom.Count == 0) return;

        // Update positions in the struct list
        _allMyObjectStructs.Clear();
        foreach (var obj in _objectsToTrack)
        {
            _allMyObjectStructs.Add(new MyObjectStruct { objPosition = obj.position });
        }

        var array = _allMyObjectStructs.ToArray();
        // Set data to the compute buffer
        _computeBuffer.SetData(array);

        // Loop through the list of GameObjects and find renderers
        foreach (var gameObject in ListOfObjectsToRetrieveMatFrom)
        {
            if (gameObject != null)
            {
                // Check for MeshRenderer
                MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    foreach (var material in meshRenderer.sharedMaterials)
                    {
                        material.SetBuffer("_objectStructs", _computeBuffer);
                    }
                }

                // Check for ParticleSystemRenderer
                ParticleSystemRenderer particleRenderer = gameObject.GetComponent<ParticleSystemRenderer>();
                if (particleRenderer != null)
                {
                    foreach (var material in particleRenderer.sharedMaterials)
                    {
                        material.SetBuffer("_objectStructs", _computeBuffer);
                    }
                }
            }
        }
    }

    private void OnDestroy()
    {
        // Clean up buffer on destruction
        _computeBuffer?.Release();
    }
}
