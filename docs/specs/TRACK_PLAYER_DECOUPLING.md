# Plan: untangle track vocabulary from player control

> **Status:** plan — ready to execute, not yet started. This is the deliverable
> [COUPLING_AUDIT.md](COUPLING_AUDIT.md) asked for (its "Produce" line), and Phase B of
> [DOMAIN_DECOMPOSITION.md](DOMAIN_DECOMPOSITION.md#8-phasing).
> **Verified at:** `main` @ 7faa742 (2026-09-04). The brief's evidence was gathered at PR #27
> and every item below was re-checked at this commit; where the finding changed, it says so.
> **Owner ruling folded in:** *a domain owns its own data*
> ([DOMAIN_DECOMPOSITION §9 Q5](DOMAIN_DECOMPOSITION.md#new-raised-by-phase-a)). That settles
> §5 — configuration follows the concern that reads it, and shared mutable state must name an
> owner or stop existing.

## Scope

The brief's two theses, restated as what this plan changes:

1. **Track/segment vocabulary is intermingled with player control** → §1 finds one root cause
   under most of it, and §4 fixes the turn system as the worked example.
2. **`Blackboard` is historic and works, but it smells** → §5 says in one sentence what it is
   for, and moves three kinds of thing out of it.

Not in scope, and deliberately: the app-clock finding (`PauseController` writing global
`Time.timeScale`) and the obstacle-lethality finding (`PowerUpBuffController` deciding whether
obstacles kill). Both are real, both are already written up in
`docs/event-review/event-seam-audit.html`, and neither is track/player coupling. They are
cross-referenced where this plan touches their files, and otherwise left alone.

---

## 1. The root cause: absolute position along the run has no owner

The brief lists nine symptoms. Re-checking them at `7faa742` shows that four of them
(#1, #2, part of #4, part of #5) are one defect wearing four costumes.

**The track measures in segment-relative units. Every consumer needs absolute ones. The
conversion is unowned, so each consumer performs it privately.**

`TrackSegmentDefinition` carries four distances — `ToPivotDistance`, `ExitDistance`,
`TeleportDistance`, `TurnFailureDistance` — plus `Length`, and all five are measured from the
*segment's* start. `DistanceTracker.DistanceTravelled` is measured from the *run's* start.
Converting between them needs one number: the distance at which the active segment began.

Nobody owns that number. Three components each rebuild it, independently, from the same event:

| File | Line | The accumulator |
|---|---|---|
| `Player/TurnController.cs` | 102–103 | `_segmentStartDistance += _previousSegmentLength;` |
| `Player/TurnCollisionDetector.cs` | 75–76 | `_currentSegmentInitialDistance += _previousSegmentLength;` |
| `Track/SegmentTransitionController.cs` | 74–75 | `_segmentStartDistance += _previousSegmentLength;` |

All three subscribe to `ActiveTrackChanging` for no other reason. Two of them then compute the
*same derived value* — the absolute distance at which a turn is failed:

```csharp
// TurnController.cs:104
_trackDistance = _segmentStartDistance + trackSegment.TurnPointDistance;
// TurnCollisionDetector.cs:79
_turnFailureDistance = _currentSegmentInitialDistance + trackSegmentInfo.TurnPointDistance;
```

That is the whole of evidence item #2, and it is worse than the brief recorded it: the brief
says the accumulation "lives privately in `TurnController`". It lives privately in three
places, and the fact that they currently agree is a coincidence maintained by hand. The long
comment at `TurnController.cs:98–101` documents a bug that came from one of these
accumulations being subtly different — evidence that keeping them in step is a real cost
already being paid.

**Once the number has an owner, three of the brief's items dissolve rather than get fixed:**

- **#2** disappears: `TrackSegmentInfo` carries absolute distances, so no consumer converts.
- **#1** becomes a deletion: `AIController` holds a `[SerializeField] TurnController` and reads
  `TurnFailedDistance`/`TurnDirection` off it. Both values are on the segment message the
  moment it carries absolutes — so the reference and the extra subscription both go, and **no
  new event is added** (the brief's `TurnWindowChanged` sketch is not needed; see §4).
- **#5's** four-component turn flow loses one of its reasons to exist: `TurnController` and
  `TurnCollisionDetector` stop sharing hidden derived state.

**Who should own it: `TrackManager`, at the publish site.** It is the only component that knows
the queue order and every segment's length, and it already publishes both events that carry a
`TrackSegmentInfo`. One accumulator replaces three, and it lives in the domain that owns the
unit.

---

## 2. Vocabulary map (deliverable #1)

The brief's diagnosis is right: *"the leak is mostly that shared concepts have no home, so they
get parked in whichever component noticed them first."* Naming the shared ones is therefore
most of the work.

**Track-owned** — the shape of the world, meaningless to a player component:
`TrackSegmentDefinition`, `SegmentGeometryData`, `SequenceIndex`, splines and pivots,
the generation lifecycle (`TrackSegment*`, `SplineSegment*`, `SegmentRequested`), pooling,
and the Either-junction commit state.

**Player-owned** — intent and motion, meaningless to a generator: every `*Requested →
*Starting → *Started → *Ending → *Ended` ladder (turn, jump, slide, dash, lane), transform
writing, lane offset, failure and lives, the run clock.

**Genuinely shared — and this is where the leak is.** Four concepts, of which only one has a
home today:

| Shared concept | Home today | Home it should have |
|---|---|---|
| **Absolute distance along the run** | `DistanceTracker` publishes it; the conversion from segment-relative is re-derived in 3 components | **The message.** `TrackSegmentInfo` carries absolutes; `TrackManager` owns the one accumulator (§1) |
| **The turn window** — may I turn, which way, by when have I failed | split between `TurnController`'s privates and `TrackSegmentInfo`'s relatives | **The message.** All three values land on `TrackSegmentInfo`; `TurnController` keeps only the *decision* |
| **Which component owns the player transform right now** | a `Direction.Straight` test, re-derived in `MoveCharacterByDistance` and `TeleportController` independently (item #6) | **The message.** A named property on the spline payload (§3, item 6) |
| **`Direction`** | a shared enum, referenced by both sides | fine as-is — genuinely shared vocabulary, correctly homeless |

The pattern is worth stating because it is the lesson: **three of the four leaks are the same
mistake — a derived fact that several components need, computed by each of them instead of
carried on the message that already reaches all of them.** Shared *data* belongs on the event
payload. Shared *decisions* belong to one owner. The Blackboard is where this project has been
putting both, which is §5.

### The would-be TrackPCG bridge table

[DOMAIN_DECOMPOSITION §5](DOMAIN_DECOMPOSITION.md#5-candidate-trackpcg--right-split-wrong-moment)
asks that this map double as the bridge table a TrackPCG domain split would need, with every
row passing the translate-don't-relay test. **After** the phases below, it is this short:

| TrackPCG publishes | → | Player domain receives | Contract |
|---|---|---|---|
| `ActiveTrackChanging(TrackSegmentInfo)` | → | `TurnWindowChanged(TurnWindow)` | the window moved; absolute distances, direction |
| `SplineSectionChanging(SplineSection)` | → | `PathChanging(SplineSection)` | run along this, and who owns the transform |
| `SegmentGeometryReady`, `SplineSegmentCreated` | → | *(not bridged)* | visuals/spawners only — track-internal |
| — | ← | `TurnCommitted(Direction)` → `SegmentRequested` | the player chose an exit at a junction |
| — | ← | `DistanceUpdated(float)` → track interest | the only continuous signal the track needs |

Four rows, each a contract rather than an apology. **This table is the acceptance test for the
phases below** — if executing them does not leave the crossings looking like this, the phases
were wrong, not the table. Note that today the same table would need ten rows and three of them
would carry a component reference; that is the measure of what this plan buys.

Both renames in the table are **Phase C work, not this plan's** — today `ActiveTrackChanging`
and `CurrentSplineChanging` stay as they are. This plan makes their payloads sufficient; the
split, if it ever happens, renames them at the bridge.

---

## 3. Verdict on each item (deliverable #2)

The brief asks for three judgements per item: same root cause as which others; does it
**mislead a student** or merely offend; and fix now / student task / leave with a comment.

| # | Item | Root cause | Misleads? | Verdict |
|---|---|---|---|---|
| 1 | `AIController` holds a `TurnController` + listens to a generation event | §1 | **Yes** — it teaches "reach into a controller, and listen to a track event for a gameplay cue". It is the file students are pointed at as the *proof* input is replaceable | **Fix now** (Phase 1) — becomes a deletion, not a rewrite |
| 2 | Segment-relative vs absolute distances | **is** §1 | **Yes** — three copies of one accumulator is a pattern a student will copy | **Fix now** (Phase 1) |
| 3 | `PlayerFailureAutoTurnController` calls `TurnController.ForceTurn()` | own cause: a missing event | **Yes** — a literal violation of CLAUDE.md rule 1, in a repo whose first rule that is | **Fix now** (Phase 3) — `TurnForceRequested = 44`, per the seam audit's already-agreed value |
| 4 | Turn controllers live in `Scripts/Track/` | §1 (partly) + naming | Mildly | **Fix now** (Phase 3) — a file move with its `.meta`; costs nothing, and §2's map makes the right folder obvious |
| 5 | A turn crosses four components and three scenes | §1 (partly) | **No** — this is the architecture working as designed | **Leave, and narrate.** After Phase 1 it crosses four components sharing *no* hidden state. Document it in ADDING_A_MECHANIC; do not restructure |
| 6 | `Direction.Straight` decides which component writes the transform | §1's shape (a derived fact re-derived) | **Yes** — load-bearing and invisible; the comment at `MoveCharacterByDistance.cs:40–44` exists because it was learned the hard way | **Fix now** (Phase 2) — name it on the payload |
| 7 | Behaviour depends on breadth-first delivery order | own cause: undocumented runtime mechanics | **Yes**, by omission — the mechanic is not in any doc | **Leave the code; fix the docs** (Phase 6). It is also the precondition for a TrackPCG split, so it must be written down before Phase C is considered |
| 8 | `Blackboard` mixes config, state and a controller reference | own cause | **Yes** — the controller reference teaches components to reach each other through a global | **Fix now** (Phase 4) — see §5 |
| 9 | Precedent: remove Blackboard entries rather than add | — | — | **Adopt as a rule** (§5) |

### Three findings the brief does not have

Re-verification at `7faa742` turned up three things worth more than several of the items above.

**10. Two of the five power-ups are inert, and a third writes a field nobody reads.** Every
`Blackboard` power-up field is written and never read:

| Field | Written by | Read by | Consequence |
|---|---|---|---|
| `ActiveSpeedMultiplier` | `SpeedBoostEffect` | **nobody** | `DistanceController:98` multiplies by dash and slide only — **SpeedBoost does nothing** |
| `CoinMagnetActive`, `CoinMagnetRadius` | `CoinMagnetEffect` | **nobody** | there is no attraction logic anywhere — **CoinMagnet does nothing** |
| `ShieldActive` | `ShieldEffect` | **nobody** | Shield *works*, but through `TryAbsorbObstacle`; the field is vestigial |
| `ActiveScoreMultiplier` | `ScoreMultiplierEffect`, `CoinDoublerEffect` | `CoinCollectionController:38` | works |

**Misleads a student, badly.** `IPowerUpEffect` is advertised as the repo's extension seam, and
`CoinDoublerEffect`'s own comment says it "proves the extension seam" — which it does, while two
shipped siblings silently do not work. A student adding a sixth effect will follow
`SpeedBoostEffect`, the broken one, because it is the first in the list. Fixed in Phase 4.

**11. The 320-block is dead; the legacy prefixed block is live.** This inverts what
[DOMAIN_DECOMPOSITION §3](DOMAIN_DECOMPOSITION.md#3-bridge-vocabulary-translate-dont-relay)
assumed. Reference counts outside the enum:

| Legacy (to retire) | files | Native replacement | files |
|---|---|---|---|
| `TempleRunDifficultySettingsApplied` (310) | 4 | `DifficultySettingsApplied` (320) | **0** |
| `TempleRunDifficultyChanging` (312) | 2 | `DifficultyChanging` (321) | **0** |
| `TempleRunDifficultyChanged` (314) | 1 | `DifficultyChanged` (322) | **0** |
| `TempleRunDifficultyChangeFailed` (316) | **0** | `DifficultyChangeFailed` (323) | 1 |
| `TempleRunDifficultyChangeRequested` (318), `TempleRunConfigApplied` (300) | 3, 1 | — | — |

So the "migration onto the 320-block" is not a migration: the 320-block was declared and never
wired. It is a **rename of the live members plus a delete of the unwired duplicates** — strictly
simpler, and it *reduces* the dead-member count by three instead of leaving them. Phase 5.

**12. Dead-member baseline: 28.** `TempleRunEvents` has 121 members; 28 have no
`TempleRunEvents.X` reference outside the enum (the brief's "~29", confirmed). The brief's
"reduce, don't raise" constraint is scored against this number in §6.

---

## 4. Target design: the turn system (deliverable #3)

### What changes

**One field is added to the message, and eight private fields are deleted.**

```csharp
// Track/TrackSegmentInfo.cs — the struct gains the run-absolute origin of this segment,
// set once by TrackManager when the segment is created.
public float StartDistance;

// and the four relative distances gain absolute siblings, computed not accumulated:
public float TurnFailureDistance => StartDistance + (Definition?.TurnFailureDistance ?? 0f);
public float PivotDistance       => StartDistance + (Definition?.ToPivotDistance     ?? 0f);
public float ExitDistanceAbsolute=> StartDistance + (Definition?.ExitDistance        ?? 0f);
public float EndDistance         => StartDistance + Length;
```

The segment-relative values stay exactly where the track already reads them — on
`Definition`, which is what the geometry builders (`ArcTurnBuilder`, `AxisAligned90Builder`) and
`VoxelLaneTrackSpawner` use today. Nothing on the track side changes. *Naming note:* today's
`TurnPointDistance` accessor has three consumers (`TurnController:104`,
`TurnCollisionDetector:79`, `SegmentTransitionController:143`) and **all three immediately add a
segment-start accumulator to it** — there is no consumer that wants it relative. It is therefore
renamed to the absolute form rather than duplicated, so no call site is left choosing between two
similar names.

`TrackManager` gains one accumulator at segment creation — the point in the code that already
knows the queue and the lengths — and stamps `StartDistance` on each `TrackSegmentInfo` as it is
built. Both `TrackSegmentCreated` and `ActiveTrackChanging` then carry a message that is
complete on its own.

### What is deleted

| File | Deleted |
|---|---|
| `Player/TurnController.cs` | `_segmentStartDistance`, `_previousSegmentLength`, `_trackDistance`; `OnTrackChanging` shrinks to two assignments |
| `Player/TurnCollisionDetector.cs` | `_currentSegmentInitialDistance`, `_previousSegmentLength` |
| `Track/SegmentTransitionController.cs` | `_segmentStartDistance`, `_previousSegmentLength` |
| `Player/AIController.cs` | `[SerializeField] private TurnController _turnController` **and** its `ActiveTrackChanging` subscription's second job |

`AIController` after Phase 1 reads its two values off the segment message it already receives:

```csharp
private void OnTrackChanging(string eventName, object sender, object data)
{
    var segment = (TrackSegmentInfo)data;
    _turnUnderway = false;
    _failDistance = segment.TurnFailureDistance;   // already absolute
    _direction    = segment.Direction;
}
```

That is the whole fix for item #1. **No new event, no new payload type, one fewer serialized
reference** — which is why this is preferred over the brief's `TurnWindow` /
`TurnWindowChanged` sketch. The sketch was right that the three values belong together and
right that `AIController` should get them by subscription; it was one step short in assuming
they needed a new event to travel on. They already have one. Per the "prefer fewer, better-named
events" constraint, adding `TurnWindowChanged` now would raise the member count to carry data
that an existing message can carry. It reappears in §2's bridge table only as the *rename* of
`ActiveTrackChanging` at a future domain boundary — where it is a translation, not an addition.

### What stays

`TurnController` remains the gate and nothing else: it still owns `_turnAvailableDistance` (the
window's near edge, which is a *decision* about safe distance, not shared data) and still
publishes exactly one rung. `TurnCommitController` is untouched by Phase 1 — its ordering
comment at lines 17–26 stays true, because §1 changes no publish order.

---

## 5. Position on `Blackboard` (deliverable #4)

> **`Blackboard` is the run's mutable state of record: the values that describe the run
> currently in progress, are written by one component, read by several, and must outlive any
> one component's lifetime.**

Everything that fails that sentence moves out. Under the owner's ruling that *a domain owns its
own data*, "moves out" means "moves to the concern that owns it" — not to another global.

| What | Fails the sentence because | Replaced by |
|---|---|---|
| `LaneChangeController` (the controller reference) | it is not a value; it is one component reaching another through a global | **The float it is used for.** All three readers want `LateralLaneOffset`. `Blackboard.LateralLaneOffset` is written by `LaneChangeController`, read per-frame by `MoveCharacterByDistance` and `CharacterTeleporter` — a per-frame value is exactly what this class is *for* |
| `CoinMagnetActive`, `CoinMagnetRadius`, `ShieldActive` | written, never read (finding #10) | **Deleted.** Per item #9, a field with no reader does not get a Sticky event either; it gets removed |
| `ActiveSpeedMultiplier` | written, never read — but the feature is advertised | **Wired**, not deleted: one term added at `DistanceController:98` beside the dash and slide multipliers it was always meant to sit with |
| `LaneConfig`, `JumpConfig`, `SlideConfig`, `DashConfig`, `CoinConfig` (asset refs) | they are configuration, not run state — and by the Q5 ruling, config follows its mechanic | **Serialized on the controller that reads each** — but see the deferral below |

**The config references are deferred to Phase 7 and recommended as a student task.** They fail
the sentence, so the ruling says they should move; but each move is a serialized field on a
prefab or scene object, which means a manual Inspector step per config per host, and the win is
tidiness rather than capability. Moving them is a *good* exercise — it is mechanical, verifiable
by play test, and teaches the ruling — and a poor use of a maintainer's afternoon. It is written
up as such rather than silently dropped.

What remains in `Blackboard` afterwards is coherent and passes its own sentence: `GameConfig`,
`DistanceTracker`, `CurrentSpeed`, `TrackWidthOffset`, `TileLength`, `MasterRandom`, the four
live offset/multiplier values, `SessionCoinCount`, and `LateralLaneOffset`.

One thing this does **not** do: `Blackboard.GameConfig` is written by `OnDifficultyChanging`,
which publishes `TempleRunDifficultyChanged` from inside a subscriber (lines 111–117). That is
legal and deliberate, and Phase 5 renames the two members without touching the shape.

---

## 6. Phasing (deliverable #5)

Every phase ends compile-clean (`dotnet build Assembly-CSharp.csproj`), play-tested from
`0_BootStrap_Game_Only` with event logging on, and `/audit-events`-clean.

> **Manual Inspector steps: none, in any phase.** This is deliberate and worth stating loudly,
> because it is unusual here. No phase adds a MonoBehaviour, and the one file move (Phase 3)
> carries its `.meta`, so every script GUID and every scene reference survives. The only
> serialized change is a *removal* (`AIController._turnController`), which Unity drops
> silently and safely. Phase 7, if taken, is the exception and says so.

**Phase 1 — absolute distances on the message.** (§1, §4 — the root cause.)
`TrackSegmentInfo` gains `StartDistance` and absolute accessors; `TrackManager` stamps it at
creation; the three accumulators and `AIController`'s serialized reference are deleted.
*Verify:* the turn window must land in the same place as before — play a run with event logging
and confirm `PlayerFailingAtTurn` fires at the same distances as a `pre-coupling-audit` run, and
that the AI still turns. The Either junction is the sharp case: confirm a T-junction still
commits, since its geometry is re-resolved after creation. **Net events: 0.**

**Phase 2 — name the transform-ownership rule.** (item #6.)
Replace the `(Vector3, Vector3, Direction, float)` tuple carried by `CurrentSplineChanging` /
`CurrentSplineChanged` with a `SplineSection` struct whose members are named, and which exposes
the convention as a property (`TeleportOwnsTransform => Direction != Direction.Straight`), so
`MoveCharacterByDistance:45` and `TeleportController:35` test *one named thing* instead of
re-deriving the same rule. *This also retires a documented gotcha:* CLAUDE.md currently teaches
the four-element tuple cast as the example of a tuple payload. **Net events: 0.**
*Verify:* turns land the player in-lane with no snap — the exact defect the comment at
`MoveCharacterByDistance.cs:40–44` records.

**Phase 3 — remove the last direct call, and move two files.** (items #3, #4.)
Add `TurnForceRequested = 44` (the seam audit's agreed value; 50–59 is full);
`PlayerFailureAutoTurnController` publishes it after its delay, `TurnController` subscribes and
calls its own `ForceTurn()`. Move `TurnCommitController.cs` from `Track/` to `Player/` **with its
`.meta`**. `SegmentTransitionController` stays in `Track/` — §2's map puts it on the track side
as the translator from geometry to path. **Net events: +1.**
*Verify:* fail a turn deliberately; the auto-turn still fires after the delay, and the event log
now shows the failure system's own identity rather than `TurnController`'s.

**Phase 4 — Blackboard.** (§5, items #8, #10.)
Controller reference → `LateralLaneOffset`; delete the three dead power-up fields; wire
`ActiveSpeedMultiplier` into `DistanceController`. **Net events: 0.**
*Verify:* lane offset survives a turn (the `CharacterTeleporter.LaneOffset` path); collect a
SpeedBoost and confirm the run actually accelerates — a behaviour change, so it needs its own
play test. CoinMagnet stays inert by design and becomes a student task (§7).

**Phase 5 — retire the difficulty prefixes.** (finding #11; this is DOMAIN_DECOMPOSITION's
Phase B rename batch, now known to be simpler than assumed.)
Rename the four live `TempleRunDifficulty*` / `TempleRunConfigApplied` members to their native
forms, delete the three unwired 320-block duplicates and the one dead legacy member.
**Net events: −4.** *Verify:* difficulty selection still applies; `Blackboard.GameConfig` still
receives its table.

**Phase 6 — write down the runtime mechanics.** (item #7.) No code. The breadth-first delivery
rule, the "grep undercounts publishers" trap, and the renumbering asymmetry are established
facts that exist only in the brief and in scattered comments. They go in EVENTS.md (§7).

**Phase 7 — config injection (recommended as a student task, not scheduled).** The five config
asset references leave `Blackboard` for the controllers that read them. **This is the one phase
with manual Inspector steps** — one serialized field per config per host object — and they must
be enumerated in its own spec before anyone starts.

### Scoring against "reduce, don't raise"

Dead members today: **28** of 121. Phase 3 adds one member that has both a publisher and a
subscriber, so it is not dead. Phase 5 deletes four members, all of them currently dead — the
three unwired 320-block duplicates plus `TempleRunDifficultyChangeFailed` (316) — and renames
the live legacy members into the freed names.

**Dead: 28 → 24. Total enum: 121 → 118** (−4 from Phase 5, +1 from Phase 3). Every phase meets
the constraint individually except Phase 3, whose +1 buys the removal of the repo's only direct
cross-controller call.

### RUGS

Phases 1, 2 and 4 are ordinary gameplay changes and should port cleanly. Phase 3 touches no
auto-event flow (the new member is subscribed, not chained) but **does add an enum member**, so
it needs the same review the Countdown domain addition got. Phase 5 renames members that RUGS
may bridge to its UGS domain — check `RunnerUGSTemplate`'s bridge tables before renaming, and
port the two together or not at all.

---

## 7. Documentation and course fallout (deliverable #6)

| Document | Change | Phase |
|---|---|---|
| **CLAUDE.md** | The tuple-payload example (`var (point1, point2, direction, _) = ...`) is retired — replace with the `SplineSection` cast. Key-files table: `TurnCommitController` moves to the Player Controllers row | 2, 3 |
| **docs/EVENTS.md** | `TurnForceRequested`; the difficulty renames and deletions; `TrackSegmentInfo`'s new payload shape. **New section: runtime delivery mechanics** — breadth-first drain, publish-from-inside-a-callback ordering, why grep undercounts publishers, and the TempleRun/GameFlow renumbering asymmetry | 2–6 |
| **docs/ARCHITECTURE.md** | **Probably nothing.** Checked: "Where things live" is a four-line top-level tree that names no controller, and there is no turn-flow diagram to update. Re-read "Track generation (summary)" after Phase 1 and confirm it still reads true | 1 |
| **docs/ADDING_A_MECHANIC.md** | No forced change — checked, it carries no payload cast. **Worth adding** as new guidance: the §2 rule, *shared derived data goes on the payload; shared decisions get one owner*. It is the lesson Phase 1 exists to teach and the walkthrough is where a student would look for it | 1, 2 |
| **docs/STUDENT_TASKS.md** | L13's dead-member list drops from 28 to 24 (§6). **New task:** implement `CoinMagnetEffect` — it has a type, an asset hook, and a registered effect, and does nothing; a genuinely useful exercise with a visible result. **Revise** any task pointing at `AIController._turnController` or at `TurnController.ForceTurn()` as a public seam — both are gone | 1, 3, 4, 5 |
| **docs/KNOWN_ISSUES.md** | Add the two inert power-ups if they are not fixed in the same PR as they are documented | 4 |
| **docs/TALK_OUTLINE.md** | §1 is a better story than the one currently in the coupling slide: *three components independently recomputing the same number, because the message they all received was missing it.* That is the architecture's failure mode stated in one sentence, and its fix is a field | 1 |
| **docs/specs/COUPLING_AUDIT.md** | Status → analysis complete, pointing here | on merge |
| **docs/specs/DOMAIN_DECOMPOSITION.md** | Phase B → in progress/complete; §2's bridge table is the Phase C gate | on merge |

---

## 8. Open questions

1. **Phase 1's Either-junction case.** A T-junction's geometry is re-resolved after creation
   (`SegmentGeometryReady` republishes for the active segment). Its *length* should not change,
   so a creation-time `StartDistance` should stay valid — but this is asserted from reading, not
   from a play test. It is the one place Phase 1 could be wrong, and it is the first thing its
   play test must exercise.
2. **CoinMagnet: implement, or delete the type?** Recommended: keep and make it a student task —
   `PowerUpType` values are serialized in `PowerUpDefinition` assets, so removing the enum member
   is the more disruptive option, and an advertised-but-unimplemented power-up is a better
   exercise than a missing one. Needs the owner's call before Phase 4 documents it as such.
3. **Does Phase 7 happen at all?** It follows from the Q5 ruling, so the ruling says yes
   eventually. The question is only whether a maintainer does it or a student does.
4. **Phase C gate.** Nothing here answers whether TrackPCG should be extracted — but §2's table
   is now the evidence for that decision, and Phase 6 answers the cross-bus timing precondition
   that DOMAIN_DECOMPOSITION §2 requires before anyone tries.
