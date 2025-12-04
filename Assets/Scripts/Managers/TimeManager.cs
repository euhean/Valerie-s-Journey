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

    [Header("Audio")]
    [Tooltip("Optional AudioClip to play on each beat (metronome sound)")]
    public AudioClip beatSound;
    [Range(0f, 1f)]
    public float beatVolume = 0.5f;
    
    private AudioSource audioSource;

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
        
        // Set up AudioSource for beat sounds
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = beatVolume;
        }
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
        // Start the metronome loop using this MonoBehaviour
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
        
        double dspNow = AudioSettings.dspTime;
        double secondsPerBeat = 60.0 / Math.Max(1f, bpm);
        
        // Start slightly in the future to allow scheduling
        nextBeatDSP = dspNow + 0.5; 
        lastBeatDSP = nextBeatDSP - secondsPerBeat;

        // Ensure clip is assigned
        if (audioSource != null && beatSound != null)
        {
            audioSource.clip = beatSound;
        }

        bool isAudioScheduled = false;

        while (running)
        {
            dspNow = AudioSettings.dspTime;
            secondsPerBeat = 60.0 / Math.Max(1f, bpm);

            // 1. Schedule Audio Ahead (lookahead 0.1s)
            if (!isAudioScheduled && dspNow + 0.1 >= nextBeatDSP)
            {
                if (audioSource != null && beatSound != null)
                {
                    if (audioSource.clip != beatSound) audioSource.clip = beatSound;
                    audioSource.PlayScheduled(nextBeatDSP);
                }
                isAudioScheduled = true;
            }

            // 2. Fire Logic (Catch-up loop)
            // CHANGED: Replaced while loop with if + reset to prevent potential freezes if dspTime desyncs
            if (dspNow >= nextBeatDSP)
            {
                // Fire beat logic
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
                
                // If we are still behind after one beat, just reset to current time to avoid spiral of death
                if (AudioSettings.dspTime >= nextBeatDSP + secondsPerBeat)
                {
                    nextBeatDSP = AudioSettings.dspTime + secondsPerBeat;
                    // CRITICAL: Update lastBeatDSP so IsOnBeat() works correctly relative to the new timeline
                    lastBeatDSP = nextBeatDSP - secondsPerBeat;
                }
                else
                {
                    nextBeatDSP += secondsPerBeat;
                }
                
                isAudioScheduled = false; // Reset for the new nextBeatDSP
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
        
        // Check distance to LAST beat
        double distToLast = Math.Abs(dspTime - lastBeatDSP);
        
        // Check distance to NEXT beat (predictive)
        double secondsPerBeat = 60.0 / Math.Max(1f, bpm);
        double nextBeat = lastBeatDSP + secondsPerBeat;
        double distToNext = Math.Abs(dspTime - nextBeat);

        // If we are closer to the next beat than the last one, treat it as hitting the next beat early
        return distToLast <= tol || distToNext <= tol;
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