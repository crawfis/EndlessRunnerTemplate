# Spec: Strategy Interfaces for Segment Selection & Power-Ups

**Status:** proposal / design exploration (not implemented)
**Scope:** two independent extension points — `ISegmentSelector` and `IPowerUpEffect`. They
share a theme (replace a baked-in switch/algorithm with pluggable strategies) but ship separately.

This is a design doc in the spirit of [../../AGENTS.md](../../AGENTS.md): it explores the
interface shape and trade-offs rather than prescribing a minimal implementation. Push back on
any of it.

---

# Part A — `ISegmentSelector`

## Motivation

Today, *how the next track segment is chosen* is hard-wired inside
`TrackSegmentLibrary.SelectNext` (`Assets/TempleRun/Scripts/Track/TrackSegmentLibrary.cs`):
connection-filtering → `MaxRepeat` → difficulty-gating → weighted random. That's one policy.
A template should make the **selection policy** a first-class, swappable thing so a level can be
"pure weighted random," "authored sequence," "difficulty ramps with distance," "wave/encounter
based," or something experimental — without subclassing `TrackManager` or editing the library.

## Current state

- `TrackManager` owns the look-ahead queue and calls `_library.SelectNext(previousId, repeat, random, targetDifficulty, range)`.
- `TrackSegmentLibrary` owns both the **data** (the segment pool, connections, lane config) and
  the **algorithm** (`SelectNext`, `SelectWeighted`, `IsAllowed`, `IsInDifficultyRange`).

The data and the algorithm are entangled. The refactor separates them.

## Proposed interface

```csharp
namespace CrawfisSoftware.TempleRun.Track
{
    /// Immutable-ish view of the segment pool + level config the selector reads from.
    public interface ISegmentPool
    {
        IReadOnlyList<TrackSegmentDefinition> Segments { get; }
        TrackSegmentDefinition ById(string id);
        IReadOnlyList<string> ConnectionsFrom(string id);   // empty => unconstrained
        int   LaneCount { get; }
        float LaneWidth { get; }
        string StartSegmentId { get; }
    }

    /// The state the selector may use to decide the next segment.
    public readonly struct SelectionContext
    {
        public readonly TrackSegmentDefinition Previous;   // null at start
        public readonly int    PreviousRepeatCount;
        public readonly float  DistanceTravelled;          // enables distance-based ramps
        public readonly int    SegmentIndex;               // how many chosen so far
        public readonly System.Random Random;              // deterministic seed source
    }

    /// The pluggable policy. One instance per run; construct from the pool.
    public interface ISegmentSelector
    {
        TrackSegmentDefinition SelectStart(ISegmentPool pool, SelectionContext ctx);
        TrackSegmentDefinition SelectNext(ISegmentPool pool, SelectionContext ctx);
    }
}
```

`TrackSegmentLibrary` becomes the `ISegmentPool` (data only). The existing algorithm moves into a
`WeightedDifficultySelector : ISegmentSelector` — behaviour-preserving, so nothing changes by
default.

## How it plugs in

- `TrackManager` gets a serialized selector choice. Options, roughly in order of effort:
  1. A `[SerializeReference]` field holding an `ISegmentSelector` (Unity can serialize managed
     references with a type picker) — most flexible, keeps it in the scene.
  2. A `ScriptableObject` factory (`SegmentSelectorAsset` with `Create(): ISegmentSelector`) —
     asset-based, reusable across levels, inspector-friendly.
  3. A string/enum on the level JSON (`"SelectorPolicy": "weighted"`) resolved by a small factory
     — keeps policy in level data, at the cost of a lookup table.
- `TrackManager` calls `_selector.SelectNext(_pool, ctx)` instead of `_library.SelectNext(...)`.

## Example strategies this unlocks

- `WeightedDifficultySelector` (the port of today's behaviour).
- `AuthoredSequenceSelector` — plays `ActiveSegmentIds` in order, looping — for tutorials/boss runs.
- `DistanceRampSelector` — raises `targetDifficulty` as `DistanceTravelled` grows.
- `WaveSelector` — alternates "calm" and "challenge" stretches by segment index.
- `MarkovSelector` — uses `Connections` as transition weights for authored flow.

## Migration path (behaviour-preserving)

1. Extract `ISegmentPool` from `TrackSegmentLibrary` (add the interface; the class already has
   all the members).
2. Move `SelectNext`/`SelectWeighted`/`IsAllowed`/`IsInDifficultyRange` into
   `WeightedDifficultySelector : ISegmentSelector`.
3. `TrackManager` resolves a selector (default = weighted) and calls it. Delete the old
   `SelectNext` from the library (or keep as `[Obsolete]` shim for one release).
4. Verify a run looks identical, then add a second selector to prove the seam.

## Trade-offs & open questions

- **`SelectionContext` surface.** Adding `DistanceTravelled`/`SegmentIndex` is what makes ramps
  possible, but every field is a commitment. Start minimal; grow it when a strategy needs it.
- **Where does policy live** — scene (`SerializeReference`), asset (`ScriptableObject`), or level
  JSON? Level JSON keeps everything data-driven (fits the track system) but needs a factory and
  can't hold tuning references. Leaning toward the `ScriptableObject` factory for authored levels
  plus a JSON fallback name.
- **Determinism.** Selectors must draw only from `ctx.Random` (the seeded `RandomProvider`) so
  replays stay reproducible. Worth stating as a contract in the interface XML docs.
- **Either-junction interaction.** Selection and the Either/T-junction resolution
  (`SegmentRequested`) are orthogonal today; keep them so — the selector picks *which* segment,
  `PathProvider` still resolves the branch.

---

# Part B — `IPowerUpEffect`

## Motivation

Adding a power-up today means editing **four** places: the `PowerUpType` enum, the `ApplyBuff`
switch, the `RemoveBuff` switch (`PowerUpBuffController`), and the relevant `Blackboard` fields.
The effect logic is smeared across two parallel switch statements. Make each power-up a
self-contained strategy so adding one is adding one file.

## Current state

`PowerUpBuffController` (`Assets/TempleRun/Scripts/Player/PowerUpBuffController.cs`):
- `ApplyBuff(PowerUpDefinition)` and `RemoveBuff(PowerUpType)` are twin `switch (type)` blocks
  writing `Blackboard` fields (`ActiveSpeedMultiplier`, `CoinMagnetActive/Radius`, `ShieldActive`,
  `ActiveScoreMultiplier`).
- The controller also owns duration timers, the "reset timer if re-collected" rule, and the
  **ObstacleHit gate** (Shield absorbs the hit → `ObstacleRecovered`, else → `PlayerFailingAtObstacle`).

## Proposed interface

```csharp
namespace CrawfisSoftware.TempleRun.PowerUps
{
    public interface IPowerUpEffect
    {
        PowerUpType Type { get; }
        void Apply(PowerUpContext ctx);     // called on activate (and on re-activate after Remove)
        void Remove(PowerUpContext ctx);    // called on expiry / cleanup

        // Optional hooks a few effects need. Default no-op via a base class.
        // Lets Shield intercept an obstacle hit without the controller special-casing it.
        bool TryAbsorbObstacle(PowerUpContext ctx) => false;
    }

    public readonly struct PowerUpContext
    {
        public readonly Blackboard Board;
        public readonly PowerUpDefinition Definition;   // Magnitude, Duration, etc.
    }
}
```

- `PowerUpBuffController` keeps what is genuinely central: the registry of active effects, the
  duration timers, the re-collect-resets-timer rule, and the event plumbing
  (`PowerUpActivated/Deactivated`). It delegates the *what happens* to `IPowerUpEffect`.
- `Shield` becomes an effect whose `TryAbsorbObstacle` returns true and publishes recovery; the
  controller's `OnObstacleHit` asks each active effect `TryAbsorbObstacle` instead of hard-coding
  `Blackboard.ShieldActive`. That removes the one special-case that currently leaks power-up
  knowledge into the collision path.

## How it plugs in

- Effects are registered in a `Dictionary<PowerUpType, IPowerUpEffect>`, built from either:
  - a `[SerializeReference] List<IPowerUpEffect>` on the controller, or
  - a set of `PowerUpEffectAsset` ScriptableObjects (parallels the existing `PowerUpDefinition`
    assets nicely — `PowerUpDefinition` holds data, the effect asset holds behaviour), or
  - reflection over `IPowerUpEffect` implementors (least ceremony, least explicit).
- `PowerUpType` can remain (it keys the registry and the JSON), or long-term be replaced by the
  effect id so the enum stops being a second place to edit.

## Migration path (behaviour-preserving)

1. Introduce `IPowerUpEffect` + `PowerUpContext` and a `PowerUpEffectBase` with no-op hooks.
2. Port each `case` from `ApplyBuff`/`RemoveBuff` into a small effect class
   (`SpeedBoostEffect`, `CoinMagnetEffect`, `ShieldEffect`, `ScoreMultiplierEffect`).
3. Replace the twin switches with `effect.Apply(ctx)` / `effect.Remove(ctx)`.
4. Replace the `Blackboard.ShieldActive` check in `OnObstacleHit` with a loop over active effects'
   `TryAbsorbObstacle`.
5. Verify all four power-ups behave identically, then add a fifth (e.g. `DoubleJumpEffect`) as one
   new file to prove the seam.

## Trade-offs & open questions

- **Blackboard coupling.** Effects still write `Blackboard` fields. That's fine for now (it's the
  shared runtime state), but note the dependency; a future pass could give effects their own
  scoped state object.
- **Stacking policy.** Today re-collecting resets the timer and same-type buffs don't stack.
  Should that stay controller policy (uniform) or become per-effect (some stack, some refresh)?
  Recommend keeping it controller-level until a design actually wants per-effect stacking.
- **Data vs behaviour split.** `PowerUpDefinition` (data) + `IPowerUpEffect` (behaviour) is clean,
  but two assets per power-up is more bookkeeping. A single `PowerUpEffectAsset` that *is* the
  definition and the behaviour is an alternative worth weighing.
- **Enum retirement.** Dropping `PowerUpType` entirely removes a place-to-edit but touches the
  JSON schema and existing assets; probably a later, separate step.
