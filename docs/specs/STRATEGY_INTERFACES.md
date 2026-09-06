# Spec: Strategy Interfaces for Segment Selection & Power-Ups

**Status:** implemented (Part A `709d4b1` + PR #9, Part B `fa85967`; merged as `5a819b1`, 2026-07-22) — the "where does policy live" question below is still open
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

- **Shipped** — `WeightedDifficultySelector` (the port of today's behaviour).
- **Shipped** — `AuthoredSequenceSelector` — plays `ActiveSegmentIds` in order, looping — for tutorials/boss runs.
- **Shipped** — `DistanceRampSelector` — raises `targetDifficulty` as `DistanceTravelled` grows.
- **Shipped** — `WaveSelector` — alternates "calm" and "challenge" stretches by segment index.
- `MarkovSelector` — uses `Connections` as transition weights for authored flow.
- `CurveDrivenSelector` — the same difficulty targeting, but the knobs are `AnimationCurve`s a
  designer draws rather than scalars a programmer picks. See below.

The two difficulty-targeting policies differ only in how they pick the target, so neither restates
the selection pipeline: `WeightedDifficultySelector.SelectByDifficulty` exposes it (connection
filter → `MaxRepeat` gate → difficulty gate → weighted random, plus the ungated and whole-pool
fallbacks) and both delegate to it. A second copy of those fallbacks is the thing that would
quietly drift.

All three non-default policies honour an authored `StartSegmentId` ahead of their own logic — the
first segment is a level's choice, not a difficulty decision — and none is wired to a level by
default. `TrackManager` still constructs `WeightedDifficultySelector`, so behaviour is unchanged
until a selector is chosen deliberately; how that choice gets authored is still the open question
below.

### `CurveDrivenSelector` — curves instead of scalars

`DistanceRampSelector` takes four numbers and produces one shape: a straight line between two
difficulties. Every other shape a designer might want — a plateau in the middle, a spike before a
set piece, a dip right after a hard stretch so the run can breathe — needs another class.

The pipeline only consumes two values, `targetDifficulty` and `difficultyRange`. Make both
`AnimationCurve`s over normalized run progress and the shape stops being a programmer's decision:

```csharp
[SerializeField] private AnimationCurve _difficultyOverProgress = AnimationCurve.Linear(0, 1, 1, 8);
[SerializeField] private AnimationCurve _rangeOverProgress      = AnimationCurve.Constant(0, 1, 2);
[SerializeField] private float          _progressDistance       = 500f;   // distance at which x = 1

public TrackSegmentDefinition SelectNext(ISegmentPool pool, SelectionContext ctx)
{
    float x = _progressDistance <= 0f ? 1f : Mathf.Clamp01(ctx.DistanceTravelled / _progressDistance);
    return WeightedDifficultySelector.SelectByDifficulty(
        pool, ctx, _difficultyOverProgress.Evaluate(x), _rangeOverProgress.Evaluate(x));
}
```

A tightening `_rangeOverProgress` is the part that is hard to get any other way: start loose so the
opening is varied, then narrow the band so the late run reliably serves hard segments instead of
occasionally dropping an easy one in.

**It subsumes the two shipped policies.** A linear `_difficultyOverProgress` reproduces
`DistanceRampSelector` exactly; a repeating curve approximates `WaveSelector`, though not
identically — the wave is keyed to segment *index* and a curve is keyed to distance, so its stretches
would drift with segment length. That difference is the argument for keeping both: index-keyed
rhythm and distance-keyed trend are genuinely different intents, and a curve only expresses the
second. (An index-keyed variant is possible — evaluate at `ctx.SegmentIndex / period` — but then the
curve is a repeating waveform, which is a worse authoring surface than two lengths and two numbers.)

**Determinism is preserved.** `AnimationCurve.Evaluate` is pure and side-effect free, so the target
remains a pure function of `DistanceTravelled` and every draw still comes from `ctx.Random`. Same
seed, same track. This is worth checking rather than assuming, because it is the one property that
would disqualify the idea outright.

**It constrains the authoring question above.** An `AnimationCurve` only reaches the Inspector if
Unity serializes the object holding it, so this rules option 3 out: a `"SelectorPolicy": "weighted"`
string in level data cannot carry a curve. Options 1 (`[SerializeReference]`) and 2
(`ScriptableObject` factory) both work, and option 2 is the better fit — a curve is authored data,
so a `CurveDrivenSelectorAsset` shared across levels puts it where the rest of the level's tuning
already lives.

**Caveats.** `AnimationCurve` is a mutable reference type: a selector must treat its curves as
read-only for the duration of a run, and two selectors sharing one asset must not mutate it.
Evaluation cost is irrelevant here — a few calls per second, not per frame. And like every
difficulty-targeting policy, it is only as good as the pool: with few segments and a narrow spread
of `DifficultyRating`, the gate finds nothing in range and the shared pipeline falls back to ungated
selection, so an elaborately drawn curve reads as flat.

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
