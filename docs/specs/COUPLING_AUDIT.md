# Scoping brief: where track/segment vocabulary leaks into player control

**Status:** scoping brief — **the analysis has not been done.** This document exists so the
session that does it starts from evidence rather than a blank page.
**Produce:** a plan, in this folder, in the style of
[SO_MIGRATION.md](SO_MIGRATION.md) / [AWAITABLE_MIGRATION.md](AWAITABLE_MIGRATION.md) —
scope, target design, phasing, verification, documentation fallout.

> **How to use:** paste this whole file into a fresh Claude Code session, or point one at it.
> Read [CLAUDE.md](../../CLAUDE.md) first; the event-system rules are mandatory and this work
> must not weaken them. Tag `pre-coupling-audit` before executing anything, per the repo's
> convention, so every converted file diffs against the before state as a teaching example.

## The two theses

Both are the repo owner's, stated after a long session of turn-system repairs:

1. **Track/segment vocabulary is intermingled with player control.** The player-facing
   controllers reach into track-generation concepts, and track components own player-flow
   decisions. The turn system is where it is worst, but it is not the only place.
2. **`Blackboard` is historic and works, but it smells.** Not a call to delete it — a call to
   say what it is actually for, and to move what does not belong.

## What makes this hard, and worth doing carefully

This is a **teaching repo**. Every seam is course material: students are pointed at these
files by [STUDENT_TASKS.md](../STUDENT_TASKS.md), and the architecture is the subject of a
conference talk ([TALK_OUTLINE.md](../TALK_OUTLINE.md)). A refactor that improves the code but
makes the seams harder to *narrate* is a net loss. Prefer changes that make the existing
lesson sharper over changes that add sophistication.

Corollary: the audit should distinguish **smells that mislead a student** from smells that
merely offend. The first are urgent; the second may be deliberate teaching simplifications and
should be left with a comment saying so.

## Evidence already gathered

Verified against `main` at the merge of PR #27. Each is a symptom; the analysis should decide
which are the same underlying problem.

### Player control reaching into track concepts

| # | Evidence | Where |
|---|----------|-------|
| 1 | `AIController` holds a serialized `TurnController` and reads two live values off it every frame (`TurnFailedDistance`, `TurnDirection`), then subscribes to `ActiveTrackChanging` — a **track-generation** event published by `TrackManager` — as a stand-in for "there is a new turn to consider". | `Player/AIController.cs:22,63,64` |
| 2 | `TrackSegmentInfo` exposes **segment-relative** distances (`TurnPointDistance => Definition.TurnFailureDistance`), but every consumer needs **absolute** ones. The accumulation that converts them lives privately in `TurnController`, which is why anything needing an absolute turn point must go through that component. | `Track/TrackSegmentInfo.cs:19`, `Player/TurnController.cs:106` |
| 3 | `PlayerFailureAutoTurnController` calls `TurnController.ForceTurn()` by C# reference — the one place in `Player/` that commands another controller directly. Already written up in `docs/event-review/event-seam-audit.html`, whose proposed fix is a `TurnForceRequested` event. | `Player/PlayerFailureAutoTurnController.cs:41` |

### Track components owning player flow

| # | Evidence | Where |
|---|----------|-------|
| 4 | `SegmentTransitionController` and `TurnCommitController` live in `Scripts/Track/`, run in the **gameplay** scene, and orchestrate the player's turn. Folder, scene and concern disagree. `TurnCommitController` is new in PR #27 and its placement is unexamined. | `Track/SegmentTransitionController.cs`, `Track/TurnCommitController.cs` |
| 5 | A turn's flow crosses four components and three scenes: gate (`TurnController`, Gameplay) → commit (`TurnCommitController`, Gameplay) → exit spline (`SegmentTransitionController`, Gameplay) → motion (`CharacterTeleporter`, PlayerVisuals), with `PathProvider`/`TrackManager` (TrackPCG) resolving geometry in the middle. Reading it requires all six files open. | see `TempleRunAutoEventFlow.cs` turn block |

### Coordination by convention rather than contract

| # | Evidence | Where |
|---|----------|-------|
| 6 | `MoveCharacterByDistance` and `CharacterTeleporter` both write the player transform. Which one wins is decided by a `Direction.Straight` test inside `MoveCharacterByDistance` — a convention, not a contract, and it is load-bearing: without it the teleport animates from its destination to its destination and is invisible. | `Player/MoveCharacterByDistance.cs:45` |
| 7 | Several behaviours depend on the bus's **breadth-first** delivery and on publish order inside a method. Documented in comments now, but it is coupling through timing rather than through interfaces, and it has already produced one live bug. | `Track/TurnCommitController.cs`, `Events/TempleRunAutoEventFlow.cs` |

### The Blackboard

| # | Evidence | Where |
|---|----------|-------|
| 8 | `Blackboard` mixes three kinds of thing under one name: **config asset references** (`LaneConfig`, `JumpConfig`, …), **live gameplay state** (`JumpHeightOffset`, `CurrentSpeed`, multipliers), and — the sharpest one — **a controller reference**, `LaneChangeController`. That last is how components reach each other without admitting it. | `Config/Blackboard.cs:45,46,50` |
| 9 | There is a precedent for *removing* Blackboard entries rather than adding them: the selected level "originally parked the int on `Blackboard.SelectedLevel`; that mirror was removed once the event became `Sticky`" — a Sticky event plus `TryGetLast` replaced the field. Any proposal to add to the Blackboard should first ask whether a Sticky event does it. | [SO_MIGRATION.md](SO_MIGRATION.md) |

### One worked example, already sketched

A concrete instance of #1 and #2, offered and deferred, useful as a shape to argue with rather
than a conclusion to adopt: a `TurnWindow` struct (absolute `AvailableDistance`,
`FailDistance`, `Direction`) carried by a new `[EventDelivery(Sticky)]` `TurnWindowChanged`,
published by `TurnController` where it already computes all three. `AIController` then
subscribes once — the payload is its data, the arrival is its re-arm — and drops the
`SerializeField`. Per #9, no Blackboard mirror. Free enum range: 44–49, adjacent to the
turning block.

## Non-obvious mechanics — read before judging any of the above

These cost this session real time to establish. None are in the docs.

- **Delivery is breadth-first.** `EventsPublisherInternal.PublishEvent` enqueues one entry per
  subscriber onto a shared queue and drains FIFO; a publish from inside a callback appends to
  the back of that same queue. So *a chained rung always fires before the consequences of the
  source event's other subscribers.* `Publish` returns only once the queue is empty. This is
  why the turn ladder is almost entirely hand-published rather than chained.
- **Grep undercounts publishers.** `TurnController` and `LaneOffsetController` publish through
  a *variable* (`endingEvent`, `completionEvent`), so `Publish(TempleRunEvents.X)` misses them.
  Count *references* to the member, not publish call sites, before calling a rung dead.
- **`TempleRunEvents` values are safe to renumber; `GameFlowEvents` values are not.** No
  component serializes a `TempleRunEvents` field and no scene names one. `GameFlowEvents` is
  serialized in `EventLoggerDump` and `UnloadNonActiveScenes`, and `[EventName]` strings appear
  in scene assets.
- **A new MonoBehaviour needs a manual Inspector step.** Its script GUID does not exist until
  Unity imports the file, so a session cannot wire it into a scene. Any plan that adds
  components must call this out per component, loudly, and say which scene and object — a
  missed one silently breaks the feature it was added for.
- **Compile check without the editor:** `dotnet build Assembly-CSharp.csproj`. The `.csproj` is
  gitignored and regenerated by Unity, so it lags behind file renames.

## What the analysis should produce

1. **A vocabulary map.** Which concepts belong to the player, which to the track, and which are
   genuinely shared. Name the shared ones explicitly — the leak is mostly that shared concepts
   have no home, so they get parked in whichever component noticed them first.
2. **A judgement on each item above:** same root cause as which others, mislead-a-student or
   merely-offends, worth fixing now / worth a student task / leave with a comment.
3. **A target design for the turn system specifically** — it is the worst case and the best
   worked example, and it was just rebuilt, so it is fresh.
4. **A position on `Blackboard`:** what it is for in one sentence, what moves out, and what
   replaces each thing that moves (Sticky event, payload, config injection, or a real
   interface). The controller reference is the test case.
5. **Phasing with verification**, in the style of the other specs in this folder. Every phase
   must end compile-clean and play-testable, and must say which manual scene steps it needs.
6. **Documentation and course fallout:** what changes in `CLAUDE.md`, `docs/EVENTS.md`,
   `docs/ARCHITECTURE.md`, `docs/ADDING_A_MECHANIC.md`, `STUDENT_TASKS.md` (task L-numbers may
   move or be resolved), and the talk.

## Constraints

- The event rules in `CLAUDE.md` are not up for negotiation: no direct cross-system calls, no
  cross-domain event references outside bridges, every subscription unsubscribed in
  `OnDestroy`. If the audit thinks a rule is wrong, say so explicitly and separately — do not
  route around it.
- Prefer fewer, better-named events over more indirection. The repo already carries ~29
  members that nothing reaches (task L13); this work should reduce that number, not raise it.
- Keep public seams that students are pointed at, or update the task text that points at them.
- No behavioural change without a play test. Several defects in this area were invisible in
  code review and obvious within ten seconds of playing.
