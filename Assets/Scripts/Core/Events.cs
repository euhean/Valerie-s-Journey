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
public class GameplayEvent {
    public bool inCombat;
}