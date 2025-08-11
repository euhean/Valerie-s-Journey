using UnityEngine;

/// <summary>
/// Smoothly follows a target in 2D. Attach this to your Main Camera and
/// assign the player transform to the target field in the Inspector.
/// </summary>
public class CameraController2D : MonoBehaviour {
    [Header("Follow target and offset")]
    public Transform target;             
    public Vector3 offset = new(0, 0, -10f);

    [Header("Smoothing")]
    public float smoothTime = 0.3f;     

    private Vector3 velocity = Vector3.zero;

    private void LateUpdate() {
        if (target == null) return;

        // Desired position = target position + fixed offset
        Vector3 desiredPosition = target.position + offset;

        // Smoothly move camera toward desired position
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref velocity,
            smoothTime
        );
    }
}