using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Scene-attached helper for floating combat feedback (text + hit effects).
/// Maintains a pool of TextMeshPro instances so spawning feedback doesn't allocate.
/// Attach one instance in the scene (recommended on a persistent "FX" object).
/// </summary>
public class AnimationHelper : MonoBehaviour
{
    public static AnimationHelper Instance { get; private set; }

    #region Inspector
    [Header("Feedback Text Pool")]
    [Tooltip("Required. Assign a TMP font asset (e.g. LiberationSans SDF).")]
    public TMP_FontAsset fontAsset;
    [Tooltip("Initial pool size. Pool grows on demand if exceeded.")]
    public int initialPoolSize = 16;
    [Tooltip("Font size in world units. Tune for your camera ortho size.")]
    public float worldFontSize = 4f;
    [Tooltip("2D sorting layer. Should match your sprites' layer or render above them.")]
    public string sortingLayer = "Default";
    public int sortingOrder = 1000;
    #endregion

    private readonly Queue<FeedbackText> _pool = new();
    private Transform _poolRoot;

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[AnimationHelper] Another instance exists; destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (fontAsset == null)
            DebugHelper.LogError("[AnimationHelper] fontAsset is not assigned — feedback text will not render.");

        _poolRoot = new GameObject("FeedbackTextPool").transform;
        _poolRoot.SetParent(transform, false);

        for (int i = 0; i < initialPoolSize; i++)
            _pool.Enqueue(CreatePoolItem());
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
    #endregion

    #region Pool Management
    private FeedbackText CreatePoolItem()
    {
        var go = new GameObject("FeedbackText");
        go.transform.SetParent(_poolRoot, false);
        go.SetActive(false);

        var tmp = go.AddComponent<TextMeshPro>();
        if (fontAsset != null) tmp.font = fontAsset;
        tmp.fontSize = worldFontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.sortingLayerID = SortingLayer.NameToID(sortingLayer);
        tmp.sortingOrder = sortingOrder;

        var item = go.AddComponent<FeedbackText>();
        item.BindOwner(this, tmp);
        return item;
    }

    private FeedbackText Acquire()
    {
        // Reuse from pool, or grow if empty.
        return _pool.Count > 0 ? _pool.Dequeue() : CreatePoolItem();
    }

    internal void Release(FeedbackText item)
    {
        item.gameObject.SetActive(false);
        _pool.Enqueue(item);
    }
    #endregion

    #region Static API (unchanged callers)
    public static void ShowText(Vector3 worldPos, string text, Color color, float duration = GameConstants.TEXT_DURATION)
    {
        if (Instance == null)
        {
            DebugHelper.LogWarning("[AnimationHelper] No Instance — attach AnimationHelper to a scene object.");
            return;
        }
        var item = Instance.Acquire();
        item.Show(worldPos, text, color, duration);
    }

    public static void ShowDeath(Vector3 worldPos)        => ShowText(worldPos, "DEAD", Color.red);
    public static void ShowAttack(Vector3 worldPos)       => ShowText(worldPos, "ATTACK", Color.yellow);
    public static void ShowStrongAttack(Vector3 worldPos) => ShowText(worldPos, "STRONG!", GameConstants.ORANGE_COLOR);

    public static void ShowCombo(Vector3 worldPos, int comboCount)
    {
        string comboText = $"COMBO {comboCount}/{GameConstants.COMBO_STREAK_FOR_STRONG}";
        Color comboColor = Color.Lerp(Color.white, GameConstants.ORANGE_COLOR, comboCount / (float)GameConstants.COMBO_STREAK_FOR_STRONG);
        ShowText(worldPos, comboText, comboColor, GameConstants.COMBO_TEXT_DURATION);
    }

    // Hit flash + shake are unchanged — they modify existing renderers, no allocation.
    public static void ShowHitFlash(SpriteRenderer renderer, Color flashColor, float duration, MonoBehaviour runner = null)
    {
        if (renderer == null) return;
        MonoBehaviour actualRunner = ResolveRunner(renderer, runner);
        if (actualRunner == null)
        {
            DebugHelper.LogWarning("[AnimationHelper] No runner for ShowHitFlash.");
            return;
        }
        actualRunner.StartCoroutine(HitFlashCoroutine(renderer, flashColor, duration));
    }

    public static void ShowStrongHitShake(Transform target, SpriteRenderer renderer, Color flashColor, float duration, MonoBehaviour runner = null)
    {
        if (target == null || renderer == null) return;
        MonoBehaviour actualRunner = ResolveRunner(renderer, runner);
        if (actualRunner == null)
        {
            DebugHelper.LogWarning("[AnimationHelper] No runner for ShowStrongHitShake.");
            return;
        }
        actualRunner.StartCoroutine(StrongHitShakeCoroutine(target, renderer, flashColor, duration));
    }
    #endregion

    #region Runner resolution
    private static MonoBehaviour ResolveRunner(SpriteRenderer renderer, MonoBehaviour explicitRunner)
    {
        if (explicitRunner != null) return explicitRunner;
        if (Instance != null) return Instance;
        if (renderer != null)
        {
            var parentMono = renderer.GetComponentInParent<MonoBehaviour>();
            if (parentMono != null) return parentMono;
        }
        return null;
    }
    #endregion

    #region Hit Flash Coroutines
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

    #region Nested: FeedbackText component
    /// <summary>
    /// Pooled, world-space TMP text that fades out and returns to its owner pool.
    /// </summary>
    public class FeedbackText : MonoBehaviour
    {
        private AnimationHelper owner;
        private TextMeshPro tmp;
        private float timer;
        private float duration;
        private Color baseColor;

        public void BindOwner(AnimationHelper owner, TextMeshPro tmp)
        {
            this.owner = owner;
            this.tmp = tmp;
        }

        public void Show(Vector3 worldPos, string text, Color color, float displayDuration)
        {
            transform.position = worldPos + Vector3.up * GameConstants.TEXT_OFFSET_Y;
            tmp.text = text;
            baseColor = color;
            tmp.color = color;
            duration = displayDuration;
            timer = 0f;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer >= duration)
            {
                owner.Release(this);
                return;
            }

            float alpha = 1f - (timer / Mathf.Max(0.0001f, duration));
            var c = baseColor;
            c.a = alpha;
            tmp.color = c;
        }
    }
    #endregion
}
