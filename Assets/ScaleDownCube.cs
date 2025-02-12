using UnityEngine;
using DG.Tweening;

public class ScaleDownCube : MonoBehaviour
{
    public Transform cube;
    public float scaleFactor = 0.5f; // Scale factor (e.g., 0.5 for 50%)
    public float duration = 1f; // Duration of the tween

    private Vector3 originalScale;
    private Vector3 originalPosition;

    void Start()
    {
        if (cube == null)
        {
            Debug.LogError("Cube Transform is not assigned!");
            return;
        }

        originalScale = cube.localScale;
        originalPosition = cube.position;

        ScaleDown();
    }

    void ScaleDown()
    {
        // Calculate the new scale for the Y axis (e.g., 50% of the original scale)
        float targetScaleY = originalScale.y * scaleFactor;

        // Calculate the height difference between the original and target scale
        float heightDifference = originalScale.y - targetScaleY;

        // Apply the new scale to the cube, only changing the Y axis
        Vector3 targetScale = new Vector3(originalScale.x, targetScaleY, originalScale.z);

        // Move the cube downwards by the height difference to keep the bottom in place
        Vector3 targetPosition = new Vector3(originalPosition.x, originalPosition.y - heightDifference, originalPosition.z);

        // Animate the scale and position using DOTween
        cube.DOScale(targetScale, duration);
        cube.DOMove(targetPosition, duration);
    }
}