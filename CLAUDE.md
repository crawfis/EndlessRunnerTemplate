# CLAUDE.md - AI Assistant Guide for EndlessRunner

This file provides guidance for AI assistants working with the EndlessRunner codebase — an
open-source Unity 6 endless-runner template demonstrating an event-driven architecture. For
a human-facing overview, see [README.md](README.md).

**Deep-dive docs:** [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) (diagrams),
[docs/EVENTS.md](docs/EVENTS.md) (event catalog), [docs/TRACKS.md](docs/TRACKS.md) (track
system), [docs/ADDING_A_MECHANIC.md](docs/ADDING_A_MECHANIC.md) (worked example),
[docs/KNOWN_ISSUES.md](docs/KNOWN_ISSUES.md) (Unity caveats).

## Quick Reference

### Essential Commands
```
Play in Editor:     Enter Play Mode from Assets/GameFlow/Scenes/Boot/0_BootStrap_Game_Only
Event Logging:      CrawfisSoftware > Events > Event Logging Enabled
Track Authoring:    Edit the TrackSegmentSO / TrackSegmentRegistrySO / TrackLevelSO assets
                    in Assets/TempleRun/Scriptables/Track/ (Inspector)
```

### Critical Paths
| Purpose | Path |
|---------|------|
| Entry Scene | `Assets/GameFlow/Scenes/Boot/0_BootStrap_Game_Only` |
| GameFlow Events | `Assets/GameFlow/Scripts/Events/GameFlowEvents.cs` |
| TempleRun Events | `Assets/TempleRun/Scripts/Events/TempleRunEvents.cs`, `UserInitiatedEvents.cs` |
| Event Publishers | `Assets/GameFlow/Scripts/Events/EventsPublisherGameFlow.cs`, `Assets/TempleRun/Scripts/Events/EventsPublisherTempleRun.cs`, `EventsPublisherUserInitiated.cs` |
| Auto-Event Flow | `Assets/GameFlow/Scripts/Events/GameFlowAutoEventFlow.cs`, `Assets/TempleRun/Scripts/Events/TempleRunAutoEventFlow.cs`, `Assets/_Common/Events/AutoEventFlowBase.cs` |
| Cross-Domain Bridge | `Assets/GameFlow/Scripts/TempleRunSpecific/TempleRunGameFlowBridge.cs` |
| Game State | `Assets/GameFlow/Scripts/Config/GameState.cs`, `Assets/TempleRun/Scripts/Config/Blackboard.cs` |

## MANDATORY: Event System Enforcement

**ALL communication between systems MUST go through the EventsPublisher event system. No exceptions.**

### Rules for ANY Code Change

1. **No direct cross-system method calls.** Components MUST NOT call methods on components in other scenes or domains. Use events instead.
2. **No `FindObjectOfType`, `GetComponent` across scene boundaries, `SendMessage`, or `BroadcastMessage`** for cross-system communication.
3. **Every new feature, behavior, or action** that communicates across systems MUST have corresponding events in the appropriate enum.
4. **Every subscription MUST have a matching unsubscription** in `OnDestroy()`.
5. **Domain isolation: Cross-domain event references are ONLY allowed in bridge files.** See [Domain Isolation Rule](#domain-isolation-rule) below.

### Domain Isolation Rule

**Each domain's code may ONLY subscribe to, publish, or reference events from its own domain.** Cross-domain event references are permitted ONLY inside bridge classes.

| Code Location | May Reference |
|---------------|---------------|
| `Assets/TempleRun/**/*.cs` | `TempleRunEvents`, `UserInitiatedEvents` only |
| `Assets/GameFlow/**/*.cs` (non-bridge) | `GameFlowEvents` only |
| `TempleRunGameFlowBridge.cs` | `TempleRunEvents` + `GameFlowEvents` (bridge duty) |

**Violations — what NOT to do:**
- TempleRun scripts subscribing to or publishing `GameFlowEvents` (e.g., `EventsPublisherGameFlow.Instance.SubscribeToEvent(GameFlowEvents.GameStarted, ...)` in a TempleRun file)
- GameFlow scripts subscribing to or publishing `TempleRunEvents` (e.g., `EventsPublisherTempleRun.Instance.PublishEvent(TempleRunEvents.CountdownTick, ...)` in a GameFlow file)

**How to fix a violation:** If TempleRun code needs to react to a GameFlow event, add a bridge mapping in `TempleRunGameFlowBridge.cs` that translates the GameFlow event into a TempleRun event, then subscribe to the TempleRun event in your TempleRun code.

### Required Skills Workflow

When adding any new feature or behavior, you MUST follow this workflow:

1. **`/list-events`** — First, review existing events to understand the current landscape and avoid duplicates
2. **`/add-event`** — Add new events to the correct domain enum with proper naming and numbering
3. **`/add-auto-chain`** — Wire automatic event progressions (e.g., Requested -> Starting) if needed
4. **`/add-bridge-mapping`** — Wire cross-domain bridges if the feature spans domains
5. **`/audit-events`** — After implementation, verify no anti-patterns were introduced

**Do NOT skip these steps.** Even for "simple" features, the event infrastructure must be established BEFORE writing the feature logic. The event definitions drive the architecture.

### When to Use Each Skill

| Situation | Required Skills |
|-----------|----------------|
| Adding any new feature | `/list-events` then `/add-event` then implement |
| Feature spans two domains | `/add-bridge-mapping` after `/add-event` |
| Events should auto-progress | `/add-auto-chain` after `/add-event` |
| After any implementation work | `/audit-events` to verify compliance |
| Before starting work on events | `/list-events` to understand current state |
| Authoring track segments | Edit `TrackSegmentSO` / `TrackLevelSO` assets in the Inspector (see [docs/TRACKS.md](docs/TRACKS.md#authoring)). *The `/generate-segments` skill is legacy — it targets the removed JSON registry.* |

## Architecture Overview

Unity 6 endless runner demonstrating **event-driven architecture**.

**Three Event Domains:**
- `GameFlowEvents` - Application lifecycle (loading screens, menus, level selection, game sessions, pause/resume, config/difficulty, quit)
- `TempleRunEvents` - Gameplay-specific (player lifecycle, countdown, turns, slides, jumps, dashes, lane changes, collisions, coins, power-ups, track/spline generation, teleportation)
- `UserInitiatedEvents` - Raw input events (turn requests, lane changes, jump, slide, dash, pause toggle, quit)

**Three Singleton Publishers:**
- `EventsPublisherGameFlow.Instance`
- `EventsPublisherTempleRun.Instance`
- `EventsPublisherUserInitiated.Instance`

## Event System Patterns

### Subscribing to Events

```csharp
private void Awake()
{
    EventsPublisherGameFlow.Instance.SubscribeToEvent(
        GameFlowEvents.GameStarting,
        OnGameStarting
    );
}

private void OnDestroy()
{
    EventsPublisherGameFlow.Instance.UnsubscribeToEvent(
        GameFlowEvents.GameStarting,
        OnGameStarting
    );
}

private void OnGameStarting(string eventName, object sender, object data)
{
    // Handle event - cast data if needed: var score = (float)data;
}
```

**CRITICAL: Always unsubscribe in OnDestroy()** - failure causes null reference errors after scene unload.

### Publishing Events

```csharp
// Without data
EventsPublisherGameFlow.Instance.PublishEvent(
    GameFlowEvents.MainMenuShown,
    this,
    null
);

// With data payload
float score = Blackboard.Instance.DistanceTracker.DistanceTravelled;
EventsPublisherTempleRun.Instance.PublishEvent(
    TempleRunEvents.PlayerDied,
    this,
    score
);
```

### Auto-Event Flow Pattern

Events auto-chain through dictionary mappings in `GameFlowAutoEventFlow.cs` and `TempleRunAutoEventFlow.cs`:

```csharp
// In GameFlowAutoEventFlow.cs - GameFlow domain auto-chains
_autoGameFlow2GameFlowEvents = new Dictionary<GameFlowEvents, GameFlowEvents>()
{
    { GameFlowEvents.GameStartRequested, GameFlowEvents.GameStarting },
    { GameFlowEvents.GameScenesLoaded, GameFlowEvents.GameStartRequested },
    { GameFlowEvents.GameEnding, GameFlowEvents.GameScenesUnloadRequested },
    // ... more mappings
};
```

When `GameScenesLoaded` fires, it automatically triggers `GameStartRequested` -> `GameStarting`.

> Note: `AutoEventFlowBase.cs` under `Assets/_Common/Events/` is currently an empty
> placeholder. The three dispatch classes (`GameFlowAutoEventFlow`,
> `TempleRunAutoEventFlow`, `TempleRunGameFlowBridge`, and the input bridge) each
> re-implement the same `Enum.Parse` dispatch logic. Consolidating that shared logic
> into `AutoEventFlowBase` is a good, self-contained refactoring exercise.

### Adding New Events

**Step 1: Determine the correct domain**
- `GameFlowEvents` - For app/session lifecycle (loading, menus, level select, pause, config, quit)
- `TempleRunEvents` - For gameplay mechanics (player actions, countdown, track, collisions)
- `UserInitiatedEvents` - For raw input events

**Step 2: Add to appropriate enum with a unique value** (values grouped by category, gaps of ~10 between categories).

**Step 3: (Optional) Add auto-chaining** in the appropriate flow class.

**Step 4: Subscribe and publish as needed.**

### Event Naming Conventions
- `*Requested` - User or system initiated a request
- `*Starting` / `*ing` - Action is beginning (async operation in progress)
- `*Started` / `*ed` - Action completed successfully
- `*Failed` - Action failed
- `*Cancelled` - Action was cancelled

## Coding Conventions

### Namespaces
```
CrawfisSoftware.Events           - Event system core (GameFlowEvents, UserInitiatedEvents, publishers)
CrawfisSoftware.TempleRun        - Gameplay logic (TempleRunEvents enum)
CrawfisSoftware.TempleRun.Events - Gameplay auto-event flows (TempleRunAutoEventFlow)
CrawfisSoftware.GameFlow         - Application lifecycle
CrawfisSoftware.GameFlow.Events  - GameFlow events and publishers
CrawfisSoftware.GameFlow.UI      - UI controllers
CrawfisSoftware.Config           - Shared, domain-neutral config (DifficultyConfig)
```

### Field Naming
```csharp
[SerializeField] private string _sceneName;      // Private: underscore prefix
public float TurnAvailableDistance { get; }      // Properties: PascalCase
private readonly Dictionary<...> _mapping = ...; // readonly: underscore prefix
```

### Transform Conventions
- **Prefer `transform.localPosition`** over `transform.position` when reading or writing positions
- **Prefer `transform.localRotation`** over `transform.rotation`
- **When setting parent**, use `transform.SetParent(parent, worldPositionStays: false)`

### MonoBehaviour Lifecycle
- `Awake()` - Subscriptions and initialization
- `OnDestroy()` - Cleanup and unsubscriptions
- `Start()` - Only when dependent on other Awake() completions

## Key Files Reference

| Category | Files |
|----------|-------|
| **GameFlow Domain** | |
| Event Enums | `Assets/GameFlow/Scripts/Events/GameFlowEvents.cs` |
| Event Publishers | `Assets/GameFlow/Scripts/Events/EventsPublisherGameFlow.cs` |
| Auto-Event Flow | `Assets/GameFlow/Scripts/Events/GameFlowAutoEventFlow.cs` |
| Bridge | `Assets/GameFlow/Scripts/TempleRunSpecific/TempleRunGameFlowBridge.cs` |
| Game State / Config | `Assets/GameFlow/Scripts/Config/GameState.cs`, `GameConstants.cs`, `LevelConfig.cs`, `LevelRegistry.cs`, `LevelProgressManager.cs` |
| UI Controllers | `Assets/GameFlow/Scripts/UI/MainMenuController.cs`, `MainMenuPanelController.cs`, `LevelSelectorController.cs`, `GameFlowUIPanelController.cs` |
| Game Control | `Assets/GameFlow/Scripts/GameControl/QuitController.cs`, `UnloadNonActiveScenes.cs` |
| Scene Management | `Assets/GameFlow/Scripts/SceneManagement/DynamicLevelSceneLoader.cs`, `FireEventAfterSceneLoads.cs`, `LoadSceneAfterGameControlEvent.cs` |
| **TempleRun Domain** | |
| Event Enums | `Assets/TempleRun/Scripts/Events/TempleRunEvents.cs`, `UserInitiatedEvents.cs` |
| Event Publishers | `Assets/TempleRun/Scripts/Events/EventsPublisherTempleRun.cs`, `EventsPublisherUserInitiated.cs` |
| Auto-Event Flow | `Assets/TempleRun/Scripts/Events/TempleRunAutoEventFlow.cs`, `Input2TempleRunAutoEventBridge.cs` |
| Config | `Assets/TempleRun/Scripts/Config/Blackboard.cs`, `TempleRunGameConfig.cs`, `GameDifficultyManager.cs` |
| Player Controllers | `Assets/TempleRun/Scripts/Player/TurnController.cs`, `JumpController.cs`, `SlideController.cs`, `DashController.cs`, `LaneChangeController.cs`, `PlayerLifeController.cs`, `PowerUpBuffController.cs` |
| Track Generation | `Assets/TempleRun/Scripts/Track/TrackManager.cs`, `PathProvider.cs`, `SegmentTransitionController.cs`, `TrackSegmentLibrary.cs` |
| Track Data | `Assets/TempleRun/Scriptables/Track/` — `TrackSegmentSO` (per segment), `TrackSegmentRegistrySO` (pool), `TrackLevelSO` (per level) |
| Input | `Assets/TempleRun/Scripts/Input/MovementInputActions.cs`, `SwipeDetectorActions.cs`, `DashInputActions.cs`, `AccelerometerInputActions.cs` |
| **Shared/Common** | |
| Auto-Event Base | `Assets/_Common/Events/AutoEventFlowBase.cs` (placeholder — see note above) |
| Shared Config | `Assets/_Common/Config/DifficultyConfig.cs` |
| Utilities | `Assets/_Common/Utility/Logger.cs`, `EventLoggerDump.cs`; `Assets/_Common/Events/EventHistory.cs` |

## Gotchas and Warnings

### Event Subscriptions
- **ALWAYS** unsubscribe in `OnDestroy()` - failure causes errors after scene unload
- Event handler signature: `(string eventName, object sender, object data)`
- Cast data explicitly: `var score = (float)data;` or `var tuple = ((Direction, float))data;`

### Scene Loading
- All scenes load **additively** from the persistent Boot scene (`0_BootStrap_Game_Only`)
- **Never** use `LoadSceneMode.Single` unless intentionally resetting everything
- `UnloadNonActiveScenes._lastSceneIndexToKeep` depends on gameplay scenes being LAST in the Build Settings scene list

### Auto-Event Flow
- Auto-events fire immediately by default (configurable delay in `AutoEventFlowBase`)
- Circular dependencies will cause infinite loops - verify mappings with `/audit-events`
- Some events are intentionally NOT auto-chained (documented in comments)

### Singletons
- `Blackboard.Instance` - TempleRun runtime state
- `GameState` - static GameFlow flags + selected level
- `EventsPublisher*.Instance` - Event buses (`[DefaultExecutionOrder(-10000)]`)

## Testing

### Enable Event Logging
`CrawfisSoftware > Events > Event Logging Enabled`, then inspect via `EventLoggerDump` / `EventHistory`.

### Play
Enter Play Mode from `Assets/GameFlow/Scenes/Boot/0_BootStrap_Game_Only`. The boot chain loads
the UI and gameplay scenes additively.

## Common Tasks

### Adding a New Gameplay Feature
1. **`/list-events TempleRun`** — Review existing TempleRun events
2. **`/add-event`** — Add events to `TempleRunEvents` for the new mechanic
3. **`/add-auto-chain`** — Wire auto-progressions (e.g., Requested -> Starting)
4. **`/add-bridge-mapping`** — Bridge to GameFlow if the feature affects game session state
5. Create/extend a scene under `Assets/TempleRun/Scenes/Gameplay/`
6. Subscribe to relevant events in `Awake()`, unsubscribe in `OnDestroy()`
7. Publish state changes as events via `EventsPublisherTempleRun.Instance`
8. Keep visuals/audio separate from logic
9. **`/audit-events`** — Verify compliance

### Adding a New GameFlow Feature
1. **`/list-events GameFlow`** — Review existing GameFlow events
2. **`/add-event`** — Add events to `GameFlowEvents`
3. **`/add-auto-chain`** — Wire auto-progressions
4. Implement, subscribing/publishing via `EventsPublisherGameFlow.Instance`
5. **`/audit-events`** — Verify compliance

### Authoring Track Segments / Levels
- Edit the ScriptableObject assets in `Assets/TempleRun/Scriptables/Track/` via the Inspector; create
  new ones from `Assets > Create > CrawfisSoftware > TempleRun > Track Segment / Track Segment Registry / Track Level`.
- Segment pool: `TrackSegmentRegistrySO` (array of per-segment `TrackSegmentSO`).
- Per-level rulesets: `TrackLevelSO` (tag/id-filtered selection from the registry), resolved by `LevelNumber` through `TrackLevelRegistrySO` (assigned to `TrackManager._trackLevels`).
- Seam: GameFlow publishes `LevelApplied(int)` (its `LevelConfig.LevelNumber`); it never references a track type. The int is bridged to `TempleRunLevelApplied`, parked on `Blackboard.SelectedLevel`, and read at `TrackManager` init — the SOs are read only by `TrackLibraryLoader`.
- See [docs/TRACKS.md](docs/TRACKS.md#the-data-model).

## Folder Structure

Three primary domains with clear separation of concerns:

```
Assets/
├── _Common/                          # Shared infrastructure
│   ├── Config/                       # DifficultyConfig (domain-neutral)
│   ├── Events/                       # AutoEventFlowBase (placeholder), EventHistory
│   └── Utility/                      # Logger, EventLoggerDump
│
├── GameFlow/                         # Application lifecycle domain
│   ├── Scripts/
│   │   ├── Events/                   # GameFlowEvents, EventsPublisherGameFlow, GameFlowAutoEventFlow
│   │   ├── TempleRunSpecific/        # TempleRunGameFlowBridge (bridges TempleRun <-> GameFlow)
│   │   ├── Config/                   # GameState, GameConstants, LevelConfig, LevelRegistry, LevelProgressManager
│   │   ├── GameControl/              # QuitController, UnloadNonActiveScenes
│   │   ├── UI/                       # MainMenu, LevelSelector, loading/game-over panels
│   │   └── SceneManagement/          # DynamicLevelSceneLoader, FireEventAfterSceneLoads
│   └── Scenes/Boot/                  # 0_BootStrap_Game_Only, Game_Boot_0/1/2
│
└── TempleRun/                        # Gameplay domain
    ├── Scripts/
    │   ├── Events/                   # TempleRunEvents, UserInitiatedEvents, publishers, auto-flows, input bridge
    │   ├── Config/                   # Blackboard, TempleRunGameConfig, per-mechanic configs, GameDifficultyManager
    │   ├── Player/                   # Turn/Jump/Slide/Dash/Lane controllers, PowerUpBuffController, collision detectors
    │   ├── Track/                    # TrackManager, PathProvider, SegmentTransitionController, TrackSegmentLibrary, TrackSegmentSO/RegistrySO/TrackLevelSO
    │   ├── TrackVisuals/             # PrefabSpawner (SimplePlane, Voxels)
    │   └── Input/                    # MovementInputActions, SwipeDetectorActions, DashInputActions, Accelerometer
    ├── Scenes/Gameplay/              # TempleRunGameplay, TrackPCG, TrackVisuals, PlayerVisuals, Obstacles, Collectables, Sfx, Environment, GuiOverlay
    ├── Scriptables/                  # Per-mechanic configs + Track/ (segment/registry/level SOs), Levels/ (LevelConfig, LevelRegistry)
    └── Editor/                       # TrackDataImporter (one-shot JSON -> SO converter)
```

### Event Flow Architecture

```
USER INPUT (UserInitiatedEvents in TempleRun)
    ↓
TEMPLERUN GAMEPLAY (TempleRunEvents)
    ↓ (via TempleRunGameFlowBridge in GameFlow)
GAMEFLOW SESSION (GameFlowEvents)
```
