using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BeatVisualizer : MonoBehaviour
{
    [Header("UI References")]
    public Image[] bars; 
    public Color beatColor = Color.yellow;   
    public Color baseColor = Color.white;    
    public Color onBeatColor = Color.green;  
    public Color offBeatColor = Color.red;
    public float flashDuration = 0.3f;

    private TimeManager timeManager;

    private int currentBar = 0;
    private bool inputProcessedThisFrame = false;
    private Coroutine activeCoroutine;

    private void Start()
    {
        if (timeManager == null) timeManager = FindFirstObjectByType<timeManager>();

        foreach (var bar in bars)
            bar.color = baseColor;
    }

    private void OnEnable()
    {
        EventBus.Instance.Subscribe<BeatTickEvent>(HandleBeat);
        EventBus.Instance.Subscribe<BasicPressedEvent>(HandleInput);
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<BeatTickEvent>(HandleBeat);
        EventBus.Instance.Unsubscribe<BasicPressedEvent>(HandleInput);
    }

    /// <summary>
    /// Se ejecuta cada vez que hay un beat en la música
    /// </summary>
    private void HandleBeat(BeatTickEvent_)
    {
        if (bars.Length == 0) return;

        if (!inputProcessedThisFrame)
        {
            FlashBar(bars[currentBar], offBeatColor);
            DebugHelper.LogCombat(() => $"BeatVisualizer: Miss on bar[{currentBar}]");
        }

        currentBar = (currentBar + 1) % bars.Length;
        FlashBar(bars[currentBar], beatColor);

        inputProcessedThisFrame = false;
    }

    /// <summary>
    /// Se ejecuta cuando el jugador presiona el botón Basic Action
    /// Solo procesa UN input por beat
    /// </summary>
    private void HandleInput(double dspTime)
    {
        if (timeManager == null || bars.Length == 0) return;
        if (inputProcessedThisFrame) return;

        bool onBeat = timeManager.IsOnBeat(dspTime);
        Color colorToFlash = onBeat ? onBeatColor : offBeatColor;
        FlashBar(bars[currentBar], colorToFlash);

        inputProcessedThisFrame = true;

        DebugHelper.LogCombat(() => $"BeatVisualizer: {(onBeat ? "HIT" : "MISS")} at DSP {e.dspTime:F4}");
    }

    /// <summary>
    /// Ejecuta un flash de color en una barra (detiene cualquier corrutina anterior)
    /// </summary>
    private void FlashBar(Image bar, Color flashColor)
    {
        if (activeCoroutine != null) StopCoroutine(activeCoroutine);
        foreach (var b in bars) b.color = baseColor;
        activeCoroutine = StartCoroutine(FlashCoroutine(bar, flashColor));
    }

    /// <summary>
    /// Corrutina que anima el flash de color
    /// </summary>
    private IEnumerator FlashCoroutine(Image bar, Color flashColor)
    {
        bar.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        bar.color = baseColor;
    }
}
