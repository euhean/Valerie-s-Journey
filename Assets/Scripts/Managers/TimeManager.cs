using System;
using UnityEngine;

/// <summary>
/// Central rhythm/conductor. Emits beat events based on BPM and
/// provides on‑beat/off‑beat checks for timing user inputs.
/// </summary>
public class TimeManager : BaseManager {
    [Range(40f, 220f)]
    public float bpm = 120f;

    [Tooltip("Half‑window in seconds considered 'on beat'")]
    public float onBeatWindowSec = 0.07f;

    [Tooltip("Seconds of lead‑in before beats start (for a metronome or loading delay)")]
    public float startDelaySec = 0.4f;

    public event Action<int> OnBeat; // beat index: 0..3 for 4/4

    private double nextBeatDSP;
    private int    beatIndex;

    public override void StartRuntime() {
        // Schedule first beat after a lead‑in
        double dsp = AudioSettings.dspTime;
        nextBeatDSP = dsp + startDelaySec;
        beatIndex   = -1; // first increment sets to 0
    }

    private void Update() {
        double dsp     = AudioSettings.dspTime;
        double beatDur = 60.0 / bpm;

        // Fire as many beats as needed if Update lags behind
        while (dsp >= nextBeatDSP) {
            beatIndex = (beatIndex + 1) % 4; // 4/4 time
            OnBeat?.Invoke(beatIndex);
            nextBeatDSP += beatDur;
        }
    }

    /// <summary>
    /// Returns true if the given DSP time is within the on‑beat window
    /// around the nearest beat.
    /// </summary>
    public bool IsOnBeat(double dspTime) {
        double beatDur      = 60.0 / bpm;
        double lastBeatTime = nextBeatDSP - beatDur;
        double dist         = Math.Abs((dspTime - lastBeatTime) % beatDur);
        dist = Math.Min(dist, beatDur - dist);
        return dist <= onBeatWindowSec;
    }

    /// <summary>
    /// Returns true if the DSP time is near the half‑beat (contratempo).
    /// </summary>
    public bool IsOffBeatContratempo(double dspTime) {
        double beatDur      = 60.0 / bpm;
        double halfBeat     = beatDur * 0.5;
        double lastBeatTime = nextBeatDSP - beatDur;
        double tFromLastBeat= dspTime - lastBeatTime;
        double dist         = Math.Abs((tFromLastBeat % beatDur) - halfBeat);
        dist = Math.Min(dist, beatDur - dist);
        return dist <= onBeatWindowSec;
    }
}