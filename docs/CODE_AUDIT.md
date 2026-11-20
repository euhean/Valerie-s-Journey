# Valerie's Journey — code audit and refactor log

Branch: `refactor/lifecycle-rhythm-audit` (6 commits on top of `main` @ `e43eefd`).
Unity version: 6000.1.15f1. GDD used for comparison: `docs/GDD.md` (the README at the time).

> **Not yet run in Unity.** The changes were written and reviewed without the Editor
> (no Unity install was available). The C# was syntax-checked with a parser, not
> compiled. Section 1 is the checklist to run before merging.

Contents

1. [What to check first in the Editor](#1-what-to-check-first-in-the-editor)
2. [The program life-cycle, before and after](#2-the-program-life-cycle-before-and-after)
3. [Commits made, and why](#3-commits-made-and-why)
4. [GDD compliance](#4-gdd-compliance)
5. [Things only you can fix (need the Editor or a decision)](#5-things-only-you-can-fix)
6. [Next commits, in suggested order](#6-next-commits-in-suggested-order)
7. [SOLID: where the code stands](#7-solid-where-the-code-stands)
8. [References](#8-references)

---

## 1. What to check first in the Editor

Open `Assets/Scenes/Test0.unity` and press Play.

| # | Do this | Expect | If not |
|---|---------|--------|--------|
| 1 | Open the project | No compile errors | Send me the Console output |
| 2 | Press Play | Console shows `Scene loaded: Test0 — wiring managers`, then `Game state changed: Boot → Gameplay`; metronome ticks; two enemies spawn and chase | If the first line never appears, `sceneLoaded` did not fire for the first scene — check *Project Settings → Editor → Enter Play Mode Settings*: "Reload Scene" must stay enabled |
| 3 | Move, aim with the right stick | Sword is visible from the start and follows the stick | — |
| 4 | Attack off the beat | Sword turns red for 0.2 s, bar turns red, enemy in reach loses 5 HP | — |
| 5 | Attack on 4 consecutive beats (slightly early or late is fine) | Bars go green; the 4th swing is orange and deals 20 | This was impossible before (section 3, commit 4) |
| 6 | During the orange swing, flick the right stick | Sword does **not** turn | — |
| 7 | Swing at a dead (grey) enemy | Nothing happens, no `[Weapon] Hit` log | — |
| 8 | Let both enemies hit you at once | Player flashes red and returns to white | Before, it could stay red |
| 9 | Die → RESTART | Scene reloads; you can attack again; dying again shows the death screen again | This was broken before (commit 2) |
| 10 | Toggle `DebugConfig` flags (now in `Assets/Resources`) | Log categories switch on/off | Before, the toggles did nothing |

---

## 2. The program life-cycle, before and after

### Before

```
GameManager.Awake            singleton, DontDestroyOnLoad
all other Awake / OnEnable   PlayerAttackController subscribes to InputManager/TimeManager
                             C# events — only if those refs are already set in the Inspector
sceneLoaded                  GameManager nulls its manager refs, finds them again, starts nothing
all Start                    Player/PAC/Mover pull managers from GameManager.Instance
+1 frame (Start coroutine)   AutoConfigureScene → Configure → Initialize → BindEvents → Gameplay
...
RESTART (LoadScene)          sceneLoaded: find new managers, StartRuntime only
                             → BindEvents never runs again
```

Problems this produced:

- **Restart was broken.** After a reload nothing called `BindEvents`, so `InputManager`
  did not listen to the attack button and `LevelManager` did not listen to
  `PlayerDiedEvent`. You could move but not attack, and a second death showed no
  death screen.
- **A spawned player could not attack.** A prefab instantiated at runtime has no manager
  refs when its `OnEnable` runs, so the `+=` subscriptions were skipped. It only worked
  in `Test0` because that scene's Player instance has the refs set by hand as overrides.
- **Wiring ran in both directions** (GameManager pushed with `??=`, components pulled from
  `GameManager.Instance`), in three different life-cycle phases.
- **`GameManager` knew about `BeatVisualizer`** (`HandlePlayerDeath` searched the scene
  for them) — the orchestrator had to be edited for every system that cares about a death.

### After

```
GameManager.Awake            singleton, DontDestroyOnLoad
all other Awake / OnEnable   components subscribe to the EventBus (needs no refs)
sceneLoaded                  GameManager.OnSceneLoaded — the ONLY bootstrap path:
                               stop old managers → discover managers in the scene
                               → Configure → Initialize → BindEvents (foreach over one list)
                               → RegisterPlayer(scene player)  (wires mover / PAC / weapon)
                               → ChangeState(Gameplay) → StartRuntime
all Start                    everything is already wired
...
scene unload                 each manager's OnDisable stops and unbinds itself
RESTART (LoadScene)          the same sceneLoaded path runs again
```

Rules the code now follows:

- **One bootstrap path** — first load and reload are the same code.
- **One wiring direction** — `GameManager.RegisterPlayer` pushes managers/configs into the
  player's components. Nothing in `Entities/` or `Gameplay/` reads
  `GameManager.Instance` to *get dependencies* any more (Enemy reads `MainPlayer` once
  for its target).
- **One event mechanism** — everything goes through `EventBus`. Publishers do not know
  their listeners; listeners need no reference to the publisher.
- **Each system reacts to events itself** — `TimeManager` stops on `PlayerDiedEvent`,
  `BeatVisualizer` clears on it. `GameManager` does not orchestrate that.

### Event map (after)

| Event | Published by | Listeners today |
|-------|--------------|-----------------|
| `BasicPressedEvent {dspTime}` | InputManager | PlayerAttackController |
| `SubmitPressedEvent`, `CancelPressedEvent` | InputManager | DialogManager (submit) |
| `BeatTickEvent {beatIndex, dspTime}` | TimeManager | PlayerAttackController, BeatVisualizer |
| `AttackStartedEvent {attacker, isStrong, onBeat}` | PlayerAttackController | PlayerAnimator, BeatVisualizer |
| `DamageApplied {…}` (per target, at impact) | Weapon | — (ready for hit SFX, damage numbers, score) |
| `AttackResolved {…}` (per swing, at window end) | PlayerAttackController | — (ready for score) |
| `ComboChangedEvent {streak, streakForStrong}` | PlayerAttackController | — (ready for a combo HUD) |
| `PlayerSpawnedEvent` | GameManager.RegisterPlayer | Enemy |
| `PlayerDiedEvent` | Player | LevelManager, TimeManager, BeatVisualizer |
| `EnemySpawnedEvent` | LevelManager | — |

---

## 3. Commits made, and why

### 1. `chore(repo)` — stray scene, build scene list, DebugConfig location

- Deleted `Assets/Scenes/aphla.unity`. It had no `.meta` and differs from `Aphla.unity`
  only by case; on Windows/macOS the two collide in a checkout.
- Added `Test0` to `EditorBuildSettings`. The list was empty, so a build had no scene.
- Moved `DebugConfig.asset` to `Assets/Resources/`. `DebugHelper` loads it with
  `Resources.Load("DebugConfig")`; from `Scripts/Configs/` that returned `null`, and
  `null` means "log everything".
- README said Unity 2023 LTS; the project is on 6000.1.15f1.

### 2. `fix(lifecycle)` — one scene bootstrap path; managers clean up after themselves

Fixes the broken restart described in section 2.

- `GameManager.OnSceneLoaded` is the single bootstrap. The Start coroutine with its
  one-frame delay is gone. Unity documents that `sceneLoaded` is raised after `OnEnable`
  and before `Start` [1][2], which is exactly the slot we need.
- Managers are kept in one `List<BaseManager>`; each phase is a `foreach`. The old
  hand-written lists had already drifted (`ChangeState` skipped `DialogManager`).
- A player placed in the scene now goes through `RegisterPlayer` like a spawned one.
- `BaseManager.OnDisable` calls `StopRuntime` + `UnbindEvents`. A manager destroyed with
  its scene unsubscribes itself; `GameManager` no longer calls methods on dead objects.
- `ChangeState` only starts/stops runtimes. It used to *unbind* everything when leaving
  Gameplay and never rebind, so any future return to Gameplay would have been dead.
- Removed dead API: `BaseManager.Pause/Teardown`, `GameManager.UnregisterPlayer`,
  `GameManager.HandlePlayerDeath`, `GameplayEvent`.

### 3. `refactor(events)` — beat and input events travel on the EventBus only

Fixes "a spawned player cannot attack" and removes the second event mechanism.

- New events (see the map above). `InputManager`/`TimeManager` no longer expose C# events.
- `PlayerAnimator` and `BeatVisualizer` listen to `AttackStartedEvent`, not the raw
  button. The animation used to play on presses the cooldown had rejected, and the HUD
  judged the press a second time by itself — two places that could disagree with combat.
- `BeatVisualizer`: removed obsolete `FindObjectOfType`, the per-beat `Debug.Log`
  (it logged "FALLO" twice a second, in release builds too), and its manager references.
- `EventBus`: subscriber lists are copy-on-write arrays. `Publish` used to allocate a
  `List` copy for every event; now it allocates nothing and is still safe when a handler
  unsubscribes during publish. Removed unused `SubscribeOnce`/`ClearAll`.
- Removed never-used event classes (`DialogueEvent`, `CinematicEvent`, `TextEvent`,
  `MenuEvent`). Add them back when something publishes them.

### 4. `fix(rhythm)` — judge presses against the nearest beat; count combos in beats

Two bugs made the GDD's core mechanic — strong attack on the 4th consecutive on-beat
press — unreachable:

1. `IsOnBeat` compared a press only with the **last** beat. A press 30 ms *before* a beat
   was measured against the previous beat and judged off-beat, so every press that
   anticipated the beat was rejected.
2. The combo reset logic counted ticks since the last on-beat press and reset at
   `resetOnInactivityBeats`. The shipped `BeatConfig` has `1`, so the tick right after
   any on-beat press reset the streak. It could never exceed 1.

Now:

- `TimeManager.TryGetBeatAt(dspTime, out beatIndex)` compares with the nearest beat —
  the last fired or the next due — and returns which beat it was. Comparing against the
  nearest beat is how rhythm games judge input [3][4].
- `PlayerAttackController` stores the beat index of the last on-beat press. A press on
  the following beat extends the streak; skipping `resetOnInactivityBeats` beats
  restarts it. With the value `1` this means "hit every beat", which is the GDD pattern
  `b – b – b – s`.
- The strong attack spends the combo when it triggers.
- `ComboChangedEvent` is published on every change (the GDD lists `ComboChanged`).
- Removed `IsOffBeatContratempo`: it tested "between 1× and 2× the window", which is not
  a half-beat off-beat, and nothing called it. Valerie will need a real one (section 6).

### 5. `fix(combat)` — one owner for aim, one timer for the attack window

- **Aim lock.** `Player.Update` and `PlayerMover2D.FixedUpdate` both aimed the weapon;
  the first ignored the lock. GDD: "Strong attacks cannot be redirected after trigger".
  `PlayerMover2D` is now the only aimer.
- **Window timing.** Controller and weapon each ran their own timer for the same window.
  The controller owns it now (`OpenAttackWindow` / `CloseAttackWindow`); `Weapon` only
  detects hits. `AbortAttack` really stops the window.
- **Hit detection.** The trigger is enabled only while the window is open and hits come
  from `OnTriggerEnter2D` and `OnTriggerStay2D`. Enabling the collider at window start
  makes Unity report enemies already inside the blade as new contacts.
- **Corpses.** Dead enemies stay in the scene with a live collider; hitting one counted
  as a successful hit. They are skipped now.
- `DamageApplied.onBeat` is real (was hard-coded `false`).
- Swing colour is set by the weapon directly; the old flash was overwritten in the same
  call and never showed.
- Weapon visibility follows the owner's duty state (it used to be invisible until the
  first swing and visible after death).
- Wiring: removed `ComponentHelper.FindWeaponFallback` (a tag search plus a scene-wide
  search, called from three scripts) — the weapon is a child of the Player prefab, so
  `GetComponentInParent/InChildren` is exact. `Player` no longer has manager fields.
- `RegisterPlayer` uses `== null`, not `??=`, on serialized references. C#'s `?.`, `??`
  and `??=` do a plain reference check and skip Unity's own null check [5].

### 6. `fix(enemy,feedback)` — frame-rate independent chase; flashes restore the right colour

- **Enemy speed depended on the frame rate.** `MovePosition` was called every rendered
  frame with `Time.deltaTime`. Unity applies the move on the next physics update and
  only the last call counts [6]. At 144 fps the enemy moved at about a third of
  `patrolSpeed`. The loop now runs once per physics step with `Time.fixedDeltaTime`.
  *You may want to retune `patrolSpeed` — enemies now move at the speed the Inspector says.*
- Enemy no longer calls `FindFirstObjectByType<Player>()` in its loop.
- **Stuck flash colours.** Each flash remembered "the colour before me". Two overlapping
  flashes left the sprite tinted. `AnimationHelper` keeps one base colour per flashing
  renderer; `Player`/`Enemy` call `CancelFlash` before applying their alive/dead colour.

---

## 4. GDD compliance

✅ works as described · 🟡 partial · ❌ missing

| GDD item | State | Notes |
|----------|-------|-------|
| **Game flow FSM** (Main Screen → Menu → Preload → Cinematic → Dialogue → Gameplay → Completion → Exit/Continue) | ❌ | The enum exists; the game boots straight into `Gameplay`. No other state is ever entered. |
| Additive scene loading, `App_Boot` / `UI_Global` / `EP0_Level` | ❌ | One scene (`Test0`), restart by `LoadScene`. |
| Beat source: single DSP clock | ✅ | `AudioSettings.dspTime` metronome with catch-up loop. |
| Quantisation windows (±30/60/90 ms) | 🟡 | One window (`BeatConfig.onBeatWindowSec` = 70 ms). Now symmetric around the beat. |
| Latency calibration | ❌ | — |
| Events `OnSubBeat`, `OnBeat`, `OnBar`, `OnPhrase` | 🟡 | `BeatTickEvent` only. |
| Music playback, song-length level | ❌ | No `AudioManager`, no song; only an optional metronome click (and `.gitignore` excludes `*.mp3`, so the click is not in the repo). |
| **Vincent** basic: sword, steerable mid-swing | ✅ | — |
| Vincent strong on the 4th consecutive on-beat basic | ✅ *(fixed)* | Was unreachable — commit 4. |
| Vincent strong: AOE fire cone | ❌ | Same hitbox as the basic, 20 damage, orange tint. |
| Strong direction locked at trigger | ✅ *(fixed)* | Commit 5. |
| **Valerie**, **Vin** | ❌ | — |
| Controls: Move LS, Aim RS | ✅ | — |
| Basic attack on **A** | ❌ | Bound to **RB**, left mouse, Enter. `A` is bound to UI *Submit*. See section 5. |
| Interact (X), Pause (Start) | ❌ | No actions. |
| Keyboard/mouse | 🟡 | Movement works. Aim is bound to `<Pointer>/delta` (mouse *movement*, not direction to the cursor). |
| Enemy spawns aligned to beats/phrases | ❌ | One enemy per spawn point when gameplay starts. |
| Scoring, time bonus, completion scene | ❌ | `AttackResolved`/`DamageApplied` are published and ready to be scored. |
| Dialogue with portraits | 🟡 | `DialogManager` shows a linear JSON list; disabled in `Test0` by `enableDialogManager`. No border/text FX. |
| Managers: Input, Time, Level | ✅ | — |
| Managers: Audio, UI, Menu, Pool | ❌ | — |
| EventBus events `BeatTick`, `ComboChanged` | ✅ *(new)* | — |
| EventBus events `GameStateChanged`, `SpawnWave`, `DialogueStarted`, `EntityDied` | ❌ | Only `PlayerDiedEvent`; enemy deaths publish nothing. |
| BaseManager lifecycle contract | 🟡 | Code: Configure → Initialize → BindEvents → StartRuntime → StopRuntime → UnbindEvents. GDD also names Pause/Resume/Teardown; they were never called and were removed. Update the GDD or add them with the FSM. |
| Entities composition-first (Health, Hit/Hurtboxes, AIController…) | 🟡 | `Entity` owns health/state; player is split into Mover/AttackController/Weapon/Animator. Enemy AI lives inside `Enemy`. |
| ScriptableObject data: Beat/Combat/Movement | ✅ | — |
| ScriptableObject data: CharacterConfig, AttackPattern, LevelConfig, UITheme | ❌ | — |

---

## 5. Things only you can fix

These need the Editor or a design decision; I did not touch them.

1. **`Packages/manifest.json` is not in the repository** (the whole `Packages/` folder
   never was). A fresh clone has no record that the project needs Input System and
   TextMesh Pro, so it will not compile for anyone else — or for you on a new machine.
   Commit `Packages/manifest.json`. Unity also recommends committing
   `Packages/packages-lock.json`; `.gitignore` line 30 currently excludes it.
2. **Audio is git-ignored** (`*.mp3`, `*.wav`, `*.ogg`…). `metronome-85688.mp3.meta` is
   tracked but the clip is not, so `TimeManager.beatSound` is a missing reference in a
   clone. For a rhythm game the music is source material: track audio with Git LFS.
3. **`LevelManager.playerPrefab` in `Test0` points at the Player *in the scene*, not at
   `Assets/Prefab/Player.prefab`.** It is harmless today because the scene player always
   exists, but spawning from it would clone a live scene object. Assign the prefab asset.
4. **Attack button: GDD says A, the game uses RB.** With aim on the right stick, RB is
   the better choice (the thumb cannot aim and press A at once). I recommend changing the
   GDD, not the binding — your call.
5. **`AnimationHelper` is not in any scene.** The floating texts therefore create a new
   GameObject each time and draw with `OnGUI`. See next section, item 6.
6. **`Test_DialogManager.unity`** references script GUIDs that do not exist in this
   project (its GameManager/InputManager/DialogManager/TimeManager show as missing
   scripts). Re-link or delete the scene.

---

## 6. Next commits, in suggested order

Each is sized as one pull request.

1. **`feat(flow)`: real game-state machine.** Implement the GDD flow as states with
   `Enter/Exit`, publish `GameStateChangedEvent`, add the Pause action. Start small:
   `Gameplay`, `Paused`, `GameOver`, `Dialogue`. The death screen becomes the `GameOver`
   state instead of living in `LevelManager`.
2. **`feat(audio)`: Conductor + music.** Play the song with `AudioSource.PlayScheduled`
   and derive the beat grid from the scheduled start time (`songPosition = dspTime −
   songStartDsp − offset`), as in the conductor pattern [3][4]. Then: sub-beat / bar /
   phrase events, the metronome click scheduled instead of `PlayOneShot`, a calibration
   offset, and input timestamps taken from `InputAction.CallbackContext.time` rather than
   reading `dspTime` inside the callback (dspTime only advances once per audio buffer).
3. **`feat(combat)`: `AttackPattern` ScriptableObject + per-character strategy.** One
   asset describes shape, damage, window, and which beats count (on-beat for Vincent,
   off-beat for Valerie, two-bar pattern for Vin). `PlayerAttackController` reads the
   pattern instead of hard-coding Vincent's rules. This is the Open/Closed step: a new
   character is a new asset, not an edit. Includes the fire-cone hitbox and Valerie's
   "can aim during strong" flag.
4. **`refactor(damage)`: `DamageInfo` struct + `EntityDiedEvent`.** `Enemy.TakeDamage`
   currently decides "was this a strong hit?" by `amount >= GameConstants.STRONG_DAMAGE`,
   which breaks as soon as `CombatConfig` changes. Pass `DamageInfo {amount, isStrong,
   onBeat, attacker}`. Publish `EntityDiedEvent` from `Entity.Die`.
5. **`feat(score)`: `ScoreManager`** subscribing to `AttackResolved`, `DamageApplied`,
   `ComboChangedEvent`, `EntityDiedEvent`. No existing class needs to change.
6. **`perf(feedback)`: pooled TextMeshPro world-space text** replacing `OnGUI`
   (`OnGUI` runs several times per frame per text). Put `AnimationHelper` in the scene.
7. **`feat(enemy)`: `EnemyAI` component, wall collisions, beat-aligned waves.** Enemies
   are kinematic triggers and walk through walls and each other. Corpses are never
   removed. Spawns should come from a `LevelConfig` keyed by bar.
8. **`refactor(config)`: configs are mandatory.** Delete the `GameConstants` copies of
   config values and the `config != null ? … : fallback` branches; fail loudly when a
   config is missing. Replace the remaining `?.`/`??` on Unity objects
   (`PlayerMover2D`'s seven `movementConfig?.x ?? …` properties are the main case) [5].
9. **`fix(animation)`**: attack animation direction comes from movement velocity, the
   weapon from the aim stick — they can face different ways. Hit clips loop and last
   0.53 s but the lock is 0.25 s.
10. **`fix(input)`**: mouse aim (direction from player to cursor in world space) and the
    missing Interact/Pause actions.
11. **`chore(dialog)`**: `DialogManager` uses raw `Debug.Log` and Spanish messages,
    starts the dialogue inside `Initialize`, and calls `Resources.Load` for every node.
12. **`test`**: an `.asmdef` for `Scripts/` plus EditMode tests for beat judgement and
    combo rules (move the pure maths out of the MonoBehaviours so it can be tested
    without a scene). This is the part of the game where a silent regression costs most.

---

## 7. SOLID: where the code stands

- **Single responsibility.** Improved: `Weapon` detects hits, `PlayerAttackController`
  times and judges, `PlayerMover2D` moves and aims, `Player` is health/death/visuals,
  `GameManager` bootstraps. Still mixed: `LevelManager` (spawning + death-screen UI),
  `ComponentHelper` (physics setup + UI factory), `Enemy` (entity + AI + attack),
  `AnimationHelper` (flashes + floating text + pool).
- **Open/closed.** Improved by events: a new reaction to a death, a beat or a hit is a new
  subscriber. Not yet for characters and attacks (item 3 above).
- **Liskov.** `Entity` subclasses behave consistently. Watch `Die()`: `Player` and
  `Entity` both guard `IsAlive`, and `Player` must call `base.Die()` first.
- **Interface segregation.** `BaseManager` is down to the six hooks that are called.
- **Dependency inversion.** Components depend on events and on references pushed in by
  `GameManager`, not on `GameManager.Instance`. Remaining concrete pulls:
  `Enemy` → `GameManager.Instance.MainPlayer`, `ComponentHelper.CreateFullScreenCanvas`
  → `FindFirstObjectByType<InputManager>()`.

---

## 8. References

1. Unity Manual 6000.1 — Event function execution order ("Unity raises the sceneLoaded
   event after OnEnable but before Start for all objects in the scene"):
   https://docs.unity3d.com/6000.1/Documentation/Manual/execution-order.html
2. Unity Scripting API — `SceneManager.sceneLoaded` (example: Awake → OnEnable →
   OnSceneLoaded → Start):
   https://docs.unity3d.com/ScriptReference/SceneManagement.SceneManager-sceneLoaded.html
3. Graham Tattersall, "Coding to the Beat — Under the Hood of a Rhythm Game in Unity"
   (Conductor class on `AudioSettings.dspTime`):
   https://www.gamedeveloper.com/audio/coding-to-the-beat---under-the-hood-of-a-rhythm-game-in-unity
4. Yu Chao, "Music Syncing in Rhythm Games":
   https://gamedeveloper.com/programming/music-syncing-in-rhythm-games
5. JetBrains Rider/ReSharper Unity inspections — null-coalescing / null-propagation on
   `UnityEngine.Object` bypass the lifetime check:
   https://github.com/JetBrains/resharper-unity/wiki/Possible-unintended-bypass-of-lifetime-check-of-underlying-Unity-engine-object
6. Unity Scripting API 6000.1 — `Rigidbody2D.MovePosition` ("the actual position change
   will only occur during the next physics update … calling this method repeatedly
   without waiting for the next physics update will result in the last call being used"):
   https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Rigidbody2D.MovePosition.html

Research note: I looked for comparable open Unity rhythm-action projects to benchmark
against. The conductor pattern in [3]/[4] is the common base of the ones I found; I did
not find one I could confirm runs on 6000.1 with meaningful player feedback, so the
recommendations above rest on Unity's own documentation and those two articles rather
than on a specific reference project.
