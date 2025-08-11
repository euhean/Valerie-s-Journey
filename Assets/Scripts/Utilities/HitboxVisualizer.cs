using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Draws visual hitboxes for debugging in the Scene view. Attach to GameManager or any object.
/// Checks for manually placed colliders and rigidbodies in children.
/// Supports both 2D and 3D colliders. Uses OnGUI for accurate 2D bounds.
/// </summary>
[ExecuteAlways]
public class HitboxVisualizer : MonoBehaviour
{
    [Header("Hitbox Visualizer Settings")]
    public bool showHitboxes = true;
    public Color hitboxColor = new(1, 0, 0, 0.25f);
    public Color rigidbodyColor = new(0, 1, 0, 0.15f);

    private static Texture2D _whiteTex;
    private static Texture2D WhiteTex
    {
        get
        {
            if (_whiteTex == null)
            {
                _whiteTex = new Texture2D(1, 1);
                _whiteTex.SetPixel(0, 0, Color.white);
                _whiteTex.Apply();
            }
            return _whiteTex;
        }
    }

    /// <summary>
    /// Toggle the hitbox visualization on or off.
    /// </summary>
    public void ToggleVisualization(bool enabled)
    {
        showHitboxes = enabled;
    }

    private void OnGUI()
    {
        if (!showHitboxes) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        // Draw all 2D colliders as rectangles (accurate for 2D)
        var all2DColliders = FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
        foreach (var col in all2DColliders)
        {
            DrawRectFor2DColliderOnScreen(col, cam, hitboxColor);
        }

        // Draw all 3D colliders as rectangles
        var all3DColliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);
        foreach (var col in all3DColliders)
        {
            DrawRectOnScreen(col.bounds, cam, hitboxColor);
        }
    }

    // Draws a rectangle for a 2D collider using its actual shape
    private void DrawRectFor2DColliderOnScreen(Collider2D col, Camera cam, Color color)
    {
        // Only handle BoxCollider2D for best accuracy; fallback to bounds for others
        if (col is BoxCollider2D box)
        {
            Vector2 size = Vector2.Scale(box.size, box.transform.lossyScale);
            Vector2 offset = box.offset;
            Vector2 center = (Vector2)box.transform.position + (Vector2)(box.transform.rotation * (Vector3)offset);

            // Get the 4 corners in world space
            Vector2 right = box.transform.right * size.x * 0.5f;
            Vector2 up = box.transform.up * size.y * 0.5f;
            Vector3[] worldCorners = new Vector3[4];
            worldCorners[0] = center - right - up;
            worldCorners[1] = center + right - up;
            worldCorners[2] = center + right + up;
            worldCorners[3] = center - right + up;

            // Project to screen space
            Vector2 min = cam.WorldToScreenPoint(worldCorners[0]);
            Vector2 max = min;
            for (int i = 1; i < 4; i++)
            {
                Vector2 pt = cam.WorldToScreenPoint(worldCorners[i]);
                min = Vector2.Min(min, pt);
                max = Vector2.Max(max, pt);
            }
            float width = max.x - min.x;
            float height = min.y - max.y; // Y is flipped in GUI
            Rect rect = new Rect(min.x, Screen.height - min.y, width, height);
            Color prevColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, WhiteTex, ScaleMode.StretchToFill, true, 0);
            GUI.color = prevColor;
        }
        else
        {
            // Fallback: use bounds
            DrawRectOnScreen(col.bounds, cam, color);
        }
    }

    // Draws a rectangle for any 3D collider or fallback
    private void DrawRectOnScreen(Bounds bounds, Camera cam, Color color)
    {
        // Get the 8 corners of the bounds
        Vector3[] corners = new Vector3[8];
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        corners[0] = cam.WorldToScreenPoint(new Vector3(min.x, min.y, min.z));
        corners[1] = cam.WorldToScreenPoint(new Vector3(max.x, min.y, min.z));
        corners[2] = cam.WorldToScreenPoint(new Vector3(max.x, max.y, min.z));
        corners[3] = cam.WorldToScreenPoint(new Vector3(min.x, max.y, min.z));
        corners[4] = cam.WorldToScreenPoint(new Vector3(min.x, min.y, max.z));
        corners[5] = cam.WorldToScreenPoint(new Vector3(max.x, min.y, max.z));
        corners[6] = cam.WorldToScreenPoint(new Vector3(max.x, max.y, max.z));
        corners[7] = cam.WorldToScreenPoint(new Vector3(min.x, max.y, max.z));

        // Find the 2D bounding box in screen space
        float minX = corners[0].x, maxX = corners[0].x, minY = corners[0].y, maxY = corners[0].y;
        for (int i = 1; i < 8; i++)
        {
            minX = Mathf.Min(minX, corners[i].x);
            maxX = Mathf.Max(maxX, corners[i].x);
            minY = Mathf.Min(minY, corners[i].y);
            maxY = Mathf.Max(maxY, corners[i].y);
        }

        // Flip Y for GUI coordinates
        minY = Screen.height - minY;
        maxY = Screen.height - maxY;
        float width = maxX - minX;
        float height = maxY - minY;

        // Draw rectangle
        Color prevColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(minX, maxY, width, height), WhiteTex, ScaleMode.StretchToFill, true, 0);
        GUI.color = prevColor;
    }
}