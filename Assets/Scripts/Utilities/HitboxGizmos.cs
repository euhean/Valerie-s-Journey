using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Lightweight gizmo visualizer for colliders. Editor-friendly and cheap.
/// Attach to any GameObject (e.g. GameManager) and toggle ShowHitboxes in the inspector.
/// Uses cached collider lists and refreshes on enable / validate.
/// Note: Gizmos are editor-only visual aids and won't appear in builds.
/// </summary>
[ExecuteAlways]
public class HitboxGizmos : MonoBehaviour
{
    [Header("Settings")]
    public bool showHitboxes = true;
    public Color wireColor = new Color(1f, 0f, 0f, 0.8f);
    public Color rigidbodyColor = new Color(0f, 1f, 0f, 0.6f);

    // Caches to avoid expensive finds every frame
    private Collider2D[] cached2D;
    private Collider[] cached3D;

    private void OnEnable()
    {
        RefreshCache();
    }

    private void OnValidate()
    {
        // called in editor when values change; refresh caches to reflect scene edits
        RefreshCache();
    }

    /// <summary>
    /// Refresh cached collider lists. Call if you instantiate/destroy colliders at runtime
    /// and want gizmos to immediately reflect the change.
    /// </summary>
    public void RefreshCache()
    {
        cached2D = FindObjectsByType<Collider2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        cached3D = FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    }

    private void OnDrawGizmos()
    {
        if (!showHitboxes) return;

        Gizmos.color = wireColor;

        // Draw 2D colliders
        if (cached2D != null)
        {
            foreach (var col in cached2D)
            {
                if (col == null) continue;
                DrawCollider2DWire(col);
            }
        }
    }

    private void DrawCollider2DWire(Collider2D col)
    {
        // BoxCollider2D -> draw rotated rectangle using transform.right/up
        if (col is BoxCollider2D box)
        {
            Vector2 size = Vector2.Scale(box.size, box.transform.lossyScale);
            Vector2 offset = box.offset;
            Vector2 center = (Vector2)box.transform.position + (Vector2)(box.transform.rotation * (Vector3)offset);

            Vector2 right = 0.5f * size.x * (Vector2)box.transform.right;
            Vector2 up = 0.5f * size.y * (Vector2)box.transform.up;

            Vector3 v0 = center - right - up;
            Vector3 v1 = center + right - up;
            Vector3 v2 = center + right + up;
            Vector3 v3 = center - right + up;

            Gizmos.color = wireColor;
            Gizmos.DrawLine(v0, v1);
            Gizmos.DrawLine(v1, v2);
            Gizmos.DrawLine(v2, v3);
            Gizmos.DrawLine(v3, v0);
        }
        else
        {
            // Fallback: use world-aligned bounds (works for CircleCollider2D, Polygon etc. though less precise)
            var b = col.bounds;
            Gizmos.color = wireColor;
            Gizmos.DrawWireCube(b.center, b.size);
        }
    }
}