# Valerie's Journey

## Overview

Valerie's Journey is a 2D top-down pixel‑art shooter dungeon crawler, emphasizing rhythmic combat mechanics, narrative depth, and immersive aesthetics. Inspired by **Hotline Miami** and **Hades**, the project combines intense action, character‑driven storytelling, and musical interaction.

## Technical Specifications

* **Engine:** Unity 2023 LTS (recommended)
* **Programming Language:** C#
* **Art Style:** Pixel Art (gameplay) + High‑Fidelity Illustrations (narrative)
* **Input:** Xbox gamepad via Unity **New Input System** (keyboard/mouse support planned)
* **Platforms:** PC (initial target)

## Game Flow (Session FSM)

1. **Main Screen** – splash/logo; press any button
2. **Menu Selector** – start/continue/options
3. **Level Preload** – additive load of level scene; pre‑warm pools/shaders/audio
4. **Cinematic** *(optional)* – skippable; input gated
5. **Dialogue Scene** – portraits, borders, text FX; can be beat‑aware
6. **Level Load (Gameplay)** – start music; enable rhythm signals; spawn loop
7. **Completion Scene** – results, scoring, bonuses (clear before song end)
8. **Exit or Continue** – next level / retry / back to menu

## Gameplay Concept

A rhythm‑driven, top‑down action crawler. Players eliminate devilspawn foes with timed attacks, chaining rhythmic combos to unleash powerful specials. Precision and musicality are rewarded.

## Core Features

### Musical Mechanics

* Attacks synchronized with song beats (4/4 baseline for Episode 0)
* Quantized input windows (on‑beat, off‑beat/contratempo)
* Rhythm combos feed **strong attacks** and scoring multipliers

### Strong Narrative

* Expressive dialogue with high‑fidelity character art (portrait/emotion variants)
* Dialogue boxes adapt border color, style, and text animation to character and mind state

### Influences

* **Hotline Miami:** fast, stylish action and strong audiovisual identity
* **Hades:** reactive dialogue system and expressive UI/typography

## Episode 0 – Proof of Concept

* Single playable scene for core mechanics testing
* Enemy spawns aligned to beat/phrases
* Level length = one song; clearing early grants **time bonus**
* Scoring: points for slaying enemies and for completing combos without taking damage

## Combat & Controls

* **Movement:** Left Stick
* **Aiming:** Right Stick
* **Basic Attack:** **A** (South button) — steerable mid‑animation
* **Strong Attack:** **Contextual** (earned via rhythm/combos); not a separate button
* **Interact:** **X**
* **Pause:** **Start**

**Targeting rules**

* Basic attacks can be aimed mid‑animation
* Strong attacks **cannot** be redirected after trigger
* **Exception:** *Valerie* can aim during her strong attack

## Playable Characters

Three unique playable characters are planned, each with distinct mechanics and narrative arcs. The prototype focuses on:

### Vincent

* **Personality:** Brave and passionate defender of justice and love
* **Basic:** Sword slash (A), steerable mid‑animation
* **Strong:** Triggers on the **4th consecutive on‑beat** basic; performs an AOE fire‑cone; direction is **locked at trigger**
* **Timing pattern:** `b – b – b – s`

### Valerie

* **Personality:** Repressed potential; insecure and sad but determined
* **Basic:** Shoots tears **a contratempo** (off‑beats)
* **Strong:** Forward rectangular torrent of magic tears on **beat 4**; **can aim during strong**
* **Timing pattern:** `– b – b – b s` (4/4: off‑beats for first 3 basics, strong on the 4th)

### Vin

* **Personality:** Aggressive, violent; funny in a rude way
* **Basic:** Slow melee with **knockback**; lasts **1 beat**
* **Strong:** **2‑beat spin** AOE; can move while spinning
* **Timing pattern (two bars of 4/4):** Bar 1 → `b on 1 & 3`; Bar 2 → `b on 1`, then `s spanning beats 3–4`

## Rhythm & Timing

* **Beat Source:** Single authority via audio DSP clock
* **Quantization Windows:** configurable (e.g., ±30ms / ±60ms / ±90ms)
* **Latency Calibration:** optional tap‑test stores per‑device offsets
* **Events:** `OnSubBeat`, `OnBeat`, `OnBar`, `OnPhrase`

## Software Architecture (High‑Level)

* **GameManager** – orchestrator & finite state machine implementing the Game Flow above
* **Managers (services):**

  * **InputManager** – New Input System wrapper (movement/aim/actions; timestamps for rhythm)
  * **TimeManager (Rhythm/Conductor)** – BPM, beat grid, quantization, combo validation, beat events
  * **AudioManager** – music playback (DSP), SFX buses, beat‑aligned cues
  * **LevelManager** – additive scene loading, pre‑warm, spawns & objectives
  * **UIManager** – HUD, prompts, tutorial hints
  * **DialogueManager** – boxes, portraits, borders & text FX
  * **MenuManager** – main/pause/options; routes to GameManager transitions
  * **(Optional) PoolManager** – pooled bullets, VFX, enemies
* **EventSystem (EventBus/Signals):** decouples systems (`GameStateChanged`, `BeatTick`, `SpawnWave`, `DialogueStarted`, `EntityDied`, `ComboChanged`)
* **BaseManager (abstract):** shared lifecycle contract (`Configure → Initialize → BindEvents → StartRuntime → Pause/Resume → Teardown`); not instantiated directly
* **Entities (composition‑first):**

  * **Entity (base):** Movement2D, Health, Hit/Hurtboxes, Status, Team, Signal relay
  * **Player : Entity** (Vincent, Valerie, Vin) – shared locomotion + character‑specific AttackController & dialogue hooks
  * **Enemy : Entity** – AIController + AttackController (optionally rhythm‑aware)
  * **NPC : Entity** – interaction + dialogue triggers

## Data & Authoring (ScriptableObjects)

* **Beatmap/ConductorConfig** – BPM, time signature, quantization, latency offsets
* **CharacterConfig** – movement, portraits/emotions, UI theme
* **AttackPattern** – shapes, damage, startup/recovery, beat windows, contratempos
* **LevelConfig** – song reference, spawn sets by bar/section, objective rules, bonuses
* **EventAssets** – Dialogue/Cinematic/Gameplay events with payloads
* **UITheme** – border color/type and text animation curves per character & mind state

## Defensive Coding / Null-Safety

Runtime scenes are frequently rearranged during prototyping, so systems now follow a few defensive guidelines:

1. **Fail fast with logs** – Whenever a required component is missing (Animator, Rigidbody2D, SpriteRenderer, etc.) the script emits a `DebugHelper.LogWarning` and disables itself instead of throwing null exceptions (`PlayerAnimator`, `BeatVisualizer`).
2. **Optional references use null-propagation** – Manager lookups and EventBus publishes are guarded with `?.` or explicit null checks before invoking events (`LevelManager`, `Weapon`).
3. **Graceful feature degradation** – Visual helpers (weapon flashes, beat bars) skip their effects if the renderer/UI references are absent but continue gameplay so QA can keep testing other systems.

Whenever you add a MonoBehaviour that depends on scene references, mirror this pattern: `Find`/inject the reference, validate it up front, then log and disable or fallback before Update logic runs.

## Project Structure (proposed)

```
Assets/
  Art/  Audio/  Fonts/  Prefabs/  Scenes/  Shaders/
  Scripts/
    Core/        (BaseManager, GameManager, EventBus)
    Managers/    (Input, Time, Audio, Level, UI, Menu, Pool)
    Entities/    (Entity, Player variants, Enemy, NPC, components)
    Gameplay/    (Combat, Spawning, Scoring)
    UI/          (HUD, Dialogue, Widgets)
  ScriptableObjects/
    Characters/  Attacks/  Levels/  Events/  UIThemes/  Rhythm/
```

## Quick Start (Unity + Xbox New Input System)

1. **Create Project:** Unity 2023 LTS → 2D template. Install **Input System**, **2D Pixel Perfect**, **TextMesh Pro** (Addressables optional). Project Settings → **Active Input Handling = Input System** (restart).
2. **Sprites & Camera:** Import as **Point (no filter)**, Compression **None**. Add **Pixel Perfect Camera**; set PPU to art grid.
3. **Scenes:** `App_Boot` (managers/EventBus), `UI_Global` (HUD & dialogue), `EP0_Level` (graybox). Start with `App_Boot` and load others **additively**.
4. **Input Actions:** Create asset with `Move(LS)`, `Aim(RS)`, `Basic(A)`, `Interact(X)`, `Pause(Start)`. Use **PlayerInput** and route to **InputManager**.
5. **Preload Handshake:** LevelManager pre‑warms pools/materials/fonts; AudioManager queues song; TimeManager locks to DSP; signal **ReadyToStart** → enter PreGame/Cinematic/Dialogue/Gameplay per flow.

## Roadmap (placeholder)

* Episode 0 vertical slice → rhythm calibration → dialogue polish → content expansion

## License & Credits

MIT license.

---

This README is a living **GDD placeholder** and will evolve alongside the project.
