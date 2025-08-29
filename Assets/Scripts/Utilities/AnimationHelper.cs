using UnityEngine;
using System.Collections;

/// <summary>
/// AnimationHelper as a MonoBehaviour singleton so static calls can run coroutines.
/// Attach one instance to the scene (recommended on a persistent object like GameManager),
/// or the instance will auto-create itself if missing.
/// </summary>
public class AnimationHelper : MonoBehaviour
{
    static AnimationHelper _instance;
    public static AnimationHelper Instance {
        get {
            if (_instance == null) {
                _instance = FindObjectOfType<AnimationHelper>();
                if (_instance == null) {
                    var go = new GameObject("AnimationHelper");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<AnimationHelper>();
                }
            }
            return _instance;
        }
    }

    // ---------- Static wrappers (keep existing call sites intact) ----------
    public static void ShowText(Vector3 worldPos, string text, Color color, float duration = GameConstants.TEXT_DURATION) {
        Instance.ShowTextInternal(worldPos, text, color, duration);
    }

    public static void ShowDeath(Vector3 worldPos) => ShowText(worldPos, "DEAD", Color.red);
    public static void ShowAttack(Vector3 worldPos) => ShowText(worldPos, "ATTACK", Color.yellow);
    public static void ShowStrongAttack(Vector3 worldPos) => ShowText(worldPos, "STRONG!", GameConstants.ORANGE_COLOR);
    public static void ShowCombo(Vector3 worldPos, int comboCount) {
        string comboText = $"COMBO {comboCount}/{GameConstants.COMBO_STREAK_FOR_STRONG}";
        Color comboColor = Color.Lerp(Color.white, GameConstants.ORANGE_COLOR, comboCount / (float)GameConstants.COMBO_STREAK_FOR_STRONG);
        ShowText(worldPos, comboText, comboColor, GameConstants.COMBO_TEXT_DURATION);
    }

    public static void ShowHitFlash(SpriteRenderer renderer, Color flashColor, float duration) {
        if (renderer == null) return;
        Instance.RunCoroutine(Instance.HitFlashCoroutine(renderer, flashColor, duration));
    }

    public static void ShowStrongHitShake(Transform target, SpriteRenderer renderer, Color flashColor, float duration) {
        if (target == null || renderer == null) return;
        Instance.RunCoroutine(Instance.StrongHitShakeCoroutine(target, renderer, flashColor, duration));
    }

    // ---------- Instance implementation ----------
    void Awake() {
        if (_instance != null && _instance != this) {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Helper to start coroutines from instance
    Coroutine RunCoroutine(IEnumerator r) => StartCoroutine(r);

    // ----------------- Implementation details -----------------
    private void ShowTextInternal(Vector3 worldPos, string text, Color color, float duration) {
        GameObject feedbackObj = new GameObject($"Feedback_{text}");
        feedbackObj.transform.position = worldPos + Vector3.up * GameConstants.TEXT_OFFSET_Y;
        var feedback = feedbackObj.AddComponent<InstantTextFeedback>();
        feedback.Initialize(text, color, duration);
    }

    // ---- Coroutines for hit/strong effects ----
    private IEnumerator HitFlashCoroutine(SpriteRenderer renderer, Color flashColor, float duration) {
        Color originalColor = renderer.color;
        renderer.color = flashColor;
        yield return new WaitForSeconds(duration);
        if (renderer != null) renderer.color = originalColor;
    }

    private IEnumerator StrongHitShakeCoroutine(Transform target, SpriteRenderer renderer, Color flashColor, float duration) {
        Vector3 originalPosition = target.position;
        Color originalColor = renderer.color;

        // Flash color for strong attacks
        renderer.color = flashColor;

        // Shake parameters
        float shakeDuration = duration * GameConstants.SHAKE_DURATION_MULTIPLIER; // Longer than normal flash
        float shakeIntensity = GameConstants.SHAKE_INTENSITY;
        float elapsed = 0f;

        while (elapsed < shakeDuration) {
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
        if (target != null) target.position = originalPosition;
        if (renderer != null) renderer.color = originalColor;
    }

    // ----------------- InstantTextFeedback component -----------------
    public class InstantTextFeedback : MonoBehaviour {
        private string displayText;
        private Color textColor;
        private float timer;
        private float duration;

        public void Initialize(string text, Color color, float displayDuration) {
            displayText = text;
            textColor = color;
            duration = displayDuration;
            timer = 0f;
        }

        private void Update() {
            timer += Time.deltaTime;
            if (timer >= duration) Destroy(gameObject);
        }

        private void OnGUI() {
            if (Camera.main == null) return;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
            if (screenPos.z > 0) {
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
}