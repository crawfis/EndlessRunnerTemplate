# Event Catalog

A checked-in reference of every event in the template, its numeric value, and how events
flow between domains. This is a snapshot for browsing; the source of truth is the enum files,
and the `/list-events` skill regenerates this on demand.

**Domain registry** (mirrored from CLAUDE.md's Architecture Overview — update both together):

| Domain | Enum (bus alias) | Purpose | Flow / bridge hosting (lifetime) | Bridges |
|--------|------------------|---------|----------------------------------|---------|
| **GameFlow** | `GameFlowEvents` (`GameFlowBus`) | App/session lifecycle: loading, menus, level select, pause, config/difficulty, quit | `GameFlowAutoEventFlow` in `0_BootStrap_Game_Only` (whole app) | ↔ TempleRun via `TempleRunGameFlowBridge` (hosted in `Game_Boot_2_Play`) |
| **TempleRun** | `TempleRunEvents` (`TempleRunBus`) | Gameplay: player lifecycle, countdown, movement, collisions, coins/power-ups, track/spline generation, teleportation | `TempleRunAutoEventFlow` in `TempleRunGameplay` (one run) | ↔ GameFlow via `TempleRunGameFlowBridge`; ← UserInitiated via `Input2TempleRunAutoEventBridge` |
| **UserInitiated** | `UserInitiatedEvents` (`UserInputBus`) | Raw input requests: turns, lanes, jump, slide, dash, pause toggle, quit | none (input never auto-chains) | → TempleRun via `Input2TempleRunAutoEventBridge` (hosted in `TempleRunGameplay`) |

Enum files: `Assets/GameFlow/Scripts/Events/GameFlowEvents.cs`;
`Assets/TempleRun/Scripts/Events/TempleRunEvents.cs` and `UserInitiatedEvents.cs`.
The placement convention is `Assets/*/Scripts/Events/*Events.cs` — every domain enum
matches that glob and carries `[EventEnum]`.

Naming convention: `*Requested` (a request) → `*ing`/`*Starting` (in progress) →
`*ed`/`*Started` (done); `*Failed` / `*Cancelled` for the off-nominal paths.

---

## GameFlowEvents (application lifecycle)

Publisher: `GameFlowBus` (`EventsFor<GameFlowEvents>`). Values are grouped by category with gaps of 10.

| Category | Members (value) |
|----------|-----------------|
| Loading Screen | `LoadingScreenShowRequested`(0), `LoadingScreenShowing`(1), `LoadingScreenShown`(2), `LoadingScreenHideRequested`(3), `LoadingScreenHiding`(4), `LoadingScreenHidden`(5) |
| Main Menu | `MainMenuShowRequested`(10), `MainMenuShowing`(11), `MainMenuShown`(12), `MainMenuHideRequested`(13), `MainMenuHiding`(14), `MainMenuHidden`(15) |
| Game Session | `GameStartRequested`(20), `GameStarting`(21), `GameStarted`(22), `GameEndRequested`(23), `GameEnding`(24), `GameEnded`(25), `RestartRequested`(26), `ReturnToMainMenuRequested`(27) |
| Scenes | `GameScenesUnloadRequested`(30), `GameScenesUnloading`(31), `GameScenesUnloaded`(32), `GameScenesUnloadFailed`(33), `GameScenesLoadRequested`(34), `GameScenesLoading`(35), `GameScenesLoaded`(36), `GameScenesLoadFailed`(37), `GameScenesActivating`(38), `GameScenesActivated`(39) |
| Gameplay Lifecycle | `GameplayPreparing`(50), `GameplayReady`(51), `GameplayNotReady`(52), `GameplayStarting`(53), `GameplayStarted`(54), `GameplayEnding`(55), `GameplayEnded`(56) |
| Pause | `PauseRequested`(60), `Pausing`(61), `Paused`(62), `ResumeRequested`(63), `Resuming`(64), `Resumed`(65) |
| Config / Difficulty | `GameConfigChangeRequested`(80), `GameConfigApplying`(81), `GameConfigApplied`(82) *(data: DifficultyConfig)*, `GameConfigApplyFailed`(83), `LevelApplied`(85) *(data: int selected level number)*, `DifficultyChangeRequested`(90), `DifficultyChanging`(91), `DifficultyChanged`(92), `DifficultyChangeFailed`(93), `DifficultySettingsApplied`(94) *(data: IList&lt;DifficultyConfig&gt;)* |
| Save / Load *(hooks; auto-chains commented out by default)* | `SaveLoadRequested`(100), `SaveLoading`(101), `SaveLoaded`(102), `SaveLoadFailed`(103), `SaveRequested`(110), `Saving`(111), `Saved`(112), `SaveFailed`(113) |
| Quit | `QuitRequested`(120), `Quitting`(121), `QuitCancelled`(122), `QuitCompleted`(123) |
| Level Selector | `LevelSelectorShowRequested`(130), `LevelSelectorShowing`(131), `LevelSelectorShown`(132), `LevelSelectorHideRequested`(133), `LevelSelectorHiding`(134), `LevelSelectorHidden`(135), `LevelSelected`(136) *(data: LevelConfig)*, `LevelUnlocked`(137) *(data: LevelConfig)*, `LevelProgressSaved`(138) |

### Scope note: the level selector is one flow, not the flow

The nine Level Selector events cover exactly what this template does — show the screen,
pick a level, remember what's unlocked. That is deliberately the *shape* of a selector, not
a finished one. A level-select screen is where game-specific product logic accumulates
faster than anywhere else in a session, and almost none of it is portable between games:
stars and medals, best scores and ghosts, previews, world maps, daily challenges, currency
gates, IAP or DLC downloads, leaderboards, cosmetics.

Two gaps are worth knowing about before you build on it, because both are *silent* today:

- **No browse/highlight event.** `LevelSelected`(136) is a *commit* — it carries a
  `LevelConfig` and the level starts loading. There is no event for "the highlight moved to
  level 4," so a preview pane, a star display, or a best-score readout has nothing to
  subscribe to. Any real selector needs that split (`LevelHighlighted` and friends).
- **No rejection path.** Tapping a locked level publishes *nothing*.
  `LevelSelectorController` attaches the click handler only when the level is unlocked and
  otherwise adds a `level-card--locked` class and a requirement label, so the refusal never
  leaves the UI script. Nothing can play a denied sound, animate a shake, nudge the player
  toward the unlock requirement, or count the attempt. (Unlock *state* is read the same
  way — a direct `LevelProgressManager.Instance.IsLevelUnlocked` call. That is legal, both
  are GameFlow, but it means progression is queried, never announced.)

These are left open on purpose. Designing the vocabulary — what a selector's real states
are, which transitions anyone outside the screen could care about, and which names earn
their keep — is a better exercise than inheriting someone else's answer, so the 130-series
has room and no opinions. **E13** in [STUDENT_TASKS.md](STUDENT_TASKS.md) is that exercise
(and a good one to work through with an AI acting as architect); **E9** (locked levels,
unlock criteria, star ratings) and **E10** (world-map level select) both build on it.

## TempleRunEvents (gameplay)

Publisher: `TempleRunBus` (`EventsFor<TempleRunEvents>`).

| Category | Members (value) |
|----------|-----------------|
| Player lifecycle | `PlayerFailRequested`(0), `PlayerFailing`(1), `PlayerFailed`(2), `PlayerDeathRequested`(3), `PlayerDying`(4), `PlayerDied`(5), `PlayerReviveRequested`(6), `PlayerReviving`(7), `PlayerRevived`(8), `PlayerFailingAtTurn`(12), `PlayerFailingAtObstacle`(13) |
| Pause / Resume | `PlayerPauseRequested`(20), `PlayerPausing`(21), `PlayerPaused`(22), `PlayerResumeRequested`(23), `PlayerResuming`(24), `PlayerResumed`(25), `PlayerPauseToggleRequested`(26) *(bridged from `UserPauseToggle`; `PauseController` resolves it against current state)* |
| Countdown | `CountdownStartRequested`(30), `CountdownStarting`(31), `CountdownStarted`(32), `CountdownTick`(33), `CountdownEnding`(34), `CountdownEnded`(35), `CountdownCancelled`(36) |
| Game lifecycle | `TempleRunStartRequested`(38), `TempleRunStarting`(39), `TempleRunStarted`(40), `TempleRunEndRequested`(41), `TempleRunEnding`(42), `TempleRunEnded`(43) |
| Turning | `TurnLeftRequested`(50), `TurnLeftStarting`(51), `TurnLeftStarted`(52), `TurnLeftEnding`(53), `TurnLeftEnded`(54), `TurnRightRequested`(55), `TurnRightStarting`(56), `TurnRightStarted`(57), `TurnRightEnding`(58), `TurnRightEnded`(59) |
| Slide | `SlideRequested`(60), `SlideStarting`(61), `SlideStarted`(62), `SlideEndRequested`(63), `SlideEnding`(64), `SlideEnded`(65) |
| Dash | `DashRequested`(70), `DashStarting`(71), `DashStarted`(72), `DashEnding`(73), `DashEnded`(74) |
| Jump | `JumpRequested`(80), `JumpStarting`(81), `JumpStarted`(82), `JumpEndRequested`(83), `JumpEnding`(84), `JumpEnded`(85) |
| Lane change | `LaneChangeLeftRequested`(100), `LaneChangingLeft`(101), `LaneChangedLeft`(102), `LaneChangeRightRequested`(103), `LaneChangingRight`(104), `LaneChangedRight`(105), `LaneChangeLeftFailed`(106), `LaneChangeRightFailed`(107) |
| Hazards | `ObstacleHit`(120), `ObstacleRecoveryRequested`(121), `ObstacleRecovering`(122), `ObstacleRecovered`(123) |
| Coins | `CoinCollectRequested`(140), `CoinCollecting`(141), `CoinCollected`(142) |
| Power-up collect | `PowerUpCollectRequested`(160), `PowerUpCollecting`(161), `PowerUpCollected`(162) |
| Power-up activate | `PowerUpActivateRequested`(180), `PowerUpActivating`(181), `PowerUpActivated`(182), `PowerUpDeactivateRequested`(183), `PowerUpDeactivating`(184), `PowerUpDeactivated`(185) |
| Splines | `SplineSegmentCreateRequested`(200), `SplineSegmentCreating`(201), `SplineSegmentCreated`(202) *(data: SplineSegmentData)*, `SplineSegmentReleaseRequested`(203), `SplineSegmentReleasing`(204), `SplineSegmentReleased`(205), `CurrentSplineChangeRequested`(220), `CurrentSplineChanging`(221), `CurrentSplineChanged`(222) |
| Track segments | `TrackSegmentCreateRequested`(240), `TrackSegmentCreating`(241), `TrackSegmentCreated`(242) *(data: TrackSegmentInfo)*, `TrackSegmentRecycleRequested`(243), `TrackSegmentRecycling`(244), `TrackSegmentRecycled`(245), `ActiveTrackChangeRequested`(260), `ActiveTrackChanging`(261) *(data: TrackSegmentInfo)*, `ActiveTrackChanged`(262) *(data: TrackSegmentInfo)* |
| Teleport | `TeleportRequested`(280), `TeleportStarting`(281), `TeleportStarted`(282), `TeleportEndRequested`(283), `TeleportEnding`(284), `TeleportEnded`(285) |
| Bridged from GameFlow | `TempleRunConfigApplied`(300) *(data: DifficultyConfig)*, `TempleRunScenesReady`(302), `TempleRunLevelApplied`(304) *(data: int selected level number; **sticky** — replayed to late subscribers)* |
| Difficulty (bridged) | `TempleRunDifficultySettingsApplied`(310) *(data: IList&lt;DifficultyConfig&gt;; **sticky**)*, `TempleRunDifficultyChanging`(312) *(data: DifficultyConfig)*, `TempleRunDifficultyChanged`(314) *(data: DifficultyConfig)*, `TempleRunDifficultyChangeFailed`(316), `TempleRunDifficultyChangeRequested`(318) *(data: string difficulty name)* |
| Difficulty (direct) | `DifficultySettingsApplied`(320), `DifficultyChanging`(321), `DifficultyChanged`(322), `DifficultyChangeFailed`(323) *(data: DifficultyConfig)* |
| Distance | `DistanceUpdated`(330) *(data: float distance travelled)* |
| Segment lifecycle | `SegmentRequested`(340) *(data: Direction — the direction committed at an Either junction; published by `SegmentCommitController`)*, `SegmentEntering`(342), `SegmentEntered`(343), `SegmentExiting`(344), `SegmentExited`(345) *(all data: TrackSegmentInfo)* |
| Segment geometry | `SegmentGeometryReady`(350) *(data: SegmentGeometryData)* |

## UserInitiatedEvents (raw input)

Publisher: `UserInputBus` (`EventsFor<UserInitiatedEvents>`). Implicit values 0–8.

`UserLeftTurnRequested`(0), `UserRightTurnRequested`(1), `UserPauseToggle`(2),
`UserLeftLaneChangeRequested`(3), `UserRightLaneChangeRequested`(4), `UserJumpRequested`(5),
`UserQuitRequested`(6), `UserSlideRequested`(7), `UserDashRequested`(8)

---

## Auto-chains (same-domain, fire automatically)

### GameFlow → GameFlow (`GameFlowAutoEventFlow.cs`)
```
LoadingScreenShowRequested   → LoadingScreenShowing
LoadingScreenHideRequested   → LoadingScreenHiding
MainMenuShowRequested        → MainMenuShowing
MainMenuHideRequested        → MainMenuHiding
LevelSelectorShowRequested   → LevelSelectorShowing
LevelSelectorHideRequested   → LevelSelectorHiding
GameStartRequested           → GameStarting
GameScenesLoadRequested      → GameScenesLoading
GameScenesUnloadRequested    → GameScenesUnloading
GameConfigChangeRequested    → GameConfigApplying
DifficultyChangeRequested    → DifficultyChanging
PauseRequested               → Pausing → Paused
ResumeRequested              → Resuming → Resumed
LoadingScreenHidden          → GameplayReady             (boot complete)
GameplayReady                → MainMenuShowRequested     (boot → menu)
LevelSelected                → GameScenesLoadRequested   (level chosen → load)
GameScenesLoaded             → GameStartRequested        (loaded → start)
GameEnding                   → GameScenesUnloadRequested (death → unload)
GameEnded                    → GameplayReady             (post-game → back to menu)
```
*Commented out by default: the Save/Load chain and `QuitRequested → Quitting`.*

### TempleRun → TempleRun (`TempleRunAutoEventFlow.cs`)
```
PlayerPauseRequested       → PlayerPausing → PlayerPaused
PlayerResumeRequested      → PlayerResuming → PlayerResumed
PlayerFailingAtTurn        → PlayerFailing   (two sources fanning into one target;
PlayerFailingAtObstacle    → PlayerFailing    PlayerFailed is published by PlayerFailedController)
CountdownStartRequested    → CountdownStarting
TempleRunStartRequested    → TempleRunStarting → TempleRunStarted
PlayerDied                 → TempleRunEndRequested → TempleRunEnding → TempleRunEnded
CoinCollectRequested       → CoinCollecting
PowerUpCollectRequested    → PowerUpCollecting
PowerUpCollected           → PowerUpActivateRequested → PowerUpActivating
PowerUpDeactivateRequested → PowerUpDeactivating
```

**Validation gates — why no player-movement `*Requested` is auto-chained.** Every movement
`*Requested` event is `Input2TempleRunAutoEventBridge`'s *raw* translation of user input, so it
fires whether or not the action is currently legal. An auto-chain runs before any controller
validates, so chaining `*Requested → *Starting` silently defeats the gate. Each controller
publishes its own `*Starting` once its checks pass:

| Event | Gate | Published by |
|-------|------|--------------|
| `JumpStarting` | not already airborne | `JumpController` |
| `SlideStarting` | not sliding, cooldown elapsed | `SlideController` |
| `DashStarting` | not dashing, cooldown elapsed | `DashController` |
| `LaneChangingLeft` / `Right` | lane boundary, none in flight | `LaneChangeController` |
| `TurnLeftStarting` / `TurnRightStarting` | direction matches, inside turn window | `TurnController` |

`Turn*Starting → Turn*Started` is unchained for a different reason: `SegmentCommitController`
subscribes to `Turn*Starting`, and a chain target and a subscriber of the same event have no
defined order between them. It publishes `Turn*Started` itself, then commits an Either junction
with `SegmentRequested`, then publishes `Turn*Ending` — an order the geometry depends on.

`ObstacleHit → PlayerFailingAtObstacle` is also deliberately unchained, gated by
`PowerUpBuffController` so a Shield can absorb the hit instead of failing.

---

## Cross-domain bridge (`TempleRunGameFlowBridge.cs`)

The **only** place TempleRun ↔ GameFlow event references are allowed. (The input crossing
has its own bridge — see the next section.)

### TempleRun → GameFlow
```
PlayerPaused    → PauseRequested
PlayerResumed   → ResumeRequested
CountdownEnded  → GameStarted
TempleRunEnded  → GameEnding
```
### GameFlow → TempleRun
```
GameStarted               → TempleRunStartRequested
GameStarting              → CountdownStartRequested
GameConfigApplied         → TempleRunConfigApplied
LevelApplied              → TempleRunLevelApplied
GameScenesLoaded          → TempleRunScenesReady
DifficultySettingsApplied → TempleRunDifficultySettingsApplied
```

There is an intentional cycle: `CountdownEnded → GameStarted` (TR→GF) and
`GameStarted → TempleRunStartRequested` (GF→TR). It terminates because
`TempleRunStarted` has no further mapping.

## Input bridge (`Input2TempleRunAutoEventBridge.cs`)

The **only** place in the codebase that may subscribe to `UserInitiatedEvents`. Every one of
the nine is bridged; no gameplay controller subscribes to raw input.

UserInitiated → TempleRun:
```
UserQuitRequested             → TempleRunEndRequested
UserSlideRequested            → SlideRequested
UserDashRequested             → DashRequested
UserJumpRequested             → JumpRequested
UserLeftTurnRequested         → TurnLeftRequested
UserRightTurnRequested        → TurnRightRequested
UserLeftLaneChangeRequested   → LaneChangeLeftRequested
UserRightLaneChangeRequested  → LaneChangeRightRequested
UserPauseToggle               → PlayerPauseToggleRequested
```

`UserInitiatedEvents` has a publish/subscribe asymmetry the other domains don't: **publishing
is open** (the `Scripts/Input/` action classes, `AIController`, and any future replay or
netcode driver), **subscribing is closed** (this bridge only). Open publishing is what lets an
autopilot stand in for a human; closed subscribing is what lets the whole input domain be
replaced by a source that speaks `TempleRunEvents` directly, without a controller caring.
