# Endless Runner Template

An open-source **Unity 6 endless-runner template** built around a strict **event-driven
architecture**. It is a working game — procedurally generated track, turning, lane changes,
jumping, sliding, dashing, obstacles, coins, and power-ups — structured so that every system
communicates through a typed event bus rather than direct references. Use it as a starting
point for your own runner, or as a reference for decoupled, event-driven Unity design.

## Highlights

- **Event-driven core.** No cross-system method calls. Three event domains
  (`GameFlow`, `TempleRun`, `UserInitiated`) communicate through singleton publishers, with
  declarative auto-chaining and a single cross-domain bridge.
- **Additive scene composition.** A persistent bootstrap scene loads UI and gameplay scenes
  additively; gameplay is split from visuals, audio, and environment.
- **Data-driven track generation.** Track segments and per-level rulesets are authored in
  JSON and selected at runtime by tag/difficulty. Includes an in-editor Track Level Editor.
- **Full runner mechanics.** Turns, lane changes, jump, slide, dash, obstacles, coins,
  power-ups (speed, score multiplier, coin magnet, shield), countdown, and a level selector
  with unlock/best-score persistence.
- **AI-assistant tooling.** A `CLAUDE.md` guide and six project skills that enforce and
  automate the event-system conventions.

## Requirements

- **Unity 6000.5** (developed on `6000.5.2f1`), Universal Render Pipeline.
- Git (the project pulls two dependencies as git packages — see below).

## Getting Started

1. Clone the repository and open it in Unity 6000.5.
2. Open `Assets/GameFlow/Scenes/Boot/0_BootStrap_Game_Only.unity`.
3. Enter Play Mode. The bootstrap scene loads the UI and gameplay scenes additively and
   takes you to the main menu → level select → gameplay.

## Documentation

| Doc | What |
|-----|------|
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | Event domains, run sequence, and scene composition (with diagrams) |
| [docs/EVENTS.md](docs/EVENTS.md) | Full event catalog: every event, value, auto-chain, and bridge mapping |
| [docs/TRACKS.md](docs/TRACKS.md) | Track generation pipeline, JSON schema, and segment geometry |
| [docs/ADDING_A_MECHANIC.md](docs/ADDING_A_MECHANIC.md) | End-to-end walkthrough: adding a gameplay mechanic |
| [docs/KNOWN_ISSUES.md](docs/KNOWN_ISSUES.md) | Unity 6000.5 caveats (UIDocument/Panel Renderer, build order, JsonUtility) |
| [CLAUDE.md](CLAUDE.md) | AI-assistant guide: conventions and the event-system rules |
| [docs/specs/](docs/specs/) | Design specs for proposed extensions |

## Architecture

### Event domains

All communication between systems goes through the `EventsPublisher` event bus. Each domain
owns its own enum and singleton publisher:

| Domain | Enum | Responsibility |
|--------|------|----------------|
| **GameFlow** | `GameFlowEvents` | App/session lifecycle: loading, menus, level select, pause, config, quit |
| **TempleRun** | `TempleRunEvents` | Gameplay: player actions, countdown, track/spline generation, collisions, coins, power-ups |
| **UserInitiated** | `UserInitiatedEvents` | Raw input: turn/lane/jump/slide/dash/pause/quit requests |

Events auto-chain within a domain via dictionary mappings (`*AutoEventFlow.cs`), and cross
between `TempleRun` and `GameFlow` only through `TempleRunGameFlowBridge`. Domain code never
references another domain's events directly — that isolation is the core discipline of the
template.

```
USER INPUT (UserInitiatedEvents)
    ↓
TEMPLERUN GAMEPLAY (TempleRunEvents)
    ↓  (via TempleRunGameFlowBridge)
GAMEFLOW SESSION (GameFlowEvents)
```

### Scene structure

Scenes load additively from `0_BootStrap_Game_Only`:

- **Boot chain:** `0_BootStrap_Game_Only` → `Game_Boot_0_Test_Initialization` →
  `Game_Boot_1_UI` (menus, level selector, HUD panels) → `Game_Boot_2_Play` (event
  publishers, bridge, difficulty).
- **Gameplay:** `TempleRunGameplay` + `TempleRunTrackPCG`, which in turn load the visuals,
  player, obstacles, collectables, environment, SFX, and GUI overlay scenes.

Gameplay logic is kept separate from its visual and audio representation so either can be
swapped without touching the other.

### Track generation

Track generation is a three-stage, fully event-decoupled pipeline:

1. **Segment selection** (`TrackManager`) — picks abstract segments from a library filtered
   by the active level's tags/difficulty.
2. **Geometry** (`PathProvider`) — turns each segment into an Entrance → Pivot → Exit spline
   (axis-aligned 90° turns), including "Either" T-junctions resolved by the player's choice.
3. **Visuals** (`PrefabSpawnerAbstract` subclasses) — spawns and recycles track geometry.

Segments live in `Assets/TempleRun/Resources/TrackSegments_Registry.json`; per-level rulesets
live in `TrackLevel_*.json`. Author them with **`CrawfisSoftware > Track Level Editor`** or the
`/generate-segments` skill.

## Extending the Template

Because everything is event-driven, adding a feature starts with the events, not the code.
The project ships with skills (see `.claude/skills/`) that enforce the conventions:

- `/list-events` — review the current event landscape
- `/add-event` — add events to the correct domain with proper naming/numbering
- `/add-auto-chain` — wire same-domain auto-progressions
- `/add-bridge-mapping` — wire cross-domain bridges
- `/audit-events` — scan for anti-patterns (missing unsubscriptions, cross-domain leaks, cycles)
- `/generate-segments` — author track segments

See [CLAUDE.md](CLAUDE.md) for the full architecture guide and conventions.

### A good first exercise

`Assets/_Common/Events/AutoEventFlowBase.cs` is an intentional empty placeholder. The
auto-flow and bridge classes each re-implement the same event-dispatch logic; consolidating it
into a shared base class is a self-contained refactor that teaches the dispatch mechanism.

## Dependencies

Pulled automatically as git packages (`Packages/manifest.json`):

- [`crawfis/EventsPublisher`](https://github.com/crawfis/EventsPublisher) — the event bus
- [`crawfis/GTMY.Audio`](https://github.com/crawfis/GTMY.Audio) — audio manager (Addressables-capable)

Random-seed utilities are vendored under `Assets/ThirdParty/CrawfisSoftware/`.

## License

See [LICENSE.txt](LICENSE.txt).
