using UnityEngine;
using System.Collections;

#region AnimationHelper
/// <summary>
/// AnimationHelper as a scene-attached MonoBehaviour. 
/// NO auto-create. Attach one instance in the scene (recommended on GameManager or a persistent "FX" object).
/// Static wrappers will use the instance if present, or an explicit MonoBehaviour runner passed by the caller.
/// </summary>
public class AnimationHelper : MonoBehaviour
{
    public static AnimationHelper Instance { get; private set; }

    #region Unity lifecycle
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[AnimationHelper] Another instance already exists. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
    #endregion

    #region Static API (convenience wrappers)
    public static void ShowText(Vector3 worldPos, string text, Color color, float duration = GameConstants.TEXT_DURATION)
    {
        GameObject feedbackObj = new GameObject($"Feedback_{text}");
        feedbackObj.transform.position = worldPos + Vector3.up * GameConstants.TEXT_OFFSET_Y;
        var feedback = feedbackObj.AddComponent<InstantTextFeedback>();
        feedback.Initialize(text, color, duration);
    }

    public static void ShowDeath(Vector3 worldPos) => ShowText(worldPos, "DEAD", Color.red);
    public static void ShowAttack(Vector3 worldPos) => ShowText(worldPos, "ATTACK", Color.yellow);
    public static void ShowStrongAttack(Vector3 worldPos) => ShowText(worldPos, "STRONG!", GameConstants.ORANGE_COLOR);

    public static void ShowCombo(Vector3 worldPos, int comboCount)
    {
        string comboText = $"COMBO {comboCount}/{GameConstants.COMBO_STREAK_FOR_STRONG}";
        Color comboColor = Color.Lerp(Color.white, GameConstants.ORANGE_COLOR, comboCount / (float)GameConstants.COMBO_STREAK_FOR_STRONG);
        ShowText(worldPos, comboText, comboColor, GameConstants.COMBO_TEXT_DURATION);
    }

    /// <summary>
    /// Show a color flash on a sprite renderer for duration.
    /// runner: optional MonoBehaviour to start the coroutine on (prefer the entity or GameManager).
    /// If runner is null, will use Instance if present; otherwise attempts to use a MonoBehaviour on the renderer's parent.
    /// If none found, logs and returns.
    /// </summary>
    public static void ShowHitFlash(SpriteRenderer renderer, Color flashColor, float duration, MonoBehaviour runner = null)
    {
        if (renderer == null) return;

        MonoBehaviour actualRunner = ResolveRunner(renderer, runner);
        if (actualRunner == null)
        {
            Debug.LogWarning("[AnimationHelper] No runner available for ShowHitFlash - attach AnimationHelper to scene or pass a runner.");
            return;
        }

        DebugHelper.LogCombat($"[Flash] Starting hit flash on {renderer.gameObject.name}");
        actualRunner.StartCoroutine(HitFlashCoroutine(renderer, flashColor, duration));
    }

    public static void ShowStrongHitShake(Transform target, SpriteRenderer renderer, Color flashColor, float duration, MonoBehaviour runner = null)
    {
        if (target == null || renderer == null) return;

        MonoBehaviour actualRunner = ResolveRunner(renderer, runner);
        if (actualRunner == null)
        {
            Debug.LogWarning("[AnimationHelper] No runner available for ShowStrongHitShake - attach AnimationHelper to scene or pass a runner.");
            return;
        }

        DebugHelper.LogCombat($"[Flash] Starting strong hit flash+shake on {renderer.gameObject.name}");
        actualRunner.StartCoroutine(StrongHitShakeCoroutine(target, renderer, flashColor, duration));
    }
    #endregion

    #region Runner resolution
    // Try to find a MonoBehaviour to run coroutines on (priority: explicit runner -> Instance -> parent component)
    private static MonoBehaviour ResolveRunner(SpriteRenderer renderer, MonoBehaviour explicitRunner)
    {
        if (explicitRunner != null) return explicitRunner;
        if (Instance != null) return Instance;

        if (renderer != null)
        {
            // renderer.GetComponent<MonoBehaviour>() will return null often; check parents instead.
            var parentMono = renderer.GetComponentInParent<MonoBehaviour>();
            if (parentMono != null) return parentMono;
        }

        return null;
    }
    #endregion

    #region Coroutines (static so any runner can start them)
    private static IEnumerator HitFlashCoroutine(SpriteRenderer renderer, Color flashColor, float duration)
    {
        if (renderer == null) yield break;
        Color originalColor = renderer.color;
        renderer.color = flashColor;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (renderer != null) renderer.color = originalColor;
    }

    private static IEnumerator StrongHitShakeCoroutine(Transform target, SpriteRenderer renderer, Color flashColor, float duration)
    {
        if (target == null || renderer == null) yield break;

        Vector3 originalPosition = target.position;
        Color originalColor = renderer.color;

        renderer.color = flashColor;

        float shakeDuration = duration * GameConstants.SHAKE_DURATION_MULTIPLIER;
        float shakeIntensity = GameConstants.SHAKE_INTENSITY;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            Vector3 shakeOffset = new Vector3(
                Random.Range(-shakeIntensity, shakeIntensity),
                Random.Range(-shakeIntensity, shakeIntensity),
                0f
            );
            if (target != null) target.position = originalPosition + shakeOffset;

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (target != null) target.position = originalPosition;
        if (renderer != null) renderer.color = originalColor;
    }
    #endregion

    #region InstantTextFeedback nested class
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
            if (timer >= duration) Destroy(gameObject);
        }

        private void OnGUI()
        {
            if (Camera.main == null) return;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
            if (screenPos.z > 0)
            {
                screenPos.y = Screen.height - screenPos.y;
                float alpha = 1f - (timer / Mathf.Max(0.0001f, duration));
                Color guiColor = textColor;
                guiColor.a = alpha;
                GUI.color = guiColor;
                GUI.Label(new Rect(screenPos.x - 50, screenPos.y - 10, 100, 20), displayText);
                GUI.color = Color.white;
            }
        }
    }
    #endregion
}
#endregion