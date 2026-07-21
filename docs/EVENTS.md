# Event Catalog

A checked-in reference of every event in the template, its numeric value, and how events
flow between domains. This is a snapshot for browsing; the source of truth is the enum files,
and the `/list-events` skill regenerates this on demand.

- `GameFlowEvents` → `Assets/GameFlow/Scripts/Events/GameFlowEvents.cs`
- `TempleRunEvents` → `Assets/TempleRun/Scripts/Events/TempleRunEvents.cs`
- `UserInitiatedEvents` → `Assets/TempleRun/Scripts/Events/UserInitiatedEvents.cs`

Naming convention: `*Requested` (a request) → `*ing`/`*Starting` (in progress) →
`*ed`/`*Started` (done); `*Failed` / `*Cancelled` for the off-nominal paths.

---

## GameFlowEvents (application lifecycle)

Publisher: `EventsPublisherGameFlow.Instance`. Values are grouped by category with gaps of 10.

| Category | Members (value) |
|----------|-----------------|
| Loading Screen | `LoadingScreenShowRequested`(0), `LoadingScreenShowing`(1), `LoadingScreenShown`(2), `LoadingScreenHideRequested`(3), `LoadingScreenHiding`(4), `LoadingScreenHidden`(5) |
| Main Menu | `MainMenuShowRequested`(10), `MainMenuShowing`(11), `MainMenuShown`(12), `MainMenuHideRequested`(13), `MainMenuHiding`(14), `MainMenuHidden`(15) |
| Game Session | `GameStartRequested`(20), `GameStarting`(21), `GameStarted`(22), `GameEndRequested`(23), `GameEnding`(24), `GameEnded`(25), `RestartRequested`(26), `ReturnToMainMenuRequested`(27) |
| Scenes | `GameScenesUnloadRequested`(30), `GameScenesUnloading`(31), `GameScenesUnloaded`(32), `GameScenesUnloadFailed`(33), `GameScenesLoadRequested`(34), `GameScenesLoading`(35), `GameScenesLoaded`(36), `GameScenesLoadFailed`(37), `GameScenesActivating`(38), `GameScenesActivated`(39) |
| Gameplay Lifecycle | `GameplayPreparing`(50), `GameplayReady`(51), `GameplayNotReady`(52), `GameplayStarting`(53), `GameplayStarted`(54), `GameplayEnding`(55), `GameplayEnded`(56) |
| Pause | `PauseRequested`(60), `Pausing`(61), `Paused`(62), `ResumeRequested`(63), `Resuming`(64), `Resumed`(65) |
| Config / Difficulty | `GameConfigChangeRequested`(80), `GameConfigApplying`(81), `GameConfigApplied`(82), `GameConfigApplyFailed`(83), `TrackConfigApplied`(85) *(data: string Resources path)*, `DifficultyChangeRequested`(90), `DifficultyChanging`(91), `DifficultyChanged`(92), `DifficultyChangeFailed`(93), `DifficultySettingsApplied`(94) |
| Save / Load *(hooks; auto-chains commented out by default)* | `SaveLoadRequested`(100), `SaveLoading`(101), `SaveLoaded`(102), `SaveLoadFailed`(103), `SaveRequested`(110), `Saving`(111), `Saved`(112), `SaveFailed`(113) |
| Quit | `QuitRequested`(120), `Quitting`(121), `QuitCancelled`(122), `QuitCompleted`(123) |
| Level Selector | `LevelSelectorShowRequested`(130), `LevelSelectorShowing`(131), `LevelSelectorShown`(132), `LevelSelectorHideRequested`(133), `LevelSelectorHiding`(134), `LevelSelectorHidden`(135), `LevelSelected`(136) *(data: LevelConfig)*, `LevelUnlocked`(137) *(data: LevelConfig)*, `LevelProgressSaved`(138) |

## TempleRunEvents (gameplay)

Publisher: `EventsPublisherTempleRun.Instance`.

| Category | Members (value) |
|----------|-----------------|
| Player lifecycle | `PlayerFailRequested`(0), `PlayerFailing`(1), `PlayerFailed`(2), `PlayerDeathRequested`(3), `PlayerDying`(4), `PlayerDied`(5), `PlayerReviveRequested`(6), `PlayerReviving`(7), `PlayerRevived`(8), `PlayerFailingAtTurn`(12), `PlayerFailingAtObstacle`(13) |
| Pause / Resume | `PlayerPauseRequested`(20), `PlayerPausing`(21), `PlayerPaused`(22), `PlayerResumeRequested`(23), `PlayerResuming`(24), `PlayerResumed`(25) |
| Countdown | `CountdownStartRequested`(30), `CountdownStarting`(31), `CountdownStarted`(32), `CountdownTick`(33), `CountdownEnding`(34), `CountdownEnded`(35), `CountdownCancelled`(36) |
| Game lifecycle | `TempleRunStartRequested`(38), `TempleRunStarting`(39), `TempleRunStarted`(40), `TempleRunEndRequested`(41), `TempleRunEnding`(42), `TempleRunEnded`(43) |
| Turning | `TurnLeftRequested`(50), `TurnLeftStarting`(51), `TurnLeftCompleted`(52), `TurnRightRequested`(53), `TurnRightStarting`(54), `TurnRightCompleted`(55), `SegmentRequested`(56) *(data: Direction — chosen at an Either junction)* |
| Slide | `SlideRequested`(60), `SlideStarting`(61), `SlideStarted`(62), `SlideEndRequested`(63), `SlideEnding`(64), `SlideEnded`(65) |
| Dash | `DashRequested`(70), `DashStarting`(71), `DashStarted`(72), `DashEnding`(73), `DashEnded`(74) |
| Jump | `JumpRequested`(80), `JumpStarting`(81), `JumpStarted`(82), `JumpEndRequested`(83), `JumpEnding`(84), `JumpLanded`(85) |
| Lane change | `LaneChangeLeftRequested`(100), `LaneChangingLeft`(101), `LaneChangedLeft`(102), `LaneChangeRightRequested`(103), `LaneChangingRight`(104), `LaneChangedRight`(105), `LaneChangeLeftFailed`(106), `LaneChangeRightFailed`(107) |
| Hazards | `ObstacleHit`(120), `ObstacleRecoveryRequested`(121), `ObstacleRecovering`(122), `ObstacleRecovered`(123) |
| Coins | `CoinCollectRequested`(140), `CoinCollecting`(141), `CoinCollected`(142) |
| Power-up collect | `PowerUpCollectRequested`(160), `PowerUpCollecting`(161), `PowerUpCollected`(162) |
| Power-up activate | `PowerUpActivateRequested`(180), `PowerUpActivating`(181), `PowerUpActivated`(182), `PowerUpDeactivateRequested`(183), `PowerUpDeactivating`(184), `PowerUpDeactivated`(185) |
| Splines | `SplineSegmentCreateRequested`(200), `SplineSegmentCreating`(201), `SplineSegmentCreated`(202) *(data: SplineSegmentData)*, `SplineSegmentReleaseRequested`(203), `SplineSegmentReleasing`(204), `SplineSegmentReleased`(205), `CurrentSplineChangeRequested`(220), `CurrentSplineChanging`(221), `CurrentSplineChanged`(222) |
| Track segments | `TrackSegmentCreateRequested`(240), `TrackSegmentCreating`(241), `TrackSegmentCreated`(242), `TrackSegmentRecycleRequested`(243), `TrackSegmentRecycling`(244), `TrackSegmentRecycled`(245), `ActiveTrackChangeRequested`(260), `ActiveTrackChanging`(261), `ActiveTrackChanged`(262) |
| Teleport | `TeleportRequested`(280), `TeleportStarting`(281), `TeleportStarted`(282), `TeleportEndRequested`(283), `TeleportEnding`(284), `TeleportEnded`(285) |
| Bridged from GameFlow | `TempleRunConfigApplied`(300), `TempleRunScenesReady`(302), `TempleRunTrackConfigApplied`(304) *(data: string Resources path)* |
| Difficulty (bridged) | `TempleRunDifficultySettingsApplied`(310), `TempleRunDifficultyChanging`(312), `TempleRunDifficultyChanged`(314), `TempleRunDifficultyChangeFailed`(316), `TempleRunDifficultyChangeRequested`(318) |
| Difficulty (direct) | `DifficultySettingsApplied`(320), `DifficultyChanging`(321), `DifficultyChanged`(322), `DifficultyChangeFailed`(323) |
| Distance | `DistanceUpdated`(330) |
| Segment lifecycle | `SegmentEntering`(342), `SegmentEntered`(343), `SegmentExiting`(344), `SegmentExited`(345) *(all data: TrackSegmentInfo)* |
| Segment geometry | `SegmentGeometryReady`(350) *(data: SegmentGeometryData)* |

## UserInitiatedEvents (raw input)

Publisher: `EventsPublisherUserInitiated.Instance`. Implicit values 0–8.

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
GameplayReady                → MainMenuShowRequested     (boot → menu)
LevelSelected                → GameScenesLoadRequested   (level chosen → load)
GameScenesLoaded             → GameStartRequested        (loaded → start)
GameEnding                   → GameScenesUnloadRequested (death → unload)
```
*Commented out by default: the Save/Load chain and `QuitRequested → Quitting`.*

### TempleRun → TempleRun (`TempleRunAutoEventFlow.cs`)
```
PlayerPauseRequested       → PlayerPausing → PlayerPaused
PlayerResumeRequested      → PlayerResuming → PlayerResumed
CountdownStartRequested    → CountdownStarting
TempleRunStartRequested    → TempleRunStarting → TempleRunStarted
PlayerDied                 → TempleRunEndRequested → TempleRunEnding → TempleRunEnded
LaneChangeLeftRequested    → LaneChangingLeft
LaneChangeRightRequested   → LaneChangingRight
DashRequested              → DashStarting
JumpRequested              → JumpStarting
CoinCollectRequested       → CoinCollecting
PowerUpCollectRequested    → PowerUpCollecting
PowerUpCollected           → PowerUpActivateRequested → PowerUpActivating
PowerUpDeactivateRequested → PowerUpDeactivating
```
*Deliberately NOT auto-chained (commented): `SlideRequested → SlideStarting` and
`ObstacleHit → PlayerFailingAtObstacle` — the latter is gated by `PowerUpBuffController`
so a Shield can absorb the hit instead of failing.*

---

## Cross-domain bridge (`TempleRunGameFlowBridge.cs`)

The **only** place cross-domain event references are allowed.

### TempleRun → GameFlow
```
PlayerPaused    → PauseRequested
CountdownEnded  → GameStarted
TempleRunEnded  → GameEnding
```
### GameFlow → TempleRun
```
GameStarted        → TempleRunStartRequested
GameStarting       → CountdownStartRequested
GameConfigApplied  → TempleRunConfigApplied
TrackConfigApplied → TempleRunTrackConfigApplied
GameScenesLoaded   → TempleRunScenesReady
```

There is an intentional cycle: `CountdownEnded → GameStarted` (TR→GF) and
`GameStarted → TempleRunStartRequested` (GF→TR). It terminates because
`TempleRunStarted` has no further mapping.

## Input bridge (`Input2TempleRunAutoEventBridge.cs`)

UserInitiated → TempleRun:
```
UserQuitRequested  → TempleRunEndRequested
UserSlideRequested → SlideRequested
UserDashRequested  → DashRequested
```
The other six `UserInitiatedEvents` (turns, lane changes, jump, pause toggle) are consumed
**directly** by their controllers rather than bridged.
