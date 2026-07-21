# Architecture

How the pieces fit together. For the concrete event lists see [EVENTS.md](EVENTS.md); for the
track system see [TRACKS.md](TRACKS.md); for the AI-assistant conventions see
[../CLAUDE.md](../CLAUDE.md).

## Event domains

Every system communicates through a typed event bus. Each domain owns an enum and a singleton
publisher; nothing calls across domains directly except the one bridge.

```mermaid
flowchart TD
    subgraph Input["UserInitiated (input)"]
      UI[UserInitiatedEvents]
    end
    subgraph TR["TempleRun (gameplay)"]
      TRE[TempleRunEvents]
    end
    subgraph GF["GameFlow (app lifecycle)"]
      GFE[GameFlowEvents]
    end

    UI -- "Input2TempleRunAutoEventBridge<br/>(+ direct subscribers)" --> TRE
    TRE -- "TempleRunGameFlowBridge" --> GFE
    GFE -- "TempleRunGameFlowBridge" --> TRE

    classDef d fill:#1f2937,stroke:#64748b,color:#e5e7eb;
    class UI,TRE,GFE d;
```

- **Auto-chains** move events *within* a domain (e.g. `PauseRequested → Pausing → Paused`).
- **The bridge** (`TempleRunGameFlowBridge`) is the single sanctioned crossing between
  TempleRun and GameFlow. Domain code translates a foreign event into a local one there,
  then subscribes to the local event. See the [Domain Isolation Rule](../CLAUDE.md#domain-isolation-rule).

## A run, end to end

The happy path from boot to a running game, showing which domain each step lives in and where
the bridge hands off.

```mermaid
sequenceDiagram
    participant GF as GameFlow
    participant BR as Bridge
    participant TR as TempleRun

    Note over GF: boot complete
    GF->>GF: GameplayReady → MainMenuShowRequested → MainMenuShowing
    Note over GF: player picks a level
    GF->>GF: LevelSelected → GameScenesLoadRequested → GameScenesLoading
    GF->>GF: (scenes load) → GameScenesLoaded → GameStartRequested → GameStarting
    GF->>BR: GameStarting
    BR->>TR: CountdownStartRequested → CountdownStarting → CountdownStarted → CountdownTick…
    TR->>BR: CountdownEnded
    BR->>GF: GameStarted
    GF->>BR: GameStarted
    BR->>TR: TempleRunStartRequested → TempleRunStarting → TempleRunStarted
    Note over TR: running — turns, jumps, coins, obstacles…
    TR->>TR: PlayerDied → TempleRunEndRequested → TempleRunEnding → TempleRunEnded
    TR->>BR: TempleRunEnded
    BR->>GF: GameEnding → GameScenesUnloadRequested → … → GameEnded
```

## Scene composition

Scenes load **additively** from a single persistent bootstrap scene. Gameplay logic is split
from its visual, audio, and environment scenes so either can change independently.

```mermaid
flowchart TD
    B["0_BootStrap_Game_Only<br/><i>(build index 0, persistent)</i>"]
    B --> I["Game_Boot_0_Test_Initialization"]
    I --> UIs["Game_Boot_1_UI<br/><i>menus, level selector, HUD panels</i>"]
    I --> P["Game_Boot_2_Play<br/><i>publishers, bridge, difficulty</i>"]

    subgraph Gameplay["Loaded on GameScenesLoading (DynamicLevelSceneLoader)"]
      GP["TempleRunGameplay"]
      PCG["TempleRunTrackPCG"]
    end
    P -. "GameScenesLoading" .-> GP
    P -. "GameScenesLoading" .-> PCG

    GP --> V1["TempleRunTrackVisuals"]
    GP --> V2["TempleRunPlayerVisuals"]
    GP --> V3["TempleRunObstacles"]
    GP --> V4["TempleRunCollectables"]
    GP --> V5["TempleRunEnvironment"]
    GP --> V6["TempleRunSfx"]
    GP --> V7["TempleRunGuiOverlay"]
```

### Load / unload mechanics

Scene orchestration is data-driven from Inspector components — there is no central scene
manager class:

| Component | Role |
|-----------|------|
| `LoadSceneAdditively` | unconditional additive load in `Start()` (the boot chain) |
| `DynamicLevelSceneLoader` | on `GameScenesLoading`, loads `GameState.SelectedLevel.GameplaySceneName` + `TempleRunTrackPCG` |
| `FireEventAfterSceneLoads` | waits for a set of scenes to load, then fires a completion event (this is what produces `GameScenesLoaded`) |
| `UnloadNonActiveScenes` | on `GameEnded`, unloads every scene with `buildIndex > _lastSceneIndexToKeep`, then publishes `GameScenesUnloaded` |
| `CloseSceneOnEvent` / `FireEventWhenSceneCloses` | per-scene self-unload / unload notification |

> ⚠️ **Build-order dependency.** `UnloadNonActiveScenes._lastSceneIndexToKeep` relies on the
> gameplay scenes being **last** in the Build Settings scene list, and the boot chain being
> first. The entry scene must be build index 0. If you reorder Build Settings, re-check the
> keep-index. See [KNOWN_ISSUES.md](KNOWN_ISSUES.md).

## Where things live

```
Assets/
├── _Common/    shared config (DifficultyConfig), AutoEventFlowBase placeholder, utilities
├── GameFlow/   app lifecycle: events, bridge, menus/level-select, scene management, progress
└── TempleRun/  gameplay: events, player mechanics, track generation, input, visuals
```

See [../CLAUDE.md](../CLAUDE.md) for the full folder breakdown and file reference.
