# Spec: `IPathSegmentBuilder` — Generalizing Track Geometry

**Status:** proposal / design exploration (not implemented)
**Scope:** replace the hard-wired axis-aligned 90°-turn geometry in `PathProvider` with a
pluggable geometry builder, so curved turns, ramps, banking, and arbitrary angles become
possible without rewriting the generator.

Design doc in the spirit of [../../AGENTS.md](../../AGENTS.md) — this is the richest "novel
ideas" surface in the track system. Treat the interface below as a starting point to argue with.

## Motivation

`PathProvider` (`Assets/TempleRun/Scripts/Track/PathProvider.cs`) is explicitly labelled a "pure
geometry builder," but the geometry it can express is fixed:

- `_directionAxes = { +Z, +X, -Z, -X }` — four cardinal directions only.
- Turns rotate `_directionIndex` by ±1 mod 4 — **90° only**.
- Every sub-spline is a **straight line** between entrance/pivot/exit.

That means no curved corners, no ramps/verticality, no banking, no diagonal or arbitrary-angle
turns, no S-bends. All of those are desirable runner-track features, and none are reachable by
tweaking data — they require changing the generator. Making the *geometry* pluggable turns them
into strategies.

## Current state

For each `TrackSegmentCreated`, `PathProvider.OnTrackCreated` switches on `Direction` and:

- **Straight:** publishes one spline `entrance → entrance + ToPivotDistance·axis`.
- **Left/Right:** publishes an approach spline, calls `ApplyTurn` (rotates the index and nudges
  the anchor by the track-width centering offset), then publishes an exit spline in the new axis.
- **Either:** publishes only the approach, stashes the definition, and resolves the exit later on
  `SegmentRequested` (the player's chosen `Direction`).

It also owns the running `_anchorPoint`, `_directionIndex`, and `_sequenceIndex`, and publishes
`SplineSegmentData` (for spawners) and `SegmentGeometryData` (for the transition controller).

The `Direction`-switch + axis-index math is exactly the part that hard-codes "axis-aligned 90°."

## Proposed interface

Separate **"where am I and which way am I heading"** (a pose the provider still owns) from
**"what shape does this segment trace"** (the pluggable builder).

```csharp
namespace CrawfisSoftware.TempleRun.Track
{
    /// Position + heading on the track. Heading is a unit vector (not an index),
    /// so builders are free to leave the 4-direction grid.
    public readonly struct PathPose
    {
        public readonly Vector3 Position;
        public readonly Vector3 Forward;   // unit; XZ today, but 3D-ready for ramps
        public readonly Vector3 Up;        // unit; enables banking later
        public PathPose(Vector3 pos, Vector3 fwd, Vector3 up) { Position = pos; Forward = fwd; Up = up; }
    }

    /// One or more polyline/curve spans that realize a segment, plus the pose to continue from.
    public readonly struct PathSegmentResult
    {
        public readonly IReadOnlyList<PathSpan> Spans;  // approach span(s) + exit span(s)
        public readonly PathPose ExitPose;              // where the next segment starts
        public readonly Vector3  Pivot;                 // turn/placeholder point (for geometry data)
    }

    /// A span the spawners/visuals consume. Straight => 2 points; curved => sampled polyline.
    public readonly struct PathSpan
    {
        public readonly IReadOnlyList<Vector3> Points;  // >= 2, ordered start..end
        public readonly Direction EndDirection;
        public readonly TrackSegmentDefinition Definition;
    }

    /// The pluggable geometry policy. Pure function of (entry pose, segment, chosen direction).
    public interface IPathSegmentBuilder
    {
        PathSegmentResult Build(PathPose entry, TrackSegmentDefinition segment, Direction resolved);
    }
}
```

`PathProvider` keeps the orchestration (subscriptions, the running pose, sequence indices, the
Either deferral, and publishing `SplineSegmentCreated`/`SegmentGeometryReady`). It delegates
*shape* to `IPathSegmentBuilder.Build`.

The default `AxisAligned90Builder : IPathSegmentBuilder` reproduces today's behaviour exactly:
two-point spans, 90° heading snaps, the track-width centering nudge. Nothing changes by default.

## What this unlocks

| Builder | Effect |
|---------|--------|
| `AxisAligned90Builder` | today's behaviour (default) |
| `ArcTurnBuilder` | rounded corners — sample an arc of `TurnRadius` into a polyline exit span |
| `FreeAngleBuilder` | turns of any angle (segment carries `TurnAngle`), off the 4-dir grid |
| `RampBuilder` | uses `PathPose.Up` / a `Slope` field to climb/descend (verticality) |
| `BankedTurnBuilder` | rotates `Up` through a turn for banked corners |
| `SBendBuilder` | two opposing arcs for a chicane |

Curved builders emit sampled polylines; a `PathSpan.Points` count > 2 is the only change
spawners/visuals must tolerate (see risks).

## Data additions (optional, per-builder)

Builders read extra fields off `TrackSegmentDefinition` as needed — all optional, default-zero,
so the registry stays backward-compatible:

- `TurnRadius` (arc turns), `TurnAngle` (free-angle), `Slope`/`RiseHeight` (ramps),
  `BankAngle` (banked turns).

Because `JsonUtility` ignores unknown keys and defaults missing ones, existing level/registry
JSON keeps working; a builder simply reads `0` and behaves like the straight/90° default.

## Migration path (behaviour-preserving)

1. Introduce the interface types and `AxisAligned90Builder` containing the current
   `OnTrackCreated` switch math (adapted to return spans rather than publishing directly).
2. Refactor `PathProvider` to: hold a `PathPose` instead of `(_anchorPoint, _directionIndex)`;
   call `_builder.Build(...)`; publish a `SplineSegmentData` per returned `PathSpan`; publish the
   `SegmentGeometryData` from `PathSegmentResult`.
3. Preserve the Either flow: on the approach, `Build` with `Direction.Either` returns approach
   spans + a placeholder exit pose; on `SegmentRequested`, `Build` the exit with the chosen
   direction (or add a `BuildExit` for the deferred half).
4. Confirm a run is pixel-identical with the default builder, then drop in `ArcTurnBuilder` to
   prove curved geometry end-to-end.

## Trade-offs, risks & open questions

- **Polyline consumers.** Today `SplineSegmentData` is a single start→end pair and downstream math
  (`SegmentVector`, `UnitDirection`, `Perpendicular`, the plane-stretch spawner) assumes a
  straight span. Curved spans mean either (a) emitting many short straight spans to approximate a
  curve (works with zero consumer changes — recommended first step), or (b) teaching consumers to
  handle true polylines/curves (cleaner, more work). Start with (a).
- **Player movement & collision.** `MoveCharacterByDistance`, the turn/teleport handling, and
  `SegmentTransitionController` assume straight sub-splines and axis-aligned turns. Curved/ramped
  geometry needs those to follow arc length and 3D headings — the real depth of this feature lives
  here, not in the builder. Scope the first builder (arc turns on flat ground) to keep movement
  changes contained.
- **Heading as vector vs index.** Moving from `_directionIndex` (mod 4) to a `Forward` vector is
  what frees the grid, but it loses the cheap exactness of cardinal snapping. Keep an
  `AxisAligned90Builder` that snaps, so grid-based levels stay exact.
- **Track-width centering.** The `± TrackWidthOffset` nudge at corners is currently baked into
  `ApplyTurn`. It should move into the builder (each geometry has its own correct offset), or
  become a shared helper builders call.
- **Determinism.** Builders must be pure functions of their inputs (no `Random`, no time) so
  geometry is reproducible for a given seed/segment stream.

## Relationship to the segment-selector spec

Orthogonal and composable: [`ISegmentSelector`](STRATEGY_INTERFACES.md) decides *which* segment
comes next; `IPathSegmentBuilder` decides *what shape* that segment traces. A level could pair a
`DistanceRampSelector` with an `ArcTurnBuilder` independently.
