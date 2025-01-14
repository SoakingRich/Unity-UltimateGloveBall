using UnityEngine;

public class ClickableGizmo : MonoBehaviour
{
    [SerializeField] private float radius;
    [SerializeField] private Vector3 cubeSize = new Vector3(0f, 0f, 0f); // Cube size


    // This method draws the gizmo in the Scene view
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red; // Set the color of the gizmo

        if (radius!=0.0f)
        {
            Gizmos.DrawWireSphere(transform.position, radius); // Draw a wireframe sphere around the object
        }

        if (cubeSize != Vector3.zero)
        {
            Gizmos.DrawWireCube(transform.position, cubeSize); // Draw a wireframe sphere around the object
        }
    }
}