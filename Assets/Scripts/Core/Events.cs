using System.Collections.Generic;

/// <summary>
/// Basic spawn events used by LevelManager (and future systems).
/// </summary>
public class PlayerSpawnedEvent {
    public Player player;
}

public class PlayerDiedEvent {
    public Player player;
}

public class EnemySpawnedEvent {
    public Enemy enemy;
}

// User-requested event payloads
public class DialogueEvent {
    public string speaker;
    public string text;
}

public class CinematicEvent {
    public string id;
    public float duration;
}

// Small text-only event
public class TextEvent {
    public string text;
    public float duration = 1f;
}

public class MenuEvent {
    public string menuId;
    public bool open;
}

// GameplayEvent: simple flag if in combat
public class GameplayEvent
{
    public bool inCombat;
}

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