# Endless Runner Template

An open-source **Unity 6 endless-runner template** built around a strict **event-driven
architecture**. It is a working game — procedurally generated track, turning, lane changes,
jumping, sliding, dashing, obstacles, coins, and power-ups — structured so that every system
communicates through a typed event bus rather than direct references. Use it as a starting
point for your own runner, or as a reference for decoupled, event-driven Unity design.

**Who this is for.** The template doubles as a teaching codebase. If you have taken a
software-design or game-development course (senior-undergraduate level), you already know
the ideas it is built from — publish/subscribe, the observer pattern, separation of
concerns, and data-driven design. What this project shows is those ideas *applied at full
scale*: an entire game where the only way systems talk to each other is by publishing and
subscribing to named events.

**Suggested reading order:**

1. This README — what the game is and the shape of the architecture.
2. [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) — the big idea, the three event domains,
   and how a run flows from boot to game over (with diagrams).
3. [docs/EVENTS.md](docs/EVENTS.md) — the full event catalog; skim it to see the naming
   pattern, then use it as a reference.
4. [docs/TRACKS.md](docs/TRACKS.md) — how the endless track is generated from data.
5. [docs/ADDING_A_MECHANIC.md](docs/ADDING_A_MECHANIC.md) — do this walkthrough; adding a
   mechanic touches every layer and is the fastest way to *get* the architecture.

## Highlights

- **Event-driven core.** No cross-system method calls. Three event domains
  (`GameFlow`, `TempleRun`, `UserInitiated`) communicate through a static typed event bus
  (`EventsFor<T>`), with declarative auto-chaining and exactly one bridge per domain crossing.
- **Additive scene composition.** A persistent bootstrap scene loads UI and gameplay scenes
  additively; gameplay is split from visuals, audio, and environment.
- **Data-driven track generation.** Track segments and per-level rulesets are authored as
  ScriptableObject assets (edited in the Inspector) and selected at runtime by
  tag/difficulty — no code changes needed to add a segment or a level.
- **Full runner mechanics.** Turns, lane changes, jump, slide, dash, obstacles, coins,
  power-ups (speed, score multiplier, coin magnet, coin doubler, shield), countdown, and a level selector
  with unlock/best-score persistence.
- **AI-assistant tooling.** Guides for AI agents (`AGENTS.md`, `CLAUDE.md`) plus seven
  skills that enforce and automate the event-system conventions — plain-markdown
  procedures any coding agent can follow (Claude Code runs them as slash commands).

## Requirements

- **Unity 6.5** (the `6000.5` stream), Universal Render Pipeline. Developed on Unity 6.5;
  open it with the latest 6.5 patch you have installed.
- Git (the project pulls two dependencies as git packages — see below).

## Getting Started

1. Clone the repository and open it in Unity 6.5.
2. Open `Assets/GameFlow/Scenes/Boot/0_BootStrap_Game_Only.unity`.
3. Enter Play Mode. The bootstrap scene loads the UI and gameplay scenes additively and
   takes you to the main menu → level select → gameplay.

> ⚠️ **Enable "Play Scene 0 Always."** `CrawfisSoftware > Play Scene 0 Always` in the Unity
> menu bar makes pressing Play always behave as if `0_BootStrap_Game_Only` were loaded first,
> regardless of which scene tab you actually have open. It defaults to on, but it's a global
> Editor preference, not a project setting, so a different project can leave it switched off.
> If Play Mode doesn't land you at the boot scene, check that it's still checked.

## Documentation

| Doc | What |
|-----|------|
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | Event domains, run sequence, and scene composition (with diagrams) |
| [docs/EVENTS.md](docs/EVENTS.md) | Full event catalog: every event, value, auto-chain, and bridge mapping |
| [docs/TRACKS.md](docs/TRACKS.md) | Track generation pipeline, ScriptableObject data model, and segment geometry |
| [docs/ADDING_A_MECHANIC.md](docs/ADDING_A_MECHANIC.md) | End-to-end walkthrough: adding a gameplay mechanic |
| [docs/STUDENT_TASKS.md](docs/STUDENT_TASKS.md) | Task catalog: 128 scoped projects, by sub-specialty, to take the runner to a polished product |
| [docs/TIMEBOX_1_REQUIREMENTS.md](docs/TIMEBOX_1_REQUIREMENTS.md) | The Timebox&nbsp;1 assignment (Studio Setup &amp; Greenlight): five phases, effort budget, git/AI setup, deliverable owners, team plans for 5/6/7/9+, and the presentation running order |
| [docs/TIMEBOX_2_REQUIREMENTS.md](docs/TIMEBOX_2_REQUIREMENTS.md) | The Timebox&nbsp;2 assignment (Design Wide, Build Narrow): the over-design pass, systems and seams, the design freeze, the greybox rule, sprint math, and agentic engineering |
| [docs/TIMEBOX_3_PLUS_REQUIREMENTS.md](docs/TIMEBOX_3_PLUS_REQUIREMENTS.md) | The Timebox&nbsp;3+ rhythm, reused every timebox: re-ranking, capture and before/after, video diary, light marketing, and graduating from greybox |
| [docs/EXERCISE_DRAW_THE_BOUNDARY.md](docs/EXERCISE_DRAW_THE_BOUNDARY.md) | Ungraded in-class team exercise: propose a subdomain split of the gameplay events, judged on bridge crossings and the replaceability the boundary buys |
| [docs/event-review/](docs/event-review/) | Architecture review of the event system, as two web pages: the **Event Seam Audit** (five recurring coupling forms and the defects they caused) and **The Half-Wired Chain** (six of them walked as shipped code beside the fix). Pairs with Draw the Boundary |
| [docs/canvas/](docs/canvas/) | Every assignment rendered as Canvas-ready HTML — one page per section, plus a build script per timebox to regenerate |
| [docs/KNOWN_ISSUES.md](docs/KNOWN_ISSUES.md) | Unity 6.5 caveats (UIDocument/Panel Renderer, build order, JsonUtility) |
| [AGENTS.md](AGENTS.md) | How AI agents should approach work here — any tool, not just Claude |
| [CLAUDE.md](CLAUDE.md) | AI-assistant guide: conventions and the event-system rules (written for every AI tool; Claude Code additionally runs the skills as slash commands) |
| [docs/specs/](docs/specs/) | Design specs and migration plans for proposed changes |
| [docs/playbooks/](docs/playbooks/) | Portable, project-agnostic upgrade guides (e.g. UIDocument → PanelRenderer) |
| [.github/CONTRIBUTING.md](.github/CONTRIBUTING.md) | Issue forms, labels, branch/PR conventions, and the Kanban workflow |

## Architecture

### The one rule

Systems never call each other. A system that wants something to happen **publishes an
event**; systems that care **subscribe** and react. The jump button doesn't call the player
controller — it publishes `UserJumpRequested`, which `Input2TempleRunAutoEventBridge`
translates to `JumpRequested`. `JumpController` subscribes, validates the request (no jumping
while already airborne), and publishes `JumpStarting` itself once the check passes —
deliberately *not* auto-chained, so the validation gate can reject; `JumpArcController`
subscribes to that, actually drives the
jump arc, and publishes `JumpStarted` partway through. Adding "play a sound on jump" later is
just a new subscriber to `JumpStarted` — zero edits to either controller. No component holds
a reference to a component in another system, so any system can be rewritten, replaced, or
deleted without touching the others.

### Event domains

All communication between systems goes through the static, typed `EventsFor<T>` event bus.
Events are grouped into three **domains** — separate enums, each on its own bus instance —
so that input, gameplay, and application lifecycle stay independent of one another:

| Domain | Enum | Responsibility |
|--------|------|----------------|
| **GameFlow** | `GameFlowEvents` | App/session lifecycle: loading, menus, level select, pause, config, quit |
| **TempleRun** | `TempleRunEvents` | Gameplay: player actions, countdown, track/spline generation, collisions, coins, power-ups |
| **UserInitiated** | `UserInitiatedEvents` | Raw input: turn/lane/jump/slide/dash/pause/quit requests |

Events auto-chain within a domain via a flat list of `(From, To)` pairs (`*AutoEventFlow.cs`),
so one event may fan out to several consequences. The only
crossing point between `TempleRun` and `GameFlow` is `TempleRunGameFlowBridge` — domain code
must never reference another domain's events directly anywhere else. That isolation is the
core discipline of the template, and the payoff is replaceability: GameFlow only knows about
`GameFlowEvents`, so the entire TempleRun game underneath it could be swapped for a
different game and GameFlow's menus, level select, and session management wouldn't need to
change a line. See the [Domain Isolation Rule](CLAUDE.md#domain-isolation-rule) for the full
rule and how to fix a violation.

```
UserInitiatedEvents ──▶ TempleRunEvents ◀──▶ GameFlowEvents
```

Two flows, not one pipeline. `UserInitiated` feeds into `TempleRun` one-way through
`Input2TempleRunAutoEventBridge`, the only subscriber to raw input in the codebase — so no
gameplay controller is coupled to "a human pressed a key," and an AI, a replay, or a network
peer can drive the same mechanic. `TempleRun` and `GameFlow` exchange in both directions
through `TempleRunGameFlowBridge`: `GameStarting` flows GameFlow → TempleRun to kick off the
countdown, while `TempleRunEnded` flows TempleRun → GameFlow to end the session.

---

### Scene structure

Scenes load additively from `0_BootStrap_Game_Only`:

- **Boot chain:** `0_BootStrap_Game_Only` → `Game_Boot_0_Test_Initialization` →
  `Game_Boot_1_UI` (menus, level selector, HUD panels) → `Game_Boot_2_Play` (bridge,
  blackboard, difficulty, level scene loader).
- **Gameplay:** `TempleRunGameplay` (which in turn loads the visuals, player, obstacles,
  collectables, environment, SFX, and GUI overlay scenes) plus the shared `TempleRunTrackPCG`.

Gameplay logic is kept separate from its visual and audio representation so either can be
swapped without touching the other.

### Track generation

Track generation is a three-stage, fully event-decoupled pipeline:

1. **Segment selection** (`TrackManager`) — picks abstract segments from a library filtered
   by the active level's tags/difficulty.
2. **Geometry** (`PathProvider`) — turns each segment into an Entrance → Pivot → Exit spline
   via a pluggable `IPathSegmentBuilder` (default `AxisAligned90Builder`; `ArcTurnBuilder`
   gives rounded corners), including "Either" T-junctions resolved by the player's choice.
3. **Visuals** (`PrefabSpawnerAbstract` subclasses) — spawns and recycles track geometry.

Segments and per-level rulesets are ScriptableObject assets in
`Assets/TempleRun/Scriptables/Track/` (`TrackSegmentSO`, `TrackSegmentRegistrySO`,
`TrackLevelSO`, `TrackLevelRegistrySO`). Edit them in the Inspector, create new ones from
`Assets > Create > CrawfisSoftware > TempleRun`, or use the `/generate-segments` skill.
See [docs/TRACKS.md](docs/TRACKS.md).

## Extending the Template

Because everything is event-driven, adding a feature starts with the events, not the code.
The project ships with skills (see `.claude/skills/`) that enforce the conventions. Each is
a plain-markdown procedure (`.claude/skills/<name>/SKILL.md`) that any AI coding tool can
follow as a checklist; in Claude Code they are also slash commands:

- `/list-events` — review the current event landscape
- `/add-event` — add events to the correct domain with proper naming/numbering
- `/add-auto-chain` — wire same-domain auto-progressions
- `/add-bridge-mapping` — wire cross-domain bridges
- `/audit-events` — scan for anti-patterns (missing unsubscriptions, cross-domain leaks, cycles)
- `/add-event-domain` — stand up a whole new event domain (rare; decision gate inside)
- `/generate-segments` — author track segments

See [CLAUDE.md](CLAUDE.md) for the full architecture guide and conventions.

### A good first exercise

`Assets/TempleRun/Scripts/Track/SpawnerBase.cs` and
`Assets/TempleRun/Scripts/TrackVisuals/PrefabSpawnerAbstract.cs` are parallel implementations
of the same algorithm — the first one's docstring even says "Mirrors `PrefabSpawnerAbstract`".
Consolidating them is a self-contained refactor that teaches the segment lifecycle. It is the
same exercise `AutoEventFlowBase` used to be, before that one was done (see
[docs/event-review/](docs/event-review/) for why).

## Dependencies

Pulled automatically as git packages (`Packages/manifest.json`):

- [`crawfis/EventsPublisher`](https://github.com/crawfis/EventsPublisher) — the event bus
- [`crawfis/GTMY.Audio`](https://github.com/crawfis/GTMY.Audio) — audio manager (Addressables-capable)

Random-seed utilities are vendored under `Assets/ThirdParty/CrawfisSoftware/`.

## License

See [LICENSE.txt](LICENSE.txt).
