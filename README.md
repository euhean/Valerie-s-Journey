# Valerie's Journey

[![Repo checks](https://github.com/euhean/Valerie-s-Journey/actions/workflows/repo-checks.yml/badge.svg)](https://github.com/euhean/Valerie-s-Journey/actions/workflows/repo-checks.yml)
[![Unity build](https://github.com/euhean/Valerie-s-Journey/actions/workflows/unity-build.yml/badge.svg)](https://github.com/euhean/Valerie-s-Journey/actions/workflows/unity-build.yml)

A 2D top-down action game where you fight to the beat. Attacks that land on the beat build a
combo, and the fourth one in a row becomes a strong attack. Inspired by **Hotline Miami** for
the action and **Hades** for the storytelling.

**Status:** early prototype (Episode 0). One playable test level with Vincent, the first of
three planned characters. The full design is in the [Game Design Document](docs/GDD.md).

## What works today

- **Rhythm combat**: a metronome runs on the audio clock. Every attack press is judged against
  the nearest beat (±70 ms). Four on-beat attacks in a row trigger a strong attack that deals
  four times the damage and can't be redirected mid-swing. An off-beat press or a skipped beat
  breaks the combo.
- **Movement and aiming**: twin-stick controls. Movement uses acceleration, deceleration, a
  harder brake when reversing, and a dead zone with hysteresis so the stick doesn't jitter.
- **Enemies**: they chase the player and attack in melee, flash when hit and shake on strong hits.
- **Beat HUD**: four bars light up in time with the beat, green or red when you attack on or off it.
- **Game over and restart**: dying shows a game-over screen; restart reloads the level.
- **Dialogue**: speaker, text and portrait read from JSON (built, not yet used in the test level).

## Controls

| Action | Gamepad | Keyboard and mouse |
| --- | --- | --- |
| Move | Left stick | WASD or arrow keys |
| Aim | Right stick | — (not yet supported) |
| Attack | RB | Left click or Enter |
| Confirm (menus, dialogue) | A | — |

## Tech stack

| Part | Technology |
| --- | --- |
| Engine | Unity 6 (6000.1.15f1), 2D |
| Language | C# |
| Input | Unity Input System (actions in `Assets/InputSystem_Actions.inputactions`) |
| UI | uGUI and TextMesh Pro |
| Tuning data | ScriptableObjects (`BeatConfig`, `CombatConfig`, `MovementConfig`, `DebugConfig`) |

```
Assets/
  Scripts/
    Core/          GameManager (bootstrap), EventBus, events, BaseManager
    Managers/      InputManager, TimeManager (metronome), LevelManager, DialogManager
    Entities/      Entity (health and state), Player, Enemy
    Gameplay/      Movement, attack controller, weapon, animator, beat HUD, camera
    Configs/       ScriptableObject definitions and their assets
    Utilities/     Debug logging, constants, helpers
  Prefab/          Player, Enemy, walls, camera, HUD and dialogue canvases
  Scenes/          Test0 (the playable level), plus work-in-progress scenes
  Resources/       Dialogue JSON, portraits, DebugConfig
docs/              Game design document and code audit
```

## Getting started

1. Install **Unity 6000.1.15f1** through Unity Hub (other versions may upgrade or break the project).
2. Clone the repository and open the folder in Unity Hub. Unity installs the packages listed in
   `Packages/manifest.json` on first open.
3. Open `Assets/Scenes/Test0.unity` and press Play. A gamepad is recommended.

Tuning lives in the config assets under `Assets/Scripts/Configs/`: tempo window and combo rules in
`BeatConfig`, damage and attack window in `CombatConfig`, player movement in `MovementConfig`. The
tempo itself (90 BPM in Test0) is set on the `TimeManager` object in the scene. Log categories are
switched on and off in `Assets/Resources/DebugConfig`.

## Checks

GitHub Actions runs two workflows:

- **Repo checks** (every push, under a minute, no Unity needed): every asset has its `.meta`
  file and every `.meta` has its asset, no two files differ only in letter case, no leftover
  merge-conflict markers, the package manifest is committed, and the build scene list is valid.
  Run it locally with `python .github/scripts/check_repo.py`.
- **Unity build** (pushes to `main` and pull requests): opens the project in Unity, compiles it
  and builds the game for Windows. The build can be downloaded from the run's page for 14 days.

## Design decisions

- **The beat runs on the audio clock.** The metronome uses `AudioSettings.dspTime`, which counts
  samples the audio hardware has played, instead of `Time.time`. Frame time drifts against audio
  over a few minutes; the audio clock can't. If a frame hitches, the metronome fires every beat it
  missed rather than shifting the grid.
- **One bootstrap path.** `GameManager` persists across scenes and wires the level every time a
  scene loads, the first one included: it finds the managers, runs their lifecycle
  (Configure → Initialize → BindEvents → StartRuntime) and hands the player its dependencies.
  Unity raises the scene-loaded event after every object's `OnEnable` and before any `Start`, so
  everything is wired before gameplay code runs. A restart takes exactly the same path as a fresh
  start.
- **Dependencies flow one way.** `GameManager` pushes managers and configs into the player's
  components; gameplay code never reaches back into `GameManager` to fetch them.
- **Everything talks through one event bus.** Beats, button presses, attacks, damage, spawns and
  deaths are typed events on `EventBus`. A new system (a combo counter, hit sounds, a score)
  subscribes to the events it needs without changing the code that publishes them, and the
  publisher never needs to know who's listening.
- **Each job has one owner.** The movement script is the only one that aims the weapon; the attack
  controller decides when an attack window opens and closes; the weapon only detects hits. When
  two scripts used to do the same job, they disagreed (a strong attack could be redirected because
  one of them ignored the aim lock).
- **Tuning in assets, not code.** Combat, timing and movement values live in ScriptableObjects so
  they can be changed in the Inspector, even during play, without recompiling.

## How beat judgement works

A press counts as on the beat when it falls within ±70 ms (`BeatConfig.onBeatWindowSec`) of the
**nearest** beat, which can be the one that just played or the one about to play. Players tend to
press slightly early, so comparing only with the last beat would reject every press that
anticipates the next one.

The metronome keeps the grid it's on: the time and index of the last beat, and the time of the
next. The input system stamps every press with the audio clock, and `TimeManager` measures the
press against both beats:

```csharp
double sinceLast = dspTime - lastBeatDSP;
double untilNext = nextBeatDSP - dspTime;
bool nextIsNearer = lastBeatIndex < 0 || untilNext < sinceLast;

double distance = Math.Abs(nextIsNearer ? untilNext : sinceLast);
if (distance > OnBeatWindow) return false;

beatIndex = nextIsNearer ? lastBeatIndex + 1 : lastBeatIndex;
```

It returns which beat the press belonged to, and the combo is counted in those beat numbers rather
than with timers. An on-beat press extends the streak, unless a beat was skipped since the last
one, in which case it starts a new streak of one. A second press on the same beat adds nothing.
The fourth press in a row becomes a strong attack and spends the combo:

```csharp
if (onBeat && beatIndex != lastOnBeatIndex)
{
    int missedBeats = beatIndex - lastOnBeatIndex - 1;
    bool streakBroken = InactivityBeats > 0 && missedBeats >= InactivityBeats;

    lastOnBeatIndex = beatIndex;
    SetStreak(streakBroken ? 1 : onBeatStreak + 1);

    if (onBeatStreak >= ComboStreak)
    {
        isStrong = true;
        SetStreak(0); // the strong attack spends the combo
    }
}
```

A combo can also break while you wait: on every beat, the attack controller checks whether the
previous beat's window closed without an on-beat press. It checks the previous beat, not the
current one, because the current beat's window is still open for a slightly late press.

**Not modelled yet:** music (only a metronome click), a per-device latency offset, and the
off-beat pattern Valerie's design needs.

## Roadmap

- Music playback with the beat grid locked to the song, and latency calibration.
- Game flow: main menu, pause, dialogue scenes and level completion.
- Valerie and Vin, each with their own rhythm pattern, and Vincent's fire-cone strong attack.
- Scoring, combo HUD and enemy waves aligned to the music.

The full list, with the reasoning behind each item, is in the
[code audit](docs/CODE_AUDIT.md#6-next-commits-in-suggested-order).

## License

MIT. See [LICENSE](LICENSE).
