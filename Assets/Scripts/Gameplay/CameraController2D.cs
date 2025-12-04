using UnityEngine;

/// <summary>
/// Smoothly follows a target in 2D. Attach this to your Main Camera and
/// assign the player transform to the target field in the Inspector.
/// </summary>
public class CameraController2D : MonoBehaviour
{
    [Header("Follow target and offset")]
    public Transform target;
    public Vector3 offset = new(0, 0, GameConstants.CAMERA_Z_OFFSET);

    [Header("Smoothing")]
    public float smoothTime = GameConstants.CAMERA_SMOOTH_TIME;
    private Vector3 velocity = Vector3.zero;

    private void Start()
    {
        // Safety: If Z offset is 0, the camera will clip into sprites (Z=0) and render nothing.
        // Force a safe default if the inspector value is bad.
        if (Mathf.Abs(offset.z) < 0.5f)
        {
            Debug.LogWarning($"[CameraController2D] Z-offset is too close to 0 ({offset.z}). Sprites will be clipped! Resetting to -10.");
            offset.z = -10f;
        }
    }

    private void LateUpdate()
    {
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