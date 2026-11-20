using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// TimeManager / metronome: publishes BeatTickEvent using DSP timing and consults BeatConfig.
/// Designed to be lifecycle-managed by GameManager (Configure/Initialize/BindEvents/StartRuntime/StopRuntime).
/// </summary>
public class TimeManager : BaseManager
{
    [Header("Tempo")]
    [Tooltip("Beats Per Minute. Can be tuned in editor for testing.")]
    public float bpm = 120f;

    [Header("Beat Config (assigned by GameManager or in Inspector)")]
    public BeatConfig beatConfig; // pushed by GameManager when it has one

    [Header("Audio")]
    [Tooltip("Optional AudioClip to play on each beat (metronome sound)")]
    public AudioClip beatSound;
    [Range(0f, 1f)]
    public float beatVolume = 0.5f;
    
    private AudioSource audioSource;

    // Timing state. The beat grid is: lastBeatDSP (beat lastBeatIndex) and nextBeatDSP (the one after).
    private Coroutine metronomeCoroutine;
    private double lastBeatDSP = 0.0;
    private double nextBeatDSP = 0.0;
    private int lastBeatIndex = -1; // -1 until the first beat has fired

    private double SecondsPerBeat => 60.0 / Math.Max(1f, bpm);
    private float OnBeatWindow => beatConfig != null ? Mathf.Abs(beatConfig.onBeatWindowSec) : 0.07f;

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
        EventBus.Instance.Subscribe<PlayerDiedEvent>(OnPlayerDied);
    }

    public override void StartRuntime()
    {
        if (metronomeCoroutine != null) return;
        metronomeCoroutine = StartCoroutine(MetronomeLoop());
        DebugHelper.LogManager("[TimeManager] Runtime started.");
    }

    public override void StopRuntime()
    {
        if (metronomeCoroutine == null) return;
        StopCoroutine(metronomeCoroutine);
        metronomeCoroutine = null;
        DebugHelper.LogManager("[TimeManager] Runtime stopped.");
    }

    public override void UnbindEvents()
    {
        EventBus.Instance.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
    }

    // The metronome has nothing to drive once the player is dead.
    private void OnPlayerDied(PlayerDiedEvent _) => StopRuntime();
    #endregion

    #region Metronome loop
    private IEnumerator MetronomeLoop()
    {
        lastBeatIndex = -1;
        nextBeatDSP = AudioSettings.dspTime + SecondsPerBeat * 0.1; // small lead-in before beat 0

        while (true)
        {
            // Fires every beat that is due, so a frame hitch never drops or shifts a beat.
            while (AudioSettings.dspTime >= nextBeatDSP)
            {
                lastBeatIndex++;
                lastBeatDSP = nextBeatDSP;
                nextBeatDSP += SecondsPerBeat;

                if (beatSound != null && audioSource != null)
                    audioSource.PlayOneShot(beatSound);

                EventBus.Instance.Publish(new BeatTickEvent { beatIndex = lastBeatIndex, dspTime = lastBeatDSP });
            }

            yield return null;
        }
    }
    #endregion

    #region Beat judgement
    /// <summary>
    /// Judges a timestamp against the NEAREST beat: the last one fired or the next one due.
    /// A press slightly early is as on-beat as a press slightly late.
    /// Returns true and that beat's index when the timestamp is within ±onBeatWindowSec of it.
    /// </summary>
    public bool TryGetBeatAt(double dspTime, out int beatIndex)
    {
        beatIndex = -1;
        if (metronomeCoroutine == null) return false;

        double sinceLast = dspTime - lastBeatDSP;
        double untilNext = nextBeatDSP - dspTime;
        bool nextIsNearer = lastBeatIndex < 0 || untilNext < sinceLast;

        double distance = Math.Abs(nextIsNearer ? untilNext : sinceLast);
        if (distance > OnBeatWindow) return false;

        beatIndex = nextIsNearer ? lastBeatIndex + 1 : lastBeatIndex;
        return true;
    }
    #endregion
}