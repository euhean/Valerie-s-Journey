using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Draws visual hitboxes for debugging in the Scene view. Attach to GameManager or any object.
/// Checks for manually placed colliders and rigidbodies in children.
/// </summary>
[ExecuteAlways]
public class HitboxVisualizer : MonoBehaviour
{
    [Header("Hitbox Visualizer Settings")]
    public bool showHitboxes = true;
    public Color hitboxColor = new Color(1, 0, 0, 0.25f);
    public Color rigidbodyColor = new Color(0, 1, 0, 0.15f);

    void OnDrawGizmos()
    {
        if (!showHitboxes) return;

        // Draw all colliders in children
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (var col in colliders)
        {
            Gizmos.color = hitboxColor;
            DrawColliderGizmo(col);
        }

        // Draw all rigidbodies in children
        Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>(true);
        foreach (var rb in rigidbodies)
        {
            Gizmos.color = rigidbodyColor;
            Gizmos.DrawWireCube(rb.worldCenterOfMass, Vector3.one * 0.2f);
        }
    }

    void DrawColliderGizmo(Collider col)
    {
        if (col is BoxCollider box)
        {
            Gizmos.matrix = box.transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.matrix = Matrix4x4.identity;
        }
        else if (col is SphereCollider sphere)
        {
            Gizmos.matrix = sphere.transform.localToWorldMatrix;
            Gizmos.DrawSphere(sphere.center, sphere.radius);
            Gizmos.matrix = Matrix4x4.identity;
        }
        else if (col is CapsuleCollider capsule)
        {
            Gizmos.matrix = capsule.transform.localToWorldMatrix;
            Gizmos.DrawWireSphere(capsule.center, capsule.radius);
            Gizmos.matrix = Matrix4x4.identity;
        }
        // Add more collider types as needed
    }
}
