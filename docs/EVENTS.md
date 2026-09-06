# Event Catalog

A checked-in reference of every event in the template, its numeric value, and how events
flow between domains. This is a snapshot for browsing; the source of truth is the enum files,
and the `/list-events` skill regenerates this on demand.

**Domain registry** (mirrored from CLAUDE.md's Architecture Overview — update both together):

| Domain | Enum (bus alias) | Purpose | Flow / bridge hosting (lifetime) | Bridges |
|--------|------------------|---------|----------------------------------|---------|
| **GameFlow** | `GameFlowEvents` (`GameFlowBus`) | App/session lifecycle: loading, menus, level select, pause, config/difficulty, quit | `GameFlowAutoEventFlow` in `0_BootStrap_Game_Only` (whole app) | ↔ TempleRun via `TempleRunGameFlowBridge`; → Countdown via `CountdownGameFlowBridge` (both hosted in `Game_Boot_2_Play`) |
| **Countdown** | `CountdownEvents` (`CountdownBus`) | Session ceremony: the pre-run 3… 2… 1… and its overlay | `CountdownAutoEventFlow` in `Game_Boot_2_Play` (session) | ← GameFlow via `CountdownGameFlowBridge`; → TempleRun via `Countdown2TempleRunBridge` (hosted in `Game_Boot_2_Play`) |
| **TempleRun** | `TempleRunEvents` (`TempleRunBus`) | Gameplay: player lifecycle, movement, collisions, coins/power-ups, track/spline generation, teleportation | `TempleRunAutoEventFlow` in `TempleRunGameplay` (one run) | ↔ GameFlow via `TempleRunGameFlowBridge`; ← Countdown via `Countdown2TempleRunBridge`; ← UserInitiated via `Input2TempleRunAutoEventBridge` |
| **UserInitiated** | `UserInitiatedEvents` (`UserInputBus`) | Raw input requests: turns, lanes, jump, slide, dash, pause toggle, quit | none (input never auto-chains) | → TempleRun via `Input2TempleRunAutoEventBridge` (hosted in `TempleRunGameplay`) |

Enum files: `Assets/GameFlow/Scripts/Events/GameFlowEvents.cs`;
`Assets/Countdown/Scripts/Events/CountdownEvents.cs`;
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

## CountdownEvents (session ceremony)

Publisher: `CountdownBus` (`EventsFor<CountdownEvents>`). Implicit values 0–5.

| Category | Members (value) |
|----------|-----------------|
| Countdown | `CountdownStartRequested`(0), `CountdownStarting`(1), `CountdownStarted`(2), `CountdownTick`(3), `CountdownEnding`(4), `CountdownEnded`(5) |

The whole domain is two files — `CountdownController` (the timer) and
`CountdownUIController` (the overlay) — under `Assets/Countdown/`, next to
`UI Toolkit/Countdown.uxml`. It was extracted from `TempleRunEvents` (where the members
were values 30–36) so that neither GameFlow nor gameplay owns the ceremony; the old
`CountdownCancelled`(36) was dropped in the move because nothing published or subscribed to
it. Auto-chains: [Countdown → Countdown](#countdown--countdown-countdownautoeventflowcs).
Bridges in and out: [CountdownGameFlowBridge / Countdown2TempleRunBridge](#countdown-bridges-countdowngameflowbridgecs--countdown2templerunbridgecs).

The stub test the domain is designed to pass: delete the countdown objects, map
`GameStarting → PlayerActivateRequested` instead, and a run still starts. TempleRun cannot
tell whether a countdown, a cutscene, or nothing at all sat between
`TempleRunStartRequested` and `PlayerActivateRequested`.

## TempleRunEvents (gameplay)

Publisher: `TempleRunBus` (`EventsFor<TempleRunEvents>`).

| Category | Members (value) |
|----------|-----------------|
| Player lifecycle | `PlayerFailRequested`(0), `PlayerFailing`(1), `PlayerFailed`(2), `PlayerDeathRequested`(3), `PlayerDying`(4), `PlayerDied`(5), `PlayerReviveRequested`(6), `PlayerReviving`(7), `PlayerRevived`(8), `PlayerFailingAtTurn`(12), `PlayerFailingAtObstacle`(13), `PlayerActivateRequested`(14) *(bridged from `CountdownEnded`)*, `PlayerActivating`(15), `PlayerActivated`(16) |
| Pause / Resume | `PlayerPauseRequested`(20), `PlayerPausing`(21), `PlayerPaused`(22), `PlayerResumeRequested`(23), `PlayerResuming`(24), `PlayerResumed`(25), `PlayerPauseToggleRequested`(26) *(bridged from `UserPauseToggle`; `PauseController` resolves it against current state)* |
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
| Splines | `SplineSegmentCreateRequested`(200), `SplineSegmentCreating`(201), `SplineSegmentCreated`(202) *(data: SplineSegmentData)*, `SplineSegmentReleaseRequested`(203), `SplineSegmentReleasing`(204), `SplineSegmentReleased`(205), `CurrentSplineChangeRequested`(220), `CurrentSplineChanging`(221) *(data: SplineSection)*, `CurrentSplineChanged`(222) *(data: SplineSection)* |
| Track segments | `TrackSegmentCreateRequested`(240), `TrackSegmentCreating`(241), `TrackSegmentCreated`(242) *(data: TrackSegmentInfo)*, `TrackSegmentRecycleRequested`(243), `TrackSegmentRecycling`(244), `TrackSegmentRecycled`(245), `ActiveTrackChangeRequested`(260), `ActiveTrackChanging`(261) *(data: TrackSegmentInfo)*, `ActiveTrackChanged`(262) *(data: TrackSegmentInfo)* |
| Teleport | `TeleportRequested`(280), `TeleportStarting`(281) *(data: TeleportInfo)*, `TeleportStarted`(282) *(data: TeleportInfo)*, `TeleportEndRequested`(283), `TeleportEnding`(284) *(data: SplineSection)*, `TeleportEnded`(285) *(data: SplineSection)* |
| Bridged from GameFlow | `TempleRunConfigApplied`(300) *(data: DifficultyConfig)*, `RunInitializeRequested`(302), `TrackLevelApplied`(304) *(data: int selected level number; **sticky** — replayed to late subscribers, read by `TrackManager` via `TryGetLast`)* |
| Difficulty (bridged) | `TempleRunDifficultySettingsApplied`(310) *(data: IList&lt;DifficultyConfig&gt;; **sticky**)*, `TempleRunDifficultyChanging`(312) *(data: DifficultyConfig)*, `TempleRunDifficultyChanged`(314) *(data: DifficultyConfig)*, `TempleRunDifficultyChangeFailed`(316), `TempleRunDifficultyChangeRequested`(318) *(data: string difficulty name)* |
| Difficulty (direct) | `DifficultySettingsApplied`(320), `DifficultyChanging`(321), `DifficultyChanged`(322), `DifficultyChangeFailed`(323) *(data: DifficultyConfig)* |
| Distance | `DistanceUpdated`(330) *(data: float distance travelled)* |
| Segment lifecycle | `SegmentRequested`(340) *(data: Direction — the direction committed at an Either junction; published by `TurnCommitController`)*, `SegmentEntering`(342), `SegmentEntered`(343), `SegmentExiting`(344), `SegmentExited`(345) *(all data: TrackSegmentInfo)* |
| Segment geometry | `SegmentGeometryReady`(350) *(data: SegmentGeometryData)* |

> **`TrackSegmentInfo` carries run-absolute distances.** The struct behind every
> `TrackSegment*`, `ActiveTrack*` and `Segment*` payload holds the segment's
> `Definition` (whose distances are measured from the segment's own entrance), its resolved
> `Direction`, and `StartDistance` — the distance from the start of the run at which the
> segment begins, stamped once by `TrackManager` when the segment is created. `PivotDistance`,
> `TurnFailureDistance` and `EndDistance` are the definition's distances with that origin
> already added, so a subscriber compares them against `DistanceTracker.DistanceTravelled`
> directly. `Length` and `TeleportDistance` stay relative because they are lengths, not
> positions.
>
> This is the payload-carries-the-derived-value rule in
> [ADDING_A_MECHANIC](ADDING_A_MECHANIC.md#shared-derived-data-goes-on-the-payload-shared-decisions-get-one-owner):
> the conversion arrives on the message, so no subscriber performs it — and five that used to
> keep private running sums no longer can drift apart.

> **`SplineSection` names who writes the player's transform.** The path the player follows
> arrives one straight section at a time: an *approach*, entrance to pivot, always
> `Direction.Straight`; then, if the segment turns, an *exit* from the shifted pivot along the
> new heading. `SegmentTransitionController` is the only publisher of both.
>
> The section's `TeleportOwnsTransform` (`Direction != Straight`) is the load-bearing member.
> A turn's exit is teleported onto, so `TeleportController` starts a teleport and
> `CharacterTeleporter` lerps the player over its duration — which means
> `MoveCharacterByDistance` must re-anchor but *not* place the player, or the lerp runs from the
> destination to the destination and the move takes zero frames. Both components used to reach
> that conclusion privately, from a `Direction` slot in an unnamed four-element tuple. They now
> read one property.
>
> `LandingDistance` is run-absolute and meaningful only when `TeleportOwnsTransform` is true;
> `DistanceController` snaps the tracker to it on `TeleportEnded`. An approach carries no
> landing, because nothing teleports onto one. `TeleportStarting`/`TeleportStarted` wrap the
> section in a `TeleportInfo` that adds the duration; the terminal rungs carry the section
> alone. The turn ladder's own terminal rungs carry **nothing** — `Turn*Ending` used to forward
> the exit section, which no subscriber read and which put track vocabulary on a player event.

### Two starts: "systems up" and "player go"

The run has two distinct beginnings, and subscribing to the wrong one is the easiest
mistake in this domain:

- **`TempleRunStartRequested → TempleRunStarting → TempleRunStarted`** — *the run's systems
  are up*. Bridged from GameFlow's `GameStarted`, which now fires at the **start** of the
  ceremony, so these land while the countdown is still on screen. Music, lane
  initialisation, the life controller, and the segment-advance trigger start here on
  purpose (`SetMusicPlayer`, `PlayerLifeController`, `SegmentAdvanceTrigger`,
  `LaneChangeController`).
- **`PlayerActivateRequested → PlayerActivating → PlayerActivated`** — *the player is
  released*. Bridged from the Countdown domain's `CountdownEnded`. Anything that must not
  run under the countdown subscribes here: `GameTime` (the run clock), `DistanceController`,
  `AIController` (arming the autopilot), `TurnCollisionDetector` (arming failure detection),
  and `Metronome` (the beat paces the run — and its tick interval divides by
  `Blackboard.CurrentSpeed`, which is 0 before activation).

Both `PlayerActivate*` links are auto-chained, so today activation is instantaneous. A
spawn-in animation or a grace period breaks one link in the chain table and needs no
controller edit.

## UserInitiatedEvents (raw input)

Publisher: `UserInputBus` (`EventsFor<UserInitiatedEvents>`). Implicit values 0–8.

`UserLeftTurnRequested`(0), `UserRightTurnRequested`(1), `UserPauseToggle`(2),
`UserLeftLaneChangeRequested`(3), `UserRightLaneChangeRequested`(4), `UserJumpRequested`(5),
`UserQuitRequested`(6), `UserSlideRequested`(7), `UserDashRequested`(8)

> **Every member carries the player id (`int`), and nothing else.** It is the one fact an input
> request always has and a handler can never recover on its own. Three payload types used to
> share these nine members: most carried the id, `UserPauseToggle` and `UserQuitRequested`
> carried `UnityEngine.Time.time`, and `AIController` published the run distance on the same two
> turn events the input classes published the id on. None was read; none said *who* asked. The
> clock is deliberately not carried — any handler can read it.
>
> The bridge forwards a payload unchanged, so the id arrives on the TempleRun event too, and
> those eight mirrors (`TurnLeftRequested`(50), `TurnRightRequested`(55), `SlideRequested`(60),
> `DashRequested`(70), `JumpRequested`(80), `LaneChangeLeftRequested`(100),
> `LaneChangeRightRequested`(103), `PlayerPauseToggleRequested`(26)) declare it as well — the
> bridge is their only publisher. `TempleRunEndRequested`(41) is the exception and stays
> undeclared: the ChainTable also reaches it from `PlayerDied`, which carries the score.
>
> The template is single-player and every source publishes `0`. The declaration is what makes a
> second player a wiring change rather than a payload redesign.

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
GameStarting                 → GameStarted               (GameFlow owns its own milestone)
GameEnding                   → GameScenesUnloadRequested (death → unload)
GameEnded                    → GameplayReady             (post-game → back to menu)
```
*Commented out by default: the Save/Load chain and `QuitRequested → Quitting`.*

`GameStarting → GameStarted` used to be a round trip through gameplay
(`GameStarting → CountdownStartRequested … CountdownEnded → GameStarted`). Since the
Countdown domain was extracted, GameFlow chains its own milestone: the ceremony runs in
parallel off `GameStarting`, and its end releases the player rather than deciding when the
session started. `GameStarted` therefore now fires at ceremony *start* — the HUD appears
under the countdown overlay.

### Countdown → Countdown (`CountdownAutoEventFlow.cs`)
```
CountdownStartRequested → CountdownStarting
CountdownEnding         → CountdownEnded
```
`CountdownStarting → CountdownStarted` and `CountdownStarted → …Tick… → CountdownEnding`
are `CountdownController`'s: it runs the timer and publishes the ticks. `Ending → Ended` is
left chained as the seam a "GO!" flash or a start-line delay breaks.

### TempleRun → TempleRun (`TempleRunAutoEventFlow.cs`)
```
PlayerPauseRequested       → PlayerPausing → PlayerPaused
PlayerResumeRequested      → PlayerResuming → PlayerResumed
PlayerFailingAtTurn        → PlayerFailing   (two sources fanning into one target;
PlayerFailingAtObstacle    → PlayerFailing    PlayerFailed is published by PlayerFailedController)
PlayerActivateRequested    → PlayerActivating → PlayerActivated
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

`Turn*Starting → Turn*Started` is unchained for a different reason: `TurnCommitController`
subscribes to `Turn*Starting`, and a chain target and a subscriber of the same event have no
defined order between them. It commits an Either junction with `SegmentRequested` first, then
publishes `Turn*Started` itself.

`Turn*Started → Turn*Ending` is the turn's **duration**, and the teleport fills it:
`SegmentTransitionController` publishes the exit spline on `Turn*Started`,
`TeleportController` moves the player onto it and publishes `Turn*Ending` when that motion
lands. Only `Turn*Ending → Turn*Ended` is chained.

`ObstacleHit → PlayerFailingAtObstacle` is also deliberately unchained, gated by
`PowerUpBuffController` so a Shield can absorb the hit instead of failing.

---

## Cross-domain bridge (`TempleRunGameFlowBridge.cs`)

The **only** place TempleRun ↔ GameFlow event references are allowed. (The input and
countdown crossings have their own bridges — see the next two sections.)

### TempleRun → GameFlow
```
PlayerPaused    → PauseRequested
PlayerResumed   → ResumeRequested
TempleRunEnded  → GameEnding
```
### GameFlow → TempleRun
```
GameStarted               → TempleRunStartRequested
GameConfigApplied         → TempleRunConfigApplied
LevelApplied              → TrackLevelApplied
GameScenesLoaded          → RunInitializeRequested
DifficultySettingsApplied → TempleRunDifficultySettingsApplied
```

The one cycle this table used to contain — `CountdownEnded → GameStarted` (TR→GF) feeding
`GameStarted → TempleRunStartRequested` (GF→TR) — is gone: `GameStarted` is now chained
inside GameFlow, so nothing in gameplay decides a session milestone.

Note the vocabulary of the GameFlow → TempleRun rows: each target is named in *gameplay's*
words, not GameFlow's. `GameScenesLoaded` is a scene fact; what TempleRun does with it is
`RunInitializeRequested`. `LevelApplied` becomes `TrackLevelApplied` because its consumer
resolves the int through `TrackLevelRegistrySO`. A bridge mapping should read as a
translation, not as a relay of a foreign concept wearing a local badge — the two
`TempleRun*`-prefixed rows that remain are mid-migration leftovers, not the model.

## Countdown bridges (`CountdownGameFlowBridge.cs` / `Countdown2TempleRunBridge.cs`)

The countdown is its own domain, so it crosses two boundaries — one in, one out. Both
bridges are one-directional; both are hosted on the `CountdownDomain` object in
`Game_Boot_2_Play`.

### GameFlow → Countdown (`CountdownGameFlowBridge.cs`, under `Assets/GameFlow/Scripts/CountdownSpecific/`)
```
GameStarting → CountdownStartRequested
```
### Countdown → TempleRun (`Countdown2TempleRunBridge.cs`, under `Assets/Countdown/Scripts/TempleRunSpecific/`)
```
CountdownEnded → PlayerActivateRequested
```

Both rows are translations. A session milestone becomes a ceremony trigger; the ceremony's
end becomes, in gameplay's vocabulary, "release the player". The ceremony's outcome never
travels back to GameFlow.

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
