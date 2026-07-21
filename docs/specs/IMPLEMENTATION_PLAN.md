# Implementation Plan: Extension-Point Refactors

A sequenced, **behaviour-preserving** plan to implement the three extension points from the
specs:

- **A. `IPowerUpEffect`** — [STRATEGY_INTERFACES.md § Part B](STRATEGY_INTERFACES.md#part-b--ipowerupeffect)
- **B. `ISegmentSelector`** — [STRATEGY_INTERFACES.md § Part A](STRATEGY_INTERFACES.md#part-a--isegmentselector)
- **C. `IPathSegmentBuilder`** — [PATH_SEGMENT_BUILDER.md](PATH_SEGMENT_BUILDER.md)

They are independent and ship separately. This plan orders them by risk (smallest, safest first)
and defines the checkpoints that prove each one didn't change behaviour.

## Principles (per [../../AGENTS.md](../../AGENTS.md))

- **Interface-first.** Introduce the interface + a default implementation that reproduces today's
  behaviour *exactly*, then swap the call site, then delete the old path. No behaviour change lands
  in the same step as the abstraction.
- **Verify by playing, not by test suites.** Each phase ends with a concrete in-editor check
  (drive the affected flow and observe), not a unit test. A regression is "the run looks/plays
  different," caught by the checkpoint.
- **One seam at a time.** Each of A/B/C is its own branch and its own merge. Prove the seam by
  adding a *second* implementation at the end, not by rewriting behaviour mid-refactor.

## Recommended order

1. **A. `IPowerUpEffect`** — smallest blast radius (one controller, four effects), self-contained.
2. **B. `ISegmentSelector`** — medium; touches `TrackManager` + `TrackSegmentLibrary`, but selection
   is already a clean function call.
3. **C. `IPathSegmentBuilder`** — largest; geometry changes ripple into movement/collision, so do it
   last and scope the first builder tightly.

Each is optional and independently valuable — stop after any of them.

---

## A. `IPowerUpEffect`

**Branch:** `refactor/powerup-effects`
**Files:** `Assets/TempleRun/Scripts/Player/PowerUpBuffController.cs`, new
`Assets/TempleRun/Scripts/PowerUps/` folder, `PowerUpType.cs`, `PowerUpDefinition.cs`, `Blackboard.cs` (read-only touch).

**Phase A1 — introduce types (no behaviour change).**
- Add `IPowerUpEffect`, `PowerUpContext`, and `PowerUpEffectBase` (no-op `Apply`/`Remove`,
  `TryAbsorbObstacle => false`) under `Assets/TempleRun/Scripts/PowerUps/`.
- Nothing calls them yet. Compiles; run unaffected.

**Phase A2 — port the four effects.**
- Create `SpeedBoostEffect`, `CoinMagnetEffect`, `ShieldEffect`, `ScoreMultiplierEffect`, each
  moving its `case` body out of `PowerUpBuffController.ApplyBuff`/`RemoveBuff`.
- `ShieldEffect.TryAbsorbObstacle` publishes `ObstacleRecovered` and returns true.

**Phase A3 — swap the call sites.**
- Build a `Dictionary<PowerUpType, IPowerUpEffect>` in the controller (start with a
  `[SerializeReference] List<IPowerUpEffect>` or reflection — pick per the spec's open question).
- Replace the twin switches with `effect.Apply(ctx)` / `effect.Remove(ctx)`.
- Replace `OnObstacleHit`'s `Blackboard.ShieldActive` check with a loop over active effects'
  `TryAbsorbObstacle`; fall through to `PlayerFailingAtObstacle` if none absorb.
- Delete `ApplyBuff`/`RemoveBuff`.

**Checkpoint A.** Play a run: collect each power-up and confirm its effect (speed, magnet, shield,
score) applies and expires exactly as before; re-collect resets the timer; **Shield absorbs an
obstacle → `ObstacleRecovered`**, no shield → fail. Buffs clear on `TempleRunEnded`.

**Phase A4 — prove the seam.** Add a fifth effect (e.g. `CoinDoublerEffect`) as one new file +
one `PowerUpDefinition` asset. Confirm it works with zero controller edits.

---

## B. `ISegmentSelector`

**Branch:** `refactor/segment-selector`
**Files:** `Assets/TempleRun/Scripts/Track/TrackSegmentLibrary.cs`, `TrackManager.cs`, new
selector classes under `Assets/TempleRun/Scripts/Track/Selection/`.

**Phase B1 — extract `ISegmentPool` (no behaviour change).**
- Add `ISegmentPool` and have `TrackSegmentLibrary` implement it (it already exposes `Segments`,
  by-id lookup, connections, lane config). Keep `SelectNext` for now.

**Phase B2 — extract the algorithm.**
- Add `ISegmentSelector`, `SelectionContext`, and `WeightedDifficultySelector` containing the
  current `SelectNext`/`SelectWeighted`/`IsAllowed`/`IsInDifficultyRange` logic verbatim.

**Phase B3 — swap the call site.**
- `TrackManager` constructs/holds an `ISegmentSelector` (default = `WeightedDifficultySelector`)
  and a `SelectionContext` (thread through `Previous`, `PreviousRepeatCount`, `DistanceTravelled`,
  `SegmentIndex`, seeded `Random`). Call `_selector.SelectNext(pool, ctx)`.
- Decide policy source (SerializeReference / ScriptableObject factory / level-JSON name) — see the
  spec's open question; recommend the ScriptableObject factory with a JSON-name fallback.
- Remove `TrackSegmentLibrary.SelectNext` (or `[Obsolete]` shim for one release).

**Checkpoint B.** Play each of the five levels with a **fixed seed**; confirm the segment stream is
identical to before (same ids in the same order). Verify difficulty gating, `MaxRepeat`, explicit
`Connections`, and the Either/T-junction flow still behave.

**Phase B4 — prove the seam.** Add `AuthoredSequenceSelector` (plays `ActiveSegmentIds` in order)
and point one level at it. Confirm deterministic authored order.

---

## C. `IPathSegmentBuilder`

**Branch:** `refactor/path-builder`
**Files:** `Assets/TempleRun/Scripts/Track/PathProvider.cs`, new builders under
`Assets/TempleRun/Scripts/Track/Geometry/`. **Watch:** `MoveCharacterByDistance`,
`SegmentTransitionController`, `SegmentAdvanceTrigger`, the plane-stretch spawner.

> This is the deep one — geometry assumptions leak into movement/collision. Do A and B first, and
> keep the **first** builder to today's exact behaviour so the movement code is untouched.

**Phase C1 — introduce types + default builder (no behaviour change).**
- Add `PathPose`, `PathSpan`, `PathSegmentResult`, `IPathSegmentBuilder`.
- Implement `AxisAligned90Builder` containing the exact math from `OnTrackCreated` (two-point spans,
  90° snaps, the `± TrackWidthOffset` centering nudge).

**Phase C2 — refactor `PathProvider` onto the builder.**
- Replace `(_anchorPoint, _directionIndex)` with a `PathPose`; call `_builder.Build(...)`.
- Publish one `SplineSegmentData` per returned `PathSpan`; publish `SegmentGeometryData` from
  `PathSegmentResult`.
- Preserve the Either deferral: approach `Build` returns approach spans + placeholder exit;
  `SegmentRequested` resolves the exit (add `BuildExit` if cleaner than re-`Build`).

**Checkpoint C1.** Play a run with the default builder: geometry, turns, teleport landing, spawns,
and player movement are **pixel-identical** to before. This is the gate before any new geometry.

**Phase C3 — first new geometry (contained).**
- Add `ArcTurnBuilder` that emits the exit as a **sampled polyline of short straight spans**
  (approximation path from the spec) so spawners/movement need no changes yet.
- Add optional `TurnRadius` to `TrackSegmentDefinition` (default 0 → falls back to 90° snap).

**Checkpoint C2.** A level using `ArcTurnBuilder` shows rounded corners; the player still follows the
track and turns resolve. If movement drifts on the polyline, that scopes the follow-up (true
arc-length movement) — do **not** expand scope mid-phase.

**Phase C4 (separate effort, optional).** True polyline/curve consumers: teach
`MoveCharacterByDistance` and `SegmentTransitionController` arc-length + 3D heading; then `RampBuilder`
/ `BankedTurnBuilder` become reachable. Spec this as its own doc when wanted.

---

## Cross-cutting

- **Determinism contract.** Selectors and builders must draw only from the seeded `Random` /
  their inputs (no `Time`, no `Math.Random`) so replays reproduce. State it in each interface's XML docs.
- **Audit after each merge.** Run `/audit-events` — these refactors move logic but must not add
  subscriptions without matching `OnDestroy` unsubscribes or leak cross-domain references.
- **Docs.** Update [TRACKS.md](../TRACKS.md) after B and C (selection policy, geometry builders),
  and flip each spec's **Status** from "proposal" to "implemented."
- **Rollback.** Each is an isolated branch off `main`; abandon or revert without touching the others.

## Suggested milestones

| Milestone | Contains | Outcome |
|-----------|----------|---------|
| M1 | A (Phases A1–A4) | power-ups are one-file plug-ins; Shield gate generalized |
| M2 | B (Phases B1–B4) | selection policy swappable; authored-sequence proven |
| M3 | C (Phases C1–C3) | geometry pluggable; rounded corners via approximation |
| M4 *(optional)* | C Phase C4 | true curves/ramps once movement follows arc length |
