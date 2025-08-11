using UnityEngine;
using System.Collections;

/// <summary>
/// Animation helper for placeholder feedback during alpha testing.
/// Provides quick methods for text and hit effects.
/// </summary>
public static class AnimationHelper
{
    /// <summary>
    /// Shows instant text feedback above a world position for alpha testing.
    /// </summary>
    public static void ShowText(Vector3 worldPos, string text, Color color, float duration = GameConstants.TEXT_DURATION)
    {
        // Create temporary GameObject for the feedback
        GameObject feedbackObj = new GameObject($"Feedback_{text}");
        feedbackObj.transform.position = worldPos + Vector3.up * GameConstants.TEXT_OFFSET_Y;

        var feedback = feedbackObj.AddComponent<InstantTextFeedback>();
        feedback.Initialize(text, color, duration);
    }

    // Quick helper methods for common feedback
    public static void ShowDeath(Vector3 worldPos) => ShowText(worldPos, "DEAD", Color.red);
    public static void ShowAttack(Vector3 worldPos) => ShowText(worldPos, "ATTACK", Color.yellow);
    public static void ShowStrongAttack(Vector3 worldPos) => ShowText(worldPos, "STRONG!", GameConstants.ORANGE_COLOR);

    // Combo display for Vincent's on-beat attacks
    public static void ShowCombo(Vector3 worldPos, int comboCount)
    {
        string comboText = $"COMBO {comboCount}/{GameConstants.COMBO_STREAK_FOR_STRONG}";
        Color comboColor = Color.Lerp(Color.white, GameConstants.ORANGE_COLOR, comboCount / (float)GameConstants.COMBO_STREAK_FOR_STRONG);
        ShowText(worldPos, comboText, comboColor, GameConstants.COMBO_TEXT_DURATION);
    }

    // Hit effect methods for entities
    public static void ShowHitFlash(SpriteRenderer renderer, Color flashColor, float duration)
    {
        if (renderer == null) return;
        // Find a MonoBehaviour context to run the coroutine on
        MonoBehaviour context = renderer.GetComponentInParent<MonoBehaviour>();
        if (context != null)
        {
            context.StartCoroutine(HitFlashCoroutine(renderer, flashColor, duration));
        }
    }

    public static void ShowStrongHitShake(Transform target, SpriteRenderer renderer, Color flashColor, float duration)
    {
        if (target == null || renderer == null) return;
        MonoBehaviour context = target.GetComponentInParent<MonoBehaviour>();
        if (context != null)
        {
            context.StartCoroutine(StrongHitShakeCoroutine(target, renderer, flashColor, duration));
        }
    }

    // Coroutines for hit effects
    private static IEnumerator HitFlashCoroutine(SpriteRenderer renderer, Color flashColor, float duration)
    {
        Color originalColor = renderer.color;
        renderer.color = flashColor;
        yield return new WaitForSeconds(duration);
        renderer.color = originalColor;
    }

    private static IEnumerator StrongHitShakeCoroutine(Transform target, SpriteRenderer renderer, Color flashColor, float duration)
    {
        Vector3 originalPosition = target.position;
        Color originalColor = renderer.color;
        // Flash color for strong attacks
        renderer.color = flashColor;
        // Shake parameters
        float shakeDuration = duration * GameConstants.SHAKE_DURATION_MULTIPLIER; // Longer than normal flash
        float shakeIntensity = GameConstants.SHAKE_INTENSITY;
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            // Random shake offset
            Vector3 shakeOffset = new Vector3(
                Random.Range(-shakeIntensity, shakeIntensity),
                Random.Range(-shakeIntensity, shakeIntensity),
                0f
            );
            target.position = originalPosition + shakeOffset;
            elapsed += Time.deltaTime;
            yield return null;
        }
        // Reset position and color
        target.position = originalPosition;
        renderer.color = originalColor;
    }
}

/// <summary>
/// Simple component that shows 3D text and self-destructs. Alpha strategy!
/// </summary>
public class InstantTextFeedback : MonoBehaviour
{
    private string displayText;
    private Color textColor;
    private float timer;
    private float duration;
    public void Initialize(string text, Color color, float displayDuration)
    {
        displayText = text;
        textColor = color;
        duration = displayDuration;
        timer = 0f;
    }
    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= duration)
        {
            Destroy(gameObject);
        }
    }
    private void OnGUI()
    {
        if (Camera.main == null) return;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        if (screenPos.z > 0)
        {
            // Convert Unity screen coordinates to GUI coordinates
            screenPos.y = Screen.height - screenPos.y;
            // Simple alpha fade
            float alpha = 1f - (timer / duration);
            Color guiColor = textColor;
            guiColor.a = alpha;
            GUI.color = guiColor;
            GUI.Label(new Rect(screenPos.x - 50, screenPos.y - 10, 100, 20), displayText);
            GUI.color = Color.white; // Reset
        }
    }
}