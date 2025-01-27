using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Meta.Utilities.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

public class MouseControl : MonoBehaviour
{
    [SerializeField] private InputAction mousePositionAction;

    // Start is called before the first frame update
    void Start()
    {

    }

    void Update()
    {
        Camera mainCamera = Camera.main;

        // Vector2 mouseScreenPosition = mousePositionAction.ReadValue<Vector2>();
        // float depth = 10f;
        // Vector3 mouseWorldPosition =
        //     mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, depth));
        //
        // Ray mouseRay = mainCamera.ScreenPointToRay(mouseScreenPosition);
        //
        // Debug.Log($"Mouse World Position (at depth {depth}): {mouseWorldPosition}");
        // Debug.Log($"Mouse Ray Origin: {mouseRay.origin}, Direction: {mouseRay.direction}");


        var XRSim = FindObjectOfType<XRDeviceFpsSimulator>();
        var pinchEvents = FindObjectsOfType<TriggerPinchEvents>();
        var rightPinchEvents = pinchEvents.Where(eventData => eventData.IsRight).ToArray();
        Vector3 rightHandPosition = rightPinchEvents[0].transform.position;

        Collider[] hitColliders = Physics.OverlapSphere(rightHandPosition, 1.0f);

        GameObject nearestSnapzone = null;
        float closestDistance = Mathf.Infinity;

        foreach (var hitCollider in hitColliders)
        {
            SnapZone snapzone = hitCollider.GetComponent<SnapZone>();
            if (snapzone != null)
            {
                float distance = Vector3.Distance(rightHandPosition, snapzone.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    nearestSnapzone = snapzone.gameObject;
                }
            }
        }

        if (nearestSnapzone != null)
        {
            Vector3 delta = nearestSnapzone.transform.position - rightHandPosition;

            Vector3 localDelta = mainCamera.transform.InverseTransformPoint(delta);

        //    XRSim.savedHandMovementZ = localDelta.z;

            // Debug.Log($"Nearest Snapzone: {nearestSnapzone.name}");
            // Debug.Log($"Updated Right Controller Offset: {XRSim.m_rightControllerOffset}");


        }

    }
}
