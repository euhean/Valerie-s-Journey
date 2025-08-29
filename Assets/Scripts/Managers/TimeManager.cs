using System;
using UnityEngine;

/// <summary>
/// Central rhythm/conductor. Emits beat events based on BPM and
/// provides on-beat/off-beat checks for timing user inputs.
/// Lifecycle-aware.
/// </summary>
public class TimeManager : BaseManager {
    [Range(40f, 220f)]
    public float bpm = 120f;

    [Tooltip("Half-window in seconds considered 'on beat'")]
    public float onBeatWindowSec = 0.07f;

    [Tooltip("Seconds of lead-in before beats start (for a metronome or loading delay)")]
    public float startDelaySec = 0.4f;

    [Header("Metronome Settings")]
    public bool enableMetronome = true;
    public AudioClip metronomeClip;
    [Range(0f,1f)] public float metronomeVolume = 0.5f;

    public event Action<int> OnBeat; // beat index: 0..3 for 4/4

    // internal state
    private double nextBeatDSP;
    private int beatIndex;
    private AudioSource metronomeAudioSource;
    private bool running = false;
    private double beatDuration => 60.0 / Math.Max(0.0001, bpm);

    public override void Configure(GameManager gm) {
        base.Configure(gm);
        DebugHelper.LogManager("TimeManager.Configure()");
        // Prepare audio source (idempotent)
        if (metronomeAudioSource == null) {
            metronomeAudioSource = gameObject.GetComponent<AudioSource>();
            metronomeAudioSource ??= gameObject.AddComponent<AudioSource>();
            metronomeAudioSource.playOnAwake = false;
            metronomeAudioSource.clip = metronomeClip;
            metronomeAudioSource.volume = metronomeVolume;
        }
    }

    public override void Initialize() {
        DebugHelper.LogManager("TimeManager.Initialize()");
        // Precompute anything if necessary; ensure beat index state is sane
        beatIndex = -1;
        running = false;
    }

    public override void BindEvents() {
        DebugHelper.LogManager("TimeManager.BindEvents()");
        // Nothing to subscribe to at the moment; other systems will subscribe to us
    }

    public override void StartRuntime() {
        DebugHelper.LogManager("TimeManager.StartRuntime()");
        // schedule first beat after lead-in
        double dsp = AudioSettings.dspTime;
        nextBeatDSP = dsp + startDelaySec;
        beatIndex = -1;
        running = true;
    }

    public override void Pause(bool isPaused) {
        DebugHelper.LogManager($"TimeManager.Pause({isPaused})");
        running = !isPaused;
        if (isPaused) metronomeAudioSource?.Pause();
        else metronomeAudioSource?.UnPause();
    }

    public override void Teardown() {
        DebugHelper.LogManager("TimeManager.Teardown()");
        running = false;
        metronomeAudioSource?.Stop();
    }

    private void Update() {
        if (!running) return;
        double dsp = AudioSettings.dspTime;
        double dur = beatDuration;

        while (dsp >= nextBeatDSP) {
            beatIndex = (beatIndex + 1) % 4;
            PlayMetronomeClick();
            OnBeat?.Invoke(beatIndex);
            nextBeatDSP += dur;
        }
    }

    private void PlayMetronomeClick() {
        if (enableMetronome && metronomeAudioSource != null && metronomeClip != null) {
            metronomeAudioSource.volume = metronomeVolume;
            metronomeAudioSource.PlayOneShot(metronomeClip);
        }
    }

    public bool IsOnBeat(double dspTime) {
        double dur = beatDuration;
        double lastBeatTime = nextBeatDSP - dur;
        double dist = Math.Abs((dspTime - lastBeatTime) % dur);
        dist = Math.Min(dist, dur - dist);
        return dist <= onBeatWindowSec;
    }

    public bool IsOffBeatContratempo(double dspTime) {
        double dur = beatDuration;
        double halfBeat = dur * 0.5;
        double lastBeatTime = nextBeatDSP - dur;
        double tFromLastBeat= dspTime - lastBeatTime;
        double dist = Math.Abs((tFromLastBeat % dur) - halfBeat);
        dist = Math.Min(dist, dur - dist);
        return dist <= onBeatWindowSec;
    }
}