using System.Collections.Generic;

// ---- Entity lifecycle ----

public class PlayerSpawnedEvent {
    public Player player;
}

public class PlayerDiedEvent {
    public Player player;
}

public class EnemySpawnedEvent {
    public Enemy enemy;
}

// ---- Input edges (published by InputManager) ----

/// <summary>The basic-action button went down. dspTime is the audio clock at the press.</summary>
public struct BasicPressedEvent
{
    public double dspTime;
}

public struct SubmitPressedEvent { }
public struct CancelPressedEvent { }

// ---- Tempo (published by TimeManager) ----

/// <summary>One metronome beat. dspTime is the beat's scheduled time on the audio clock.</summary>
public struct BeatTickEvent
{
    public int beatIndex;
    public double dspTime;
}

// ---- Combat ----

/// <summary>
/// An attack window just opened (the press was accepted). Subscribe for swing
/// animation, swing sound and beat feedback.
/// </summary>
public struct AttackStartedEvent
{
    public Entity attacker;
    public bool   isStrong;
    public bool   onBeat;
}

/// <summary>The on-beat streak changed. streak == 0 means the combo was reset or spent.</summary>
public struct ComboChangedEvent
{
    public int streak;
    public int streakForStrong;
}

/// <summary>
/// Fired once per swing when its attack window closes, with everything the swing hit.
/// Subscribe for scoring and swing-level effects.
/// </summary>
public struct AttackResolved
{
    public Entity attacker;
    public bool   success;
    public bool   isStrong;
    public bool   onBeat;
    public IReadOnlyList<Entity> hitTargets;
    public double dspTime;

    public AttackResolved(Entity attacker, bool success, bool isStrong, bool onBeat, IReadOnlyList<Entity> hitTargets, double dspTime)
    {
        this.attacker   = attacker;
        this.success    = success;
        this.isStrong   = isStrong;
        this.onBeat     = onBeat;
        this.hitTargets = hitTargets;
        this.dspTime    = dspTime;
    }
}

/// <summary>
/// Fired at the moment of impact, once per target. A swing that hits N targets
/// produces N DamageApplied and then one AttackResolved.
/// </summary>
public struct DamageApplied
{
    public Entity attacker;
    public Entity target;
    public float  amount;
    public bool   killingBlow;
    public bool   isStrong;
    public bool   onBeat;

    public DamageApplied(Entity attacker, Entity target, float amount, bool killingBlow, bool isStrong, bool onBeat)
    {
        this.attacker    = attacker;
        this.target      = target;
        this.amount      = amount;
        this.killingBlow = killingBlow;
        this.isStrong    = isStrong;
        this.onBeat      = onBeat;
    }
}