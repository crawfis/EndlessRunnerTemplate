# CLAUDE.md - AI Assistant Guide for EndlessRunner

This file is the concrete working guide for AI assistants — **any** AI assistant or coding
agent, not just Claude — working with the EndlessRunner codebase: an open-source Unity 6.5
(6000.5 stream) endless-runner template demonstrating an event-driven architecture. Start
with [AGENTS.md](AGENTS.md) for how to approach work here; this file holds the rules,
conventions, and paths. For a human-facing overview, see [README.md](README.md).

**Deep-dive docs:** [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) (diagrams),
[docs/EVENTS.md](docs/EVENTS.md) (event catalog), [docs/TRACKS.md](docs/TRACKS.md) (track
system), [docs/ADDING_A_MECHANIC.md](docs/ADDING_A_MECHANIC.md) (worked example),
[docs/KNOWN_ISSUES.md](docs/KNOWN_ISSUES.md) (Unity caveats).
**Course material:** [docs/STUDENT_TASKS.md](docs/STUDENT_TASKS.md) (the 130-task catalog),
`docs/TIMEBOX_*_REQUIREMENTS.md` (the timebox assignments), and `docs/EXERCISE_*.md`
(in-class team exercises) — students working in this repo will ask for help with these.

## Quick Reference

### Essential Commands
```
Play in Editor:     Enter Play Mode from Assets/GameFlow/Scenes/Boot/0_BootStrap_Game_Only
Event Logging:      CrawfisSoftware > Events > Log Events   (same menu: Clear Now,
                    List Current Subscribers, Clear Events on Exiting Play Mode)
List Domains:       CrawfisSoftware > Events > List Domains   (per domain: prefix, enum,
                    member / payload / sticky / replay counts — EventsPublisher 2.5.0+)
Track Authoring:    Edit the TrackSegmentSO / TrackSegmentRegistrySO / TrackLevelSO /
                    TrackLevelRegistrySO assets under Assets/TempleRun/Scriptables/Track/
                    (one asset per segment in Track/Segments/) via the Inspector
```

### Critical Paths
| Purpose | Path |
|---------|------|
| Entry Scene | `Assets/GameFlow/Scenes/Boot/0_BootStrap_Game_Only` |
| GameFlow Events | `Assets/GameFlow/Scripts/Events/GameFlowEvents.cs` |
| TempleRun Events | `Assets/TempleRun/Scripts/Events/TempleRunEvents.cs`, `UserInitiatedEvents.cs` |
| Countdown Events | `Assets/Countdown/Scripts/Events/CountdownEvents.cs` |
| Event Bus | `EventsFor<T>` from the `com.crawfissoftware.eventspublisher` package (no per-domain publisher files) |
| Auto-Event Flow | `Assets/GameFlow/Scripts/Events/GameFlowAutoEventFlow.cs`, `Assets/TempleRun/Scripts/Events/TempleRunAutoEventFlow.cs`, `Assets/Countdown/Scripts/Events/CountdownAutoEventFlow.cs`, `Assets/_Common/Events/AutoEventFlowBase.cs` |
| Cross-Domain Bridge | `Assets/GameFlow/Scripts/TempleRunSpecific/TempleRunGameFlowBridge.cs`, `Assets/GameFlow/Scripts/CountdownSpecific/CountdownGameFlowBridge.cs`, `Assets/Countdown/Scripts/TempleRunSpecific/Countdown2TempleRunBridge.cs` |
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

The rule's purpose is **replaceability**: a domain that talks only through events can be
swapped for a completely different implementation — a new track generator, or a
deterministic autopilot / replayed recording instead of a human (`AIController` already
publishes the same `UserInitiatedEvents` the input actions do) — or stubbed out with a
trivial fake, without touching code on the other side. The additive scene composition makes
this concrete: replacing a domain is loading a different scene that speaks the same events.

| Code Location | May Reference |
|---------------|---------------|
| `Assets/TempleRun/**/*.cs` (non-bridge) | `TempleRunEvents` only |
| `Assets/GameFlow/**/*.cs` (non-bridge) | `GameFlowEvents` only |
| `Assets/Countdown/**/*.cs` (non-bridge) | `CountdownEvents` only |
| `TempleRunGameFlowBridge.cs` | `TempleRunEvents` + `GameFlowEvents` (bridge duty) |
| `Input2TempleRunAutoEventBridge.cs` | `UserInitiatedEvents` + `TempleRunEvents` (bridge duty) |
| `CountdownGameFlowBridge.cs` | `GameFlowEvents` + `CountdownEvents` (bridge duty) |
| `Countdown2TempleRunBridge.cs` | `CountdownEvents` + `TempleRunEvents` (bridge duty) |

**`UserInitiatedEvents` is a one-way funnel, not a shared vocabulary.** It has a
publish/subscribe asymmetry the other domains don't:

- **Publishing is open.** Any *input source* may publish it — the `Scripts/Input/` action
  classes, `AIController`, and any future replay or netcode driver. That is the seam that
  makes the player replaceable.
- **Subscribing is closed.** Only `Input2TempleRunAutoEventBridge` may subscribe. Gameplay
  controllers subscribe to the *TempleRun* event the bridge translates it into.

Both halves are load-bearing. Open publishing is what lets an autopilot stand in for a human;
closed subscribing is what lets the entire input domain be swapped for a network or replay
source that speaks `TempleRunEvents` directly, without a controller caring.

**Violations — what NOT to do:**
- TempleRun scripts subscribing to or publishing `GameFlowEvents` (e.g., `GameFlowBus.Subscribe(GameFlowEvents.GameStarted, ...)` in a TempleRun file)
- GameFlow scripts subscribing to or publishing `TempleRunEvents` (e.g., `TempleRunBus.Publish(TempleRunEvents.PlayerActivateRequested, ...)` in a GameFlow file)
- Gameplay or UI scripts outside `Assets/Countdown/` referencing `CountdownEvents` (e.g., `CountdownBus.Subscribe(CountdownEvents.CountdownTick, ...)` in a TempleRun file) — only the two Countdown bridges may
- A gameplay controller subscribing to `UserInitiatedEvents` (e.g., `UserInputBus.Subscribe(UserInitiatedEvents.UserJumpRequested, ...)` in `JumpController`). Add a bridge mapping and subscribe to the TempleRun event instead.

> **Validation gates and auto-chains.** A ladder (`*Requested → *Starting → *Started →
> *Ending → *Ended`) is meant to start **fully auto-chained**: that alone gives a working
> mechanic with no controller, and each link you later break out of the `ChainTable` is
> where code goes — `Requested → Starting` is the gate (may this happen?), `Starting →
> Started` is warm-up (often nothing), `Started → Ending` is the action's duration,
> `Ending → Ended` is the recovery window.
>
> **Leave every link you have no code for chained.** A link still in the `ChainTable` is a
> seam a teammate can open — they insert a hook or a delay by breaking that one link, and no
> controller or subscriber changes. Two adjacent `Publish` calls for consecutive rungs of the
> same ladder destroy that seam and are always the anti-pattern; the second call belongs in
> the `ChainTable`. Because a chained event fires synchronously inside its source's publish,
> do teardown *before* publishing the `*Ending` rung, not between the two.
>
> **The rule is that a gate and a chain cannot share a link.** Once input arrives via the
> bridge, the domain's `*Requested` event is the bridge's *raw* translation — it fires
> whether or not the action is legal. So a controller that validates (cooldown,
> already-airborne, lane boundary) must **publish `*Starting` itself** once its checks pass,
> and that link must be removed from the `ChainTable` in the same edit — an auto-chain fires
> before any validation runs and silently defeats the gate. See the comments in
> `TempleRunAutoEventFlow.cs`, and [docs/ADDING_A_MECHANIC.md](docs/ADDING_A_MECHANIC.md#2-chain-the-whole-ladder-then-break-the-links-you-need).

**How to fix a violation:** If TempleRun code needs to react to a GameFlow event, add a bridge mapping in `TempleRunGameFlowBridge.cs` that translates the GameFlow event into a TempleRun event, then subscribe to the TempleRun event in your TempleRun code.

> There are no assembly definitions (`.asmdef`) — everything compiles into
> `Assembly-CSharp`, so the compiler will NOT catch a violation. Domain isolation is
> enforced only by review and by `/audit-events`; run it.

### Required Skills Workflow

Each step below is a **skill**: a step-by-step procedure stored as plain markdown in
`.claude/skills/<name>/SKILL.md`. In Claude Code, invoke it as the slash command shown. In
any other AI tool (Copilot, Cursor, Codex, Gemini, …), open the skill file and follow it as
a checklist — the steps are ordinary read/search/edit work and assume nothing
Claude-specific. Anywhere this repo's docs say `/some-skill`, read it as "follow
`.claude/skills/some-skill/SKILL.md`".

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
| Feature needs a whole NEW domain (rare) | `/add-event-domain` — decision gate inside; then `/add-event` for its events |
| Authoring track segments | Edit `TrackSegmentSO` / `TrackLevelSO` assets in the Inspector, or use `/generate-segments` for bulk creation (see [docs/TRACKS.md](docs/TRACKS.md#authoring)) |

## Architecture Overview

Unity 6.5 endless runner demonstrating **event-driven architecture**.

**Domain Registry** — the authoritative list of event domains:

| Domain | Enum (bus alias) | Purpose | Flow / bridge hosting (lifetime) | Bridges |
|--------|------------------|---------|----------------------------------|---------|
| **GameFlow** | `GameFlowEvents` (`GameFlowBus`) | App/session lifecycle: loading, menus, level select, pause, config/difficulty, quit | `GameFlowAutoEventFlow` in `0_BootStrap_Game_Only` (whole app) | ↔ TempleRun via `TempleRunGameFlowBridge` (hosted in `Game_Boot_2_Play`); → Countdown via `CountdownGameFlowBridge` |
| **TempleRun** | `TempleRunEvents` (`TempleRunBus`) | Gameplay: player lifecycle, movement, collisions, coins/power-ups, track/spline generation, teleportation | `TempleRunAutoEventFlow` in `TempleRunGameplay` (one run) | ↔ GameFlow via `TempleRunGameFlowBridge`; ← UserInitiated via `Input2TempleRunAutoEventBridge`; ← Countdown via `Countdown2TempleRunBridge` |
| **Countdown** | `CountdownEvents` (`CountdownBus`) | Session ceremony: the pre-run 3…2…1 (start, ticks, end) and its overlay | `CountdownAutoEventFlow` in `Game_Boot_2_Play`, on the `CountdownDomain` object with both bridges (session) | ← GameFlow via `CountdownGameFlowBridge` (`GameStarting → CountdownStartRequested`); → TempleRun via `Countdown2TempleRunBridge` (`CountdownEnded → PlayerActivateRequested`) |
| **UserInitiated** | `UserInitiatedEvents` (`UserInputBus`) | Raw input requests: turns, lanes, jump, slide, dash, pause toggle, quit | none (input never auto-chains) | → TempleRun via `Input2TempleRunAutoEventBridge` (hosted in `TempleRunGameplay`) |

Two invariants keep this registry trustworthy:
- **Placement:** domain enums live only in `Assets/*/Scripts/Events/` folders, named
  `*Events.cs` — the full inventory is one glob (`Assets/*/Scripts/Events/*Events.cs`),
  equivalently every enum marked `[EventEnum]`. An `[EventEnum]` enum anywhere else is an
  audit finding (`/audit-events`).
- **Registration:** `/add-event-domain` adds a row here and to the mirror table at the top
  of [docs/EVENTS.md](docs/EVENTS.md) as part of its checklist.

**One static facade per domain.** `EventsFor<T>` is static and lazily initialized, so there
is no singleton, no scene GameObject and no execution-order attribute to get right. Alias it
per file:

```csharp
using GameFlowBus   = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.GameFlow.Events.GameFlowEvents>;
using TempleRunBus  = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;
using CountdownBus  = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.Countdown.Events.CountdownEvents>;
using UserInputBus  = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.Events.UserInitiatedEvents>;
```

> The per-domain `EventsPublisherEnums*.Instance` singletons were removed. `[DefaultExecutionOrder(-10000)]`
> only ordered `Awake` within one scene load batch, so it never protected the additively
> loaded scenes that actually hosted them. (The package's untyped, string-based
> `EventsPublisher.Instance` still backs the inspector-configured scene-management and
> logging helpers — `FireEventAfterSceneLoads`, `CloseSceneOnEvent`, `TimedEvent`, the
> event loggers, the `Test_AutoFireEvent*` helpers.)

## Event System Patterns

### Subscribing to Events

```csharp
private void Awake()
{
    GameFlowBus.Subscribe(GameFlowEvents.GameStarting, OnGameStarting);
}

private void OnDestroy()
{
    GameFlowBus.Unsubscribe(GameFlowEvents.GameStarting, OnGameStarting);
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
GameFlowBus.Publish(GameFlowEvents.MainMenuShown, this, null);

// With data payload
float score = Blackboard.Instance.DistanceTracker.DistanceTravelled;
TempleRunBus.Publish(TempleRunEvents.PlayerDied, this, score);
```

### Typed Payloads

An event that carries data declares its type on the enum member with `[EventPayload]` —
that declaration is the contract (and what StrictMode validates). Call sites do NOT mint
`EventId<T>` fields: the `static readonly EventId<T> X = Bus.Id<T>(...)` pattern was
removed by owner ruling (2026-09, "it makes the code hard to read"). Subscribe and publish
through the bus alias directly, and cast the payload on the first line of the handler:

```csharp
[EventPayload(typeof(TrackSegmentInfo))]
ActiveTrackChanging = 261,

private void Awake()     => TempleRunBus.Subscribe(TempleRunEvents.ActiveTrackChanging, OnTrackChanging);
private void OnDestroy() => TempleRunBus.Unsubscribe(TempleRunEvents.ActiveTrackChanging, OnTrackChanging);

private void OnTrackChanging(string eventName, object sender, object data)
{
    var segment = (TrackSegmentInfo)data;
}
```

The cast is deliberately bare — no `is` guard, no null check (see the no-defensive-guards
rule): a wrong payload should throw at the cast, and the `[EventPayload]` declaration on
the enum is what tells every call site (and StrictMode) which cast is right. Events with no
payload, or a genuinely variable one, stay undeclared - no declaration means no checking,
which is the intended default. Do not reintroduce `EventId<T>` fields in new code.

**A basic-typed payload gets an inline comment saying what the value *is*.**
`typeof(TrackSegmentInfo)` names its own meaning; `typeof(int)` does not, and a reader of
one member should not have to find the publisher to learn whether the number is a player,
a level or a score:

```csharp
[EventPayload(typeof(int))]  // Player id
JumpRequested = 80,

[EventPayload(typeof(float))]  // Distance travelled, run-absolute
DistanceUpdated = 330,
```

This applies to `int`, `float`, `string`, `bool`, `long` and `double`. Domain types name
themselves and need no comment.

### Delivery Policy - edge or level

Default is `Transient`: a subscriber that arrives after the publish hears nothing. Mark an
event `[EventDelivery(EventDelivery.Sticky)]` **only** if it is a *level*.

- **Edge** - a transition (`MainMenuHiding`, `DifficultyChangeRequested`). Only meaningful in
  sequence. Replaying it to a late subscriber is actively wrong. Leave it `Transient`.
- **Level** - a state (`TrackLevelApplied`, `TempleRunDifficultySettingsApplied` —
  currently the only two Sticky events in the codebase). Self-describing on its own, so
  replay is safe. An event that already carries a payload is usually a level.

Where a level is currently expressed as **two opposing edges** (`Paused`/`Resumed`), Sticky
alone is unsafe - a late subscriber gets whichever half fired last. Model the level directly
as one event carrying the value instead, and keep the edges `Transient` for animation and SFX.

Use **Window > Events > Upgrade Audit** after a play session to see which events actually had
a late subscriber. That measurement, not the name, is the evidence for making one Sticky.

Read a retained value without subscribing with `EventsFor<T>.TryGetLast(eventEnum, out
sender, out data)` (cast `data`), which is how `TrackManager` gets the selected level at init.

### Auto-Event Flow Pattern

Events auto-chain through flat `(From, To)` pair tables in `GameFlowAutoEventFlow.cs` and `TempleRunAutoEventFlow.cs`:

```csharp
// In GameFlowAutoEventFlow.cs - GameFlow domain auto-chains
// (three entries excerpted from a much larger table)
private static readonly (GameFlowEvents From, GameFlowEvents To)[] ChainTable =
{
    (GameFlowEvents.GameStartRequested, GameFlowEvents.GameStarting),
    (GameFlowEvents.GameScenesLoaded, GameFlowEvents.GameStartRequested),
    (GameFlowEvents.GameEnding, GameFlowEvents.GameScenesUnloadRequested),
    // ... more entries
};

protected override IReadOnlyList<(GameFlowEvents From, GameFlowEvents To)> Chains => ChainTable;
```

When `GameScenesLoaded` fires, it automatically triggers `GameStartRequested` -> `GameStarting`.

> **Chains are declared as a flat list of pairs, not a dictionary.** All seven dispatch
> classes (three auto-flows, four bridges) share one implementation in
> `Assets/_Common/Events/AutoEventFlowBase.cs`:
> `EventChainDispatcher<TSource, TDest>` does subscribe-to-all, lookup and publish;
> `AutoEventFlowBase<TSource, TDest>` is the MonoBehaviour wrapper for a single direction.
> A bidirectional bridge cannot inherit twice, so `TempleRunGameFlowBridge` holds two
> dispatchers instead.
>
> The pair list exists so **one event may declare several consequences** — a dictionary
> allowed exactly one successor each. That ceiling never produced bugs directly; it produced
> workarounds. Finding a source event's slot already taken, developers published the second
> consequence by hand inside a controller, which is how failure logic came to publish pause
> events. Targets fire in declaration order, synchronously, so each target's own chain
> completes before the next is published — keep multi-target groups together and say why the
> order matters.

### Adding New Events

**Step 1: Determine the correct domain**
- `GameFlowEvents` - For app/session lifecycle (loading, menus, level select, pause, config, quit)
- `TempleRunEvents` - For gameplay mechanics (player actions, track, collisions)
- `CountdownEvents` - For the pre-run ceremony (start, ticks, end)
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
CrawfisSoftware.Events            - Event-system core: EventsFor<T>/EventId<T> (package) + UserInitiatedEvents + EventHistory
                                    + EventChainDispatcher/AutoEventFlowBase (_Common/Events)
CrawfisSoftware.TempleRun         - Gameplay logic (TempleRunEvents enum, Blackboard, player controllers,
                                    most of Track/ and all of TrackVisuals/, GUIController)
CrawfisSoftware.TempleRun.Events  - TempleRun auto-event flow + Input2TempleRunAutoEventBridge
CrawfisSoftware.TempleRun.Track   - Segment-selection seam only (Track/Selection/); .Track.Geometry holds the spline builders
CrawfisSoftware.TempleRun.*       - Per-area: .Input, .Audio, .PowerUps, .GameConfig, .Editor
CrawfisSoftware.Countdown         - Ceremony logic (CountdownController)
CrawfisSoftware.Countdown.Events  - CountdownEvents, CountdownAutoEventFlow, Countdown2TempleRunBridge
CrawfisSoftware.Countdown.UI      - CountdownUIController
CrawfisSoftware.GameFlow          - Application lifecycle (incl. QuitController, LoadSceneAdditively)
CrawfisSoftware.GameFlow.Events   - GameFlowEvents, GameFlowAutoEventFlow, TempleRunGameFlowBridge, CountdownGameFlowBridge
CrawfisSoftware.GameFlow.UI       - UI controllers
CrawfisSoftware.GameFlow.*        - Per-area: .Config, .GameConfig, .GameControl (UnloadNonActiveScenes only), .SceneManagement
CrawfisSoftware.Config            - Shared, domain-neutral config (DifficultyConfig)
CrawfisSoftware.Utility / .Utilities / .Utility.Testing / .Test - _Common utilities and test helpers
CrawfisSoftware.Scriptables / .AssetManagement / .Spawners - vendored ThirdParty code
```

One known stray declares a namespace that doesn't match its folder:
`Assets/_Common/Utility/DebugLog.cs` (namespace `CrawfisSoftware.TempleRun`). Use the
declared namespace when referencing it. (`PlayerPrefKeys.cs` was the other; it moved to
`Assets/TempleRun/Scripts/Config/` to match its namespace and its only caller.)

### Field Naming
```csharp
[SerializeField] private string _sceneName;      // Private: underscore prefix
public float TurnAvailableDistance { get; }      // Properties: PascalCase
private readonly Dictionary<...> _mapping = ...; // readonly: underscore prefix
private static readonly (X From, X To)[] ChainTable = ...; // static readonly: PascalCase
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
| Event Bus | `EventsFor<GameFlowEvents>`, aliased as `GameFlowBus` |
| Auto-Event Flow | `Assets/GameFlow/Scripts/Events/GameFlowAutoEventFlow.cs` |
| Bridge | `Assets/GameFlow/Scripts/TempleRunSpecific/TempleRunGameFlowBridge.cs`, `Assets/GameFlow/Scripts/CountdownSpecific/CountdownGameFlowBridge.cs` |
| Game State / Config | `Assets/GameFlow/Scripts/Config/GameState.cs`, `GameConstants.cs`, `LevelConfig.cs`, `LevelConfigApplier.cs`, `LevelRegistry.cs`, `LevelProgressManager.cs`, `LevelProgressData.cs` |
| UI Controllers | `Assets/GameFlow/Scripts/UI/MainMenuController.cs`, `MainMenuPanelController.cs`, `LevelSelectorController.cs`, `LevelSelectorPanelController.cs`, `GameFlowUIPanelController.cs` (loading screen + game-over overlay; the countdown overlay lives in the Countdown domain) |
| UI Toolkit Assets | `Assets/GameFlow/UI Toolkit/UI/UXML/` (MainMenu, LevelSelector, LoadingScreen, HUD, GameOver overlay) + USS; `Assets/TempleRun/UI Toolkit/TempleRunDistances.uxml`; `Assets/Countdown/UI Toolkit/Countdown.uxml` |
| Game Control | `Assets/GameFlow/Scripts/GameControl/QuitController.cs`, `UnloadNonActiveScenes.cs`, `LoadSceneAdditively.cs` |
| Scene Management | `Assets/GameFlow/Scripts/SceneManagement/DynamicLevelSceneLoader.cs`, `FireEventAfterSceneLoads.cs`, `FireEventWhenSceneCloses.cs`, `CloseSceneOnEvent.cs`, `LoadSceneAfterGameControlEvent.cs` |
| **TempleRun Domain** | |
| Event Enums | `Assets/TempleRun/Scripts/Events/TempleRunEvents.cs`, `UserInitiatedEvents.cs` |
| Event Bus | `EventsFor<TempleRunEvents>` / `EventsFor<UserInitiatedEvents>`, aliased as `TempleRunBus` / `UserInputBus` |
| Auto-Event Flow | `Assets/TempleRun/Scripts/Events/TempleRunAutoEventFlow.cs`, `Input2TempleRunAutoEventBridge.cs` |
| Config | `Assets/TempleRun/Scripts/Config/Blackboard.cs`, `TempleRunGameConfig.cs`, `GameDifficultyManager.cs`, `DifficultySettings.cs`, `SetGameDifficulty.cs`, `LoadDefaultGameConfigs.cs`, `PlayerPrefKeys.cs`, `SpawnPrefabRegistry.cs`, `TempleRunConstants.cs`, per-mechanic configs (`CoinConfig.cs`, `DashConfig.cs`, `JumpConfig.cs`, `LaneConfig.cs`, `SlideConfig.cs`, `PowerUpDefinition.cs`, `PowerUpType.cs`) |
| Player Controllers | `Assets/TempleRun/Scripts/Player/TurnController.cs` (the turn gate only), `JumpController.cs`, `SlideController.cs`, `DashController.cs`, `LaneChangeController.cs`, `PlayerLifeController.cs`, `PowerUpBuffController.cs`, `DistanceController.cs`, `MoveCharacterByDistance.cs`, `PauseController.cs`, `PlayerPauseController.cs`, `AIController.cs` |
| Player Support | `Assets/TempleRun/Scripts/Player/` — collision detectors (`ObstacleCollisionDetector.cs`, `CollectableCollisionDetector.cs`, `TurnCollisionDetector.cs`), `CoinCollectionController.cs`, motion shaping (`JumpArcController.cs`, `SlideArcController.cs`, `DashSpeedController.cs`, `LaneOffsetController.cs`), failure/teleport (`PlayerFailedController.cs`, `PlayerFailureAutoTurnController.cs`, `TeleportController.cs`, `CharacterTeleporter.cs`, `TeleportInfo.cs`); `Assets/TempleRun/Scripts/GameTime.cs` (pausable gameplay clock) |
| Power-Up Effects | `Assets/TempleRun/Scripts/PowerUps/IPowerUpEffect.cs`, `PowerUpEffectBase.cs`, `SpeedBoostEffect.cs`, `ScoreMultiplierEffect.cs`, `CoinMagnetEffect.cs`, `CoinDoublerEffect.cs`, `ShieldEffect.cs` |
| Track Generation | `Assets/TempleRun/Scripts/Track/TurnCommitController.cs` (takes a turn from Starting through the Either-junction commit to Ending), `TrackManager.cs` (+ `TrackManagerAbstract.cs`, `TrackManagerForTiles.cs`, `TrackManagerList.cs` variants), `PathProvider.cs`, `SegmentTransitionController.cs`, `SegmentAdvanceTrigger.cs`, `TrackSegmentLibrary.cs`, `TrackLibraryLoader.cs`, `TrackSegmentInfo.cs`, `Direction.cs`, `DistanceTracker.cs`, `DistanceInterestService.cs`, `SegmentGeometryData.cs`, `SplineSection.cs`; SO classes `TrackSegmentSO.cs`, `TrackSegmentRegistrySO.cs`, `TrackLevelSO.cs`, `TrackLevelRegistrySO.cs` |
| Segment Selection | `Assets/TempleRun/Scripts/Track/Selection/` — `ISegmentSelector.cs` + `ISegmentPool.cs` (pluggable policy seam), `WeightedDifficultySelector.cs` (default, wired in `TrackManager`), `DistanceRampSelector.cs`, `WaveSelector.cs`, `AuthoredSequenceSelector.cs` |
| Track Geometry | `Assets/TempleRun/Scripts/Track/Geometry/` — `IPathSegmentBuilder.cs`, `AxisAligned90Builder.cs`, `ArcTurnBuilder.cs`, `PathPose.cs`, `PathSpan.cs`, `PathSegmentResult.cs`, `CardinalDirections.cs` |
| Spawners | `Assets/TempleRun/Scripts/Track/SpawnerBase.cs`, `CoinSpawner.cs`, `ObstacleSpawner.cs`, `PowerUpSpawner.cs`, `PowerUpIdentifier.cs` |
| Track Visuals | `Assets/TempleRun/Scripts/TrackVisuals/PrefabSpawnerAbstract.cs`, `SimplePlane/SplinePrefabSpawner.cs`, `SimplePlane/TextureScaler.cs`, `Voxels/VoxelPrefabSpawner.cs`, `Voxels/VoxelLaneTrackSpawner.cs` (lane-stretched voxels; the only visual that draws both T-junction arms) |
| Gameplay UI | `Assets/TempleRun/Scripts/UI/GUIController.cs` (distance HUD via UXML) |
| Audio / Animation | `Assets/TempleRun/Scripts/Audio/Metronome.cs`, `TurnAudioFeedback.cs`, `SetMusicPlayer.cs`, `CleanupAudioSingletons.cs` (uses the `com.gamesthatmoveyou.audiomanager` package); `Assets/TempleRun/Scripts/Animation/CapsuleAnimationLink.cs` |
| Track Data | `Assets/TempleRun/Scriptables/Track/` — `Segments/*.asset` (one `TrackSegmentSO` per segment), `TrackSegmentRegistry.asset` (the pool), `TrackLevel_01..05_*.asset` (`TrackLevelSO` per level), `TrackLevelRegistry.asset` |
| Input | `Assets/TempleRun/Scripts/Input/MovementInputActions.cs`, `SwipeDetectorActions.cs`, `DashInputActions.cs`, `AccelerometerInputActions.cs`, `PauseQuitInputActions.cs`; `GameControls.cs` + `LeftRightJumpSlide.cs` are **source-generated** from the `.inputactions` assets — regenerate, don't hand-edit |
| Editor Tools | `Assets/TempleRun/Editor/TrackDataImporter.cs` (one-shot JSON -> SO importer; menu `CrawfisSoftware > Track > Import JSON -> ScriptableObjects`) |
| **Countdown Domain** | |
| Event Enum | `Assets/Countdown/Scripts/Events/CountdownEvents.cs` (`CountdownStartRequested` 0 … `CountdownEnded` 5) |
| Event Bus | `EventsFor<CountdownEvents>`, aliased as `CountdownBus` |
| Auto-Event Flow | `Assets/Countdown/Scripts/Events/CountdownAutoEventFlow.cs` (`StartRequested -> Starting`, `Ending -> Ended`) |
| Bridges | `Assets/GameFlow/Scripts/CountdownSpecific/CountdownGameFlowBridge.cs` (GameFlow -> Countdown), `Assets/Countdown/Scripts/TempleRunSpecific/Countdown2TempleRunBridge.cs` (Countdown -> TempleRun) |
| Ceremony / UI | `Assets/Countdown/Scripts/CountdownController.cs`, `Assets/Countdown/Scripts/UI/CountdownUIController.cs`, `Assets/Countdown/UI Toolkit/Countdown.uxml` |
| **Shared/Common** | |
| Auto-Event Base | `Assets/_Common/Events/AutoEventFlowBase.cs` — `EventChainDispatcher<TSource, TDest>` + `AutoEventFlowBase<TSource, TDest>`; the one dispatch implementation, shared by all seven flow/bridge classes |
| Shared Config | `Assets/_Common/Config/DifficultyConfig.cs` |
| Utilities | `Assets/_Common/Utility/Logger.cs`, `EventLoggerDump.cs`, `DebugEventFileLogger.cs`, `DebugLog.cs`, `TimedEvent.cs`, `TextureExtensions.cs`; `Assets/_Common/Events/EventHistory.cs`; test helpers in `Assets/_Common/Test/` |
| Vendored | `Assets/ThirdParty/CrawfisSoftware/` — Random providers, AssetManagement helpers, editor tools (screenshots, play-first-scene, dev-build toggle), `Spawners/GridSpawner.cs`, `ScriptableInt.cs` |

## Gotchas and Warnings

### Event Subscriptions
- **ALWAYS** unsubscribe in `OnDestroy()` - failure causes errors after scene unload
- Event handler signature: `(string eventName, object sender, object data)`
- Cast data explicitly: `var score = (float)data;`, or `var section = (SplineSection)data;` for a
  struct payload (the `CurrentSplineChanging` payload — see `MoveCharacterByDistance.cs`)
- **Prefer a named struct over a tuple for any payload with more than one part.** A tuple's slots
  have no names, so every rule about them gets restated in each subscriber's comments instead of
  on the payload — which is how four components came to re-derive
  `SplineSection.TeleportOwnsTransform` by hand

### Scene Loading
- All scenes load **additively** from the persistent Boot scene (`0_BootStrap_Game_Only`)
- **Never** use `LoadSceneMode.Single` unless intentionally resetting everything
- `UnloadNonActiveScenes._lastSceneIndexToKeep` depends on gameplay scenes being LAST in the Build Settings scene list

### Auto-Event Flow
- Auto-chained events publish synchronously, inside the source event's publish call —
  there is no delay mechanism
- Circular dependencies will cause infinite loops - verify mappings with `/audit-events`
- Some events are intentionally NOT auto-chained (documented in comments)

### Singletons
- `Blackboard.Instance` - TempleRun runtime state
- `GameState` - MonoBehaviour singleton (`GameState.Instance`); its flags and
  `SelectedLevel` are static members
- `EventsFor<T>` - Static, lazily initialized event buses. Not singletons: no scene object,
  no execution-order attribute, no initialization race.

### Unity YAML and Line Endings
- Unity writes its YAML assets (`*.asset`, `*.unity`, `*.prefab`, `*.meta`,
  `*.inputactions`, …) with LF on every platform, and `.gitattributes` pins them to
  `eol=lf`. When generating such files by hand (e.g. `/generate-segments`), write LF —
  and never "fix" line-ending-only diffs.

### Dead / Placeholder Files (don't be misled by grep hits)
- (`Assets/_Common/Events/AutoEventFlowBase.cs` was an empty placeholder until the dispatch
  consolidation; it now holds the shared implementation.)
- `Assets/GameFlow/Scripts/Config/BlackboardGameFlow.cs` - fully commented out
- `Assets/TempleRun/Scripts/Config/DifficultyConfig.cs` - fully commented out; the live
  class is `Assets/_Common/Config/DifficultyConfig.cs` (namespace `CrawfisSoftware.Config`)
- `Assets/GameFlow/Scenes/Boot/Game_Boot_0_Initialization.unity` - not in Build Settings;
  the boot chain uses `Game_Boot_0_Test_Initialization`

## Testing

### Enable Event Logging
`CrawfisSoftware > Events > Log Events`, then inspect via `EventLoggerDump` /
`EventHistory` (or `DebugEventFileLogger` for a file sink). After a play session,
`Window > Events > Upgrade Audit` shows which events actually had late subscribers.

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
7. Publish state changes as events via `TempleRunBus`
8. Keep visuals/audio separate from logic
9. **`/audit-events`** — Verify compliance

### Adding a New GameFlow Feature
1. **`/list-events GameFlow`** — Review existing GameFlow events
2. **`/add-event`** — Add events to `GameFlowEvents`
3. **`/add-auto-chain`** — Wire auto-progressions
4. Implement, subscribing/publishing via `GameFlowBus`
5. **`/audit-events`** — Verify compliance

### Authoring Track Segments / Levels
- Edit the ScriptableObject assets in `Assets/TempleRun/Scriptables/Track/` via the Inspector; create
  new ones from `Assets > Create > CrawfisSoftware > TempleRun > Track Segment / Track Segment Registry / Track Level / Track Level Registry`.
- Segment pool: `TrackSegmentRegistrySO` (array of per-segment `TrackSegmentSO`).
- Per-level rulesets: `TrackLevelSO` (tag/id-filtered selection from the registry), resolved by `LevelNumber` through `TrackLevelRegistrySO` (assigned to `TrackManager._trackLevels`).
- Segment selection policy: `TrackManager` delegates "which segment next" to a pluggable
  `ISegmentSelector` (`Track/Selection/`); `WeightedDifficultySelector` is the default,
  with `DistanceRampSelector`, `WaveSelector`, and `AuthoredSequenceSelector` as alternates.
- T-junctions: a segment authored with `Direction: Either` (tag `either`) defers the turn
  until the player commits — `TrackManager` pauses lookahead (`_awaitingEitherDirection`)
  and `PathProvider` builds the exit spline only when the direction resolves. Only
  `VoxelLaneTrackSpawner` draws both arms before the commit.
- Seam: GameFlow publishes `LevelApplied(int)` (its `LevelConfig.LevelNumber`); it never references a track type. The int is bridged to `TrackLevelApplied`, which is `Sticky`, so `TrackManager` reads it at init with `TryGetLast` rather than it being mirrored into a field — the SOs are read only by `TrackLibraryLoader`.
- See [docs/TRACKS.md](docs/TRACKS.md#the-data-model).

## Folder Structure

Four primary domains with clear separation of concerns:

```
Assets/
├── _Common/                          # Shared infrastructure
│   ├── Config/                       # DifficultyConfig (domain-neutral, namespace CrawfisSoftware.Config)
│   ├── Events/                       # AutoEventFlowBase + EventChainDispatcher, EventHistory
│   ├── Test/                         # Test_AutoFireEvent, Test_AutoFireEventOnStart
│   └── Utility/                      # Logger, EventLoggerDump, DebugEventFileLogger, DebugLog, TimedEvent, TextureExtensions
│
├── Countdown/                        # Session-ceremony domain (the pre-run 3...2...1)
│   ├── Scripts/
│   │   ├── Events/                   # CountdownEvents, CountdownAutoEventFlow
│   │   ├── TempleRunSpecific/        # Countdown2TempleRunBridge (CountdownEnded -> PlayerActivateRequested)
│   │   ├── UI/                       # CountdownUIController
│   │   └── CountdownController.cs    # Runs the ceremony: Starting, Started, Tick, Ending
│   └── UI Toolkit/                   # Countdown.uxml (the overlay)
│
├── GameFlow/                         # Application lifecycle domain
│   ├── Scripts/
│   │   ├── Events/                   # GameFlowEvents, GameFlowAutoEventFlow
│   │   ├── TempleRunSpecific/        # TempleRunGameFlowBridge (bridges TempleRun <-> GameFlow)
│   │   ├── CountdownSpecific/        # CountdownGameFlowBridge (GameStarting -> CountdownStartRequested)
│   │   ├── Config/                   # GameState, GameConstants, LevelConfig(+Applier), LevelRegistry, LevelProgressManager/Data
│   │   ├── GameControl/              # QuitController, UnloadNonActiveScenes, LoadSceneAdditively
│   │   ├── UI/                       # MainMenu + LevelSelector controllers and panel controllers, GameFlowUIPanelController
│   │   └── SceneManagement/          # DynamicLevelSceneLoader, FireEventAfterSceneLoads/WhenSceneCloses, CloseSceneOnEvent, LoadSceneAfterGameControlEvent
│   ├── Scenes/Boot/                  # 0_BootStrap_Game_Only -> Game_Boot_0_Test_Initialization -> Game_Boot_1_UI -> Game_Boot_2_Play
│   └── UI Toolkit/                   # UXML/USS: MainMenu, LevelSelector, LoadingScreen, HUD, GameOver overlay
│
├── TempleRun/                        # Gameplay domain
│   ├── Scripts/
│   │   ├── Events/                   # TempleRunEvents, UserInitiatedEvents, TempleRunAutoEventFlow, Input2TempleRunAutoEventBridge
│   │   ├── Config/                   # Blackboard, TempleRunGameConfig, GameDifficultyManager, PlayerPrefKeys, per-mechanic configs, SpawnPrefabRegistry
│   │   ├── Player/                   # Turn/Jump/Slide/Dash/Lane/Life controllers, collision detectors, distance, pause, teleport, AI
│   │   ├── PowerUps/                 # IPowerUpEffect strategy: PowerUpEffectBase + five concrete effects
│   │   ├── Track/                    # TrackManager (+variants), PathProvider, SegmentTransitionController, spawners, SO classes, TrackLibraryLoader
│   │   │   ├── Geometry/             # IPathSegmentBuilder, AxisAligned90Builder, ArcTurnBuilder, PathPose, PathSpan, PathSegmentResult, CardinalDirections
│   │   │   └── Selection/            # ISegmentSelector/ISegmentPool + Weighted/DistanceRamp/Wave/AuthoredSequence selectors
│   │   ├── TrackVisuals/             # PrefabSpawnerAbstract; SimplePlane/ (spline plane) and Voxels/ (incl. VoxelLaneTrackSpawner)
│   │   ├── Input/                    # Movement/Swipe/Dash/Accelerometer/PauseQuit actions; generated GameControls + LeftRightJumpSlide
│   │   ├── UI/                       # GUIController (distance HUD)
│   │   ├── Audio/                    # Metronome, TurnAudioFeedback, SetMusicPlayer, CleanupAudioSingletons
│   │   ├── Animation/                # CapsuleAnimationLink
│   │   └── GameTime.cs               # Pausable gameplay clock (singleton)
│   ├── Scenes/                       # Gameplay/: TempleRunGameplay, TempleRunTrackPCG, TempleRunTrackVisuals, TempleRunPlayerVisuals,
│   │                                 #   TempleRunObstacles, TempleRunCollectables, TempleRunEnvironment, TempleRunSfx, TempleRunGuiOverlay;
│   │                                 #   PrefabScene (authoring-only, not in Build Settings)
│   ├── Scriptables/                  # Per-mechanic config assets; Track/ (Segments/*.asset, TrackSegmentRegistry, TrackLevel_01..05,
│   │                                 #   TrackLevelRegistry); Levels/ (GameFlow's LevelConfig assets + LevelRegistry live here)
│   ├── UI Toolkit/                   # TempleRunDistances.uxml
│   └── Editor/                       # TrackDataImporter (one-shot JSON -> SO converter)
│
└── ThirdParty/CrawfisSoftware/       # Vendored utilities: Random providers, AssetManagement helpers, editor tools, GridSpawner
```

### Event Flow Architecture

```
USER INPUT (UserInitiatedEvents in TempleRun)
    ↓
TEMPLERUN GAMEPLAY (TempleRunEvents)
    ↓ (via TempleRunGameFlowBridge in GameFlow)
GAMEFLOW SESSION (GameFlowEvents)
    ↓ (via CountdownGameFlowBridge — GameStarting)
COUNTDOWN CEREMONY (CountdownEvents)
    ↓ (via Countdown2TempleRunBridge — CountdownEnded)
TEMPLERUN PLAYER RELEASE (PlayerActivateRequested)
```
