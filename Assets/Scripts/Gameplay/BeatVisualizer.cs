using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Beat HUD: one bar lights up per metronome beat, and the current bar turns
/// green/red when an attack starts on/off beat. It shows the combat system's own
/// judgement (AttackStartedEvent.onBeat) instead of re-judging the input itself.
/// </summary>
public class BeatVisualizer : MonoBehaviour
{
    [Header("UI References")]
    public Image[] bars;
    public Color beatColor = Color.yellow;   // bar lit by the beat itself
    public Color baseColor = Color.white;    // idle bar
    public Color onBeatColor = Color.green;  // attack landed on the beat
    public Color offBeatColor = Color.red;   // attack was off the beat
    public float flashDuration = 0.3f;

    private int currentBar = 0;
    private bool attackShownThisBeat = false;
    private Coroutine activeFlash;

    private void Start()
    {
        ResetBars();
    }

    private void OnEnable()
    {
        EventBus.Instance.Subscribe<BeatTickEvent>(HandleBeat);
        EventBus.Instance.Subscribe<AttackStartedEvent>(HandleAttackStarted);
        EventBus.Instance.Subscribe<PlayerDiedEvent>(HandlePlayerDied);
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<BeatTickEvent>(HandleBeat);
        EventBus.Instance.Unsubscribe<AttackStartedEvent>(HandleAttackStarted);
        EventBus.Instance.Unsubscribe<PlayerDiedEvent>(HandlePlayerDied);
    }

    private void HandleBeat(BeatTickEvent _)
    {
        if (bars.Length == 0) return;

        currentBar = (currentBar + 1) % bars.Length;
        FlashBar(bars[currentBar], beatColor);
        attackShownThisBeat = false;
    }

    // Only the first attack of a beat is shown.
    private void HandleAttackStarted(AttackStartedEvent e)
    {
        if (bars.Length == 0 || attackShownThisBeat) return;

        FlashBar(bars[currentBar], e.onBeat ? onBeatColor : offBeatColor);
        attackShownThisBeat = true;
    }

    private void HandlePlayerDied(PlayerDiedEvent _)
    {
        if (activeFlash != null)
        {
            StopCoroutine(activeFlash);
            activeFlash = null;
        }
        ResetBars();
    }

    private void FlashBar(Image bar, Color flashColor)
    {
        if (activeFlash != null) StopCoroutine(activeFlash);
        ResetBars();
        activeFlash = StartCoroutine(FlashCoroutine(bar, flashColor));
    }

    private IEnumerator FlashCoroutine(Image bar, Color flashColor)
    {
        bar.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        bar.color = baseColor;
        activeFlash = null;
    }

    private void ResetBars()
    {
        foreach (var bar in bars)
            bar.color = baseColor;
    }
}
