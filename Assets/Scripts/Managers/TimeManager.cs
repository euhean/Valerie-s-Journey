using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// TimeManager / metronome: emits OnBeat events using DSP timing and consults BeatConfig.
/// Designed to be lifecycle-managed by GameManager (Configure/Initialize/BindEvents/StartRuntime/StopRuntime).
/// </summary>
public class TimeManager : BaseManager
{
    [Header("Tempo")]
    [Tooltip("Beats Per Minute. Can be tuned in editor for testing.")]
    public float bpm = 120f;

    [Header("Beat Config (assigned by GameManager or in Inspector)")]
    public BeatConfig beatConfig; // assigned by GameManager.AutoConfigureScene when available

    /// <summary>Raised on each beat; payload = beat index (starting at 0).</summary>
    public event Action<int> OnBeat;

    // Internal timing state
    private bool running = false;
    private Coroutine metronomeCoroutine;
    private double nextBeatDSP = 0.0;
    private int beatIndex = 0;
    private double lastBeatDSP = -9999.0;

    // Small guard to prevent spamming StartRuntime
    private bool runtimeStarted = false;

    #region BaseManager surface (minimal)
    public override void Configure(GameManager gm)
    {
        base.Configure(gm);
        // BeatConfig might already be assigned by GameManager.AutoConfigureScene
        if (beatConfig == null)
        {
            DebugHelper.LogManager("[TimeManager] No BeatConfig assigned (using defaults).");
        }
    }

    public override void Initialize()
    {
        base.Initialize();
        // nothing special for now
    }

    public override void BindEvents()
    {
        base.BindEvents();
        // no external subscriptions required here
    }

    public override void StartRuntime()
    {
        if (runtimeStarted) return;
        runtimeStarted = true;
        // Start the metronome loop using TimeManager's own coroutine capability
        metronomeCoroutine = StartCoroutine(MetronomeLoop());
        DebugHelper.LogManager("[TimeManager] Runtime started.");
    }

    public override void StopRuntime()
    {
        if (!runtimeStarted) return;
        runtimeStarted = false;
        if (metronomeCoroutine != null)
        {
            StopCoroutine(metronomeCoroutine);
            metronomeCoroutine = null;
        }
        running = false;
        DebugHelper.LogManager("[TimeManager] Runtime stopped.");
    }

    public override void UnbindEvents()
    {
        base.UnbindEvents();
        // nothing to unbind
    }

    private void OnDisable()
    {
        // Safety cleanup: stop runtime if active
        StopRuntime();
    }
    #endregion

    #region Metronome loop
    private IEnumerator MetronomeLoop()
    {
        running = true;
        beatIndex = 0;
        lastBeatDSP = AudioSettings.dspTime;
        double dspNow = AudioSettings.dspTime;
        double secondsPerBeat = 60.0 / Math.Max(1f, bpm);
        nextBeatDSP = dspNow + secondsPerBeat * 0.1; // small offset to avoid immediately firing

        while (running)
        {
            dspNow = AudioSettings.dspTime;

            // Catch-up loop (in case of hiccups)
            while (dspNow + 0.0001 >= nextBeatDSP)
            {
                // Fire beat
                lastBeatDSP = nextBeatDSP;
                try
                {
                    OnBeat?.Invoke(beatIndex);
                }
                catch (Exception ex)
                {
                    DebugHelper.LogWarning($"[TimeManager] OnBeat handler threw: {ex.Message}");
                }

                beatIndex++;
                // schedule next
                secondsPerBeat = 60.0 / Math.Max(1f, bpm);
                nextBeatDSP += secondsPerBeat;
                dspNow = AudioSettings.dspTime;
            }

            yield return null;
        }
    }
    #endregion

    #region Query helpers (for gameplay)
    /// <summary>
    /// Returns true if the supplied DSP timestamp is within the configured on-beat window.
    /// beatConfig.onBeatWindowSec represents the half-window (±).
    /// </summary>
    public bool IsOnBeat(double dspTime)
    {
        if (lastBeatDSP < 0) return false;
        float tol = beatConfig != null ? Mathf.Abs(beatConfig.onBeatWindowSec) : 0.07f;
        return Math.Abs(dspTime - lastBeatDSP) <= tol;
    }

    /// <summary>
    /// Returns true if the timestamp is clearly off-beat but within a larger contra window.
    /// This is useful for contratempo checks (optional).
    /// </summary>
    public bool IsOffBeatContratempo(double dspTime)
    {
        if (lastBeatDSP < 0) return false;
        float onBeatTol = beatConfig != null ? Mathf.Abs(beatConfig.onBeatWindowSec) : 0.07f;
        float contraTol = onBeatTol * 2.0f; // example: twice the on-beat window
        double diff = Math.Abs(dspTime - lastBeatDSP);
        return diff > onBeatTol && diff <= contraTol;
    }

    /// <summary>
    /// Exposes the last beat's DSP timestamp for debugging or alignment.
    /// </summary>
    public double LastBeatDSP => lastBeatDSP;
    #endregion
}