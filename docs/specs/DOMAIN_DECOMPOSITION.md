# Analysis: decomposing the two big event domains

**Status:** analysis — no code change proposed for immediate execution. This document was
requested as a prerequisite to executing [COUPLING_AUDIT.md](COUPLING_AUDIT.md): before
deciding how track vocabulary untangles from player control, decide what the *domain map*
should eventually look like, so the audit refactors toward it rather than past it.
**Question answered:** should `GameFlowEvents` be broken into several domains (level
selection, countdown, …) and `TempleRunEvents` likewise (movement, track PCG, …)? Where
does the countdown belong, and what should its seams *say*?
**Evidence base:** the full member-reference map of both enums (every
`<Enum>.<Member>` occurrence in `Assets/**/*.cs`), both chain tables, both bridge tables,
the serialized event references in scenes, the `/add-event-domain` decision gate, and the
countdown writeup in [KNOWN_ISSUES.md](../KNOWN_ISSUES.md). Verified at tag
`pre-coupling-audit` (= `main` @ c921670).

> **Freshness (reconciled at `main` @ a9ae5a2, 2026-09-04).** Phase A shipped, so this is
> no longer pure analysis: **§4 and §8's Phase A now record what was built**, not what was
> proposed, and §3's table carries each rename's outcome. §5, §6 and §7 were re-checked
> against `a9ae5a2` and are **unchanged** — in particular all ten of §5's straddling files
> still straddle, so the TrackPCG argument stands as written. Where a claim below is dated
> to the original analysis rather than re-verified, it says so. §1 and §2 still describe
> the starting point as it was at `pre-coupling-audit` — three domains, the countdown
> members at 30–36 in `TempleRunEvents`, the old mirror names — and are left as the
> baseline the analysis reasons from.

## TL;DR of the verdicts

| Candidate | Verdict | Why (one line) |
|---|---|---|
| **Countdown → its own domain** | **Yes — ✅ done** (PR #30, `main` @ a9ae5a2) | Zero straddling files, ~7 events, resolves all three KNOWN_ISSUES countdown smells structurally — with a *translated* seam (`CountdownEnded → PlayerActivateRequested`), not a relayed one — and gives the course its missing `/add-event-domain` worked example |
| **TrackPCG → its own domain** | **Yes — but only *after* the coupling audit** | It is the right end state and the audit's best acceptance test; splitting now would freeze today's turn/track tangle into a bridge |
| **Movement → its own domain(s)** | No | Movement *is* the gameplay; the replaceability seam already exists at `UserInitiatedEvents`; jump/slide/dash/lane are healthy categories |
| **GameFlow → UI / Session / Scenes domains** | No | The categories are one choreography, not separate contexts — 8 of ~12 files would become multi-domain actors, and `GameState` observes five of the candidate domains |
| **Level selection / progression → its own domain** | Not now; plausible later as "Meta" | Defensible stub test, but it drags session and menu choreography with it today; in RUGS this concern belongs with the UGS domain |

Recommended order: ~~**(A)** extract Countdown, introducing the `PlayerActivate*` ladder in
TempleRun~~ **— done** → **(B) ← in progress:** execute the coupling audit with the future TrackPCG
bridge table as its target design, folding in what remains of the §3 vocabulary renames →
**(C)** extract TrackPCG (or leave C as a capstone student task). Details in
[Phasing](#8-phasing).

## 1. What we actually have (correcting the framing)

Three domains exist: `GameFlowEvents` (74 members, 11 categories), `TempleRunEvents`
(125 members, ~24 categories), `UserInitiatedEvents` (9 members). Two clarifications the
question was asked around:

1. **The countdown enum members are already in TempleRun** (`CountdownStartRequested`…
   `CountdownCancelled`, values 30–36). What lives on the GameFlow side is the *trigger*
   (`GameStarting → CountdownStartRequested` in the bridge), the *handshake back*
   (`CountdownEnded → GameStarted`), and the visual asset
   (`Assets/GameFlow/UI Toolkit/UI/UXML/Overlays/Countdown.uxml`). So "move Countdown to
   TempleRun" is already true of the code; the smell is that the *boundary traffic* and the
   assets say otherwise. KNOWN_ISSUES documents this as three misplacements.
2. **A category is not a domain.** Inside one enum, categories share a bus, and a
   subscriber crossing categories costs nothing and is invisible. A domain split makes
   every crossing a named bridge mapping — that is its value (visibility, replaceability,
   stub-ability) and its cost (bridge classes, scene hosting, registration). The decision
   gate in `.claude/skills/add-event-domain/SKILL.md` is the repo's own bar: (1) separate
   concern with its own lifecycle, (2) the stub test — could a trivial fake sit in its
   place? — and (3) it will grow a family of events. This analysis applies that gate to
   each candidate.

## 2. What a split costs — the fixed overhead, measured here

Every extraction pays these regardless of size:

- **A bridge class per adjacent domain** (two dispatcher tables), plus optionally an
  auto-flow class — each a new MonoBehaviour that **requires a manual Inspector step** to
  host in a scene whose lifetime matches the domain (script GUIDs don't exist until Unity
  imports the file; a session cannot wire them). Every phase below names its scene and
  object.
- **Registration in ~12 places** (`add-event-domain` Step 7): CLAUDE.md's four tables,
  five skills, GEMINI.md + copilot-instructions, ARCHITECTURE.md, EVENTS.md, README.md.
- **Serialized references.** Measured, the exposure is small and precisely located:
  - Type-qualified event strings in scenes — exactly three in the whole project:
    `GameFlowEvents/GameScenesLoaded` + `GameFlowEvents/GameEnded`
    (`FireEventAfterSceneLoads` in `TempleRunGameplay.unity`) and
    `TempleRunEvents/SplineSegmentCreated` (`TempleRunTrackPCG.unity`). Moving a member to
    a new enum breaks its string; these three are the checklist.
  - `GameFlowEvents` fields serialized as ints in `EventLoggerDump` and
    `UnloadNonActiveScenes` — GameFlow members must not renumber; `TempleRunEvents` members
    are safe to renumber, rename, or remove (established during the turn renumbering).
- **Cross-bus timing.** Each `EventsFor<T>` drains its own breadth-first queue. Several
  behaviours (the turn commit especially — COUPLING_AUDIT item 7) depend on delivery order
  *within one bus*. Any split that cuts through a synchronous choreography must first
  verify what a publish onto a second bus from inside the first bus's drain actually does.
  This is a hard precondition for the TrackPCG split and irrelevant to the Countdown one
  (no other TempleRun code consumes countdown events).
- **The RUGS port.** Approved changes here must also land in `../RunnerUGSTemplate`, which
  carries a fourth (UGS) domain and its own bridge — and where the auto-event flows are
  off-limits to modification. A domain split is the most invasive kind of change for that
  port; each phase below must be judged portable or explicitly ERT-only before merging.

And one recurring structural fact: **the run lifecycle is the shared vocabulary.**
`TempleRunStarted`/`TempleRunEnded` are consumed by twelve files across every would-be
TempleRun sub-domain (audio, time, spawners, track, distance, collisions, power-ups). Any
extracted domain needs those signals mirrored across its bridge — the pattern already
exists (`TempleRunScenesReady`, `TempleRunLevelApplied` are exactly such mirrors), but it
means no TempleRun split is ever "zero bridge mappings".

## 3. Bridge vocabulary: translate, don't relay

A domain split only buys replaceability if each enum stays meaningful *on its own terms*.
That gives every bridge mapping a test:

> **Cover the source column. The target event's name alone must tell a reader of that
> domain what is happening and why it matters — in that domain's vocabulary.** A mapping
> that passes is a *translation* (the seam carries meaning across); one that fails is a
> *relay* (the same foreign concept wearing a local badge).

Two smells identify relays in the current tables:

- **The self-prefix.** A member that needs its own enum's name as a prefix to make sense —
  `TempleRunLevelApplied` *inside* `TempleRunEvents` — is imported vocabulary announcing
  itself. Native members never need the badge (`JumpStarting`, `CountdownTick`).
- **The foreign noun.** `TempleRunScenesReady`: "scenes" is GameFlow's word. TempleRun has
  no scene concept; what its one subscriber (`TrackManager`) actually treats it as is
  "begin the run's initialization".

Audit of the existing crossings (renames are cheap on the TempleRun side — §2). The
**Status** column is the reconciliation: three of the four relays were fixed in Phase A,
one was not.

| Current mapping | Verdict | Better target vocabulary | Status @ a9ae5a2 |
|---|---|---|---|
| `CountdownEnded → GameStarted` (then `GameStarted → TempleRunStartRequested` back) | **Relay, and the worst one** — a ceremony detail decides a session milestone (KNOWN_ISSUES #1) | See §4: countdown's end *means*, in gameplay terms, "release the player" → `PlayerActivateRequested` | ✅ **gone** — the mapping no longer exists; `Countdown2TempleRunBridge` carries `CountdownEnded → PlayerActivateRequested`, and GameFlow chains its own `GameStarting → GameStarted` |
| `GameScenesLoaded → TempleRunScenesReady` | Relay (foreign noun) | `RunInitializeRequested` — what TrackManager actually does with it | ✅ **renamed** (`TempleRunEvents.RunInitializeRequested` = 302) |
| `LevelApplied → TempleRunLevelApplied` | Relay (self-prefix), though the Sticky-mirror *mechanism* is right | `TrackLevelApplied` — its consumer resolves the int through `TrackLevelRegistrySO`; "track level" is native vocabulary | ✅ **renamed** (`TempleRunEvents.TrackLevelApplied` = 304, still Sticky) |
| `GameConfigApplied → TempleRunConfigApplied`, `DifficultySettingsApplied → TempleRunDifficultySettingsApplied` (+ the `TempleRunDifficulty*` family) | Relays mid-migration — the enum already contains the unprefixed, native replacements (`DifficultySettingsApplied` 320, `DifficultyChanging/Changed/ChangeFailed` 321–323, commented "direct, non-legacy") | Finish that migration: retarget the five `TempleRunDifficulty*` / `TempleRunConfig*` members onto the 320-block and retire the prefixed ones | ⬜ **open — the last surviving relay.** `TempleRunConfigApplied` (300) and `TempleRunDifficultySettingsApplied`/`Changing`/`Changed`/`ChangeFailed`/`ChangeRequested` (310–318) still sit beside the native 320–323 block. Carried into Phase B |
| `PlayerPaused → PauseRequested`, `PlayerResumed → ResumeRequested` | **Translation** — gameplay states a fact, session requests its own transition | keep as the model | — kept |
| `TempleRunEnded → GameEnding` | **Translation** — the run genuinely ends inside gameplay; the session reacts in its own words | keep | — kept |
| `UserJumpRequested → JumpRequested` (whole input bridge) | **Translation** — raw intent → domain request | keep; already the exemplar | — kept |
| `GameStarting → CountdownStartRequested` | **Translation** — session milestone → ceremony trigger, each named natively | keep | — kept, now spanning GameFlow → **Countdown** rather than GameFlow → TempleRun |

Note the asymmetry the good rows share: the *source* publishes a fact in its own past/
present tense; the *target* receives a request or an application in its own terms. When
both sides of a mapping are the same concept, ask whether the concept is genuinely shared
**data** (the difficulty table, the level int — where a Sticky mirror is the right
mechanism and only the *name* needs nativizing) or a milestone one domain is outsourcing
(the countdown/GameStarted round-trip — where the mapping itself is wrong).

These renames are safe (nothing serializes TempleRun member names except
`SplineSegmentCreated`, untouched) but each touches its subscribers and EVENTS.md; they
were batched rather than done piecemeal — the two cheap ones rode along with Phase A
(they were already in the files it was editing), and the difficulty-prefix retirement,
which touches a wider set of subscribers, remains for Phase B.

## 4. Candidate: Countdown — extract to its own domain (✅ done, PR #30)

> **Reconciled.** This section was the argument *for* the extraction; it now doubles as the
> record of it. The evidence and seam design below are unchanged (they were the input to
> [COUNTDOWN_DOMAIN.md](COUNTDOWN_DOMAIN.md), the implementation spec); what was the
> forward-looking "Sketch" at the end is now [**As built**](#as-built) and states where
> reality differed from the prediction.

### The evidence

The countdown is the cleanest clique in the codebase. Its full footprint:

| Piece | Touches countdown events | Touches anything else? |
|---|---|---|
| `Player/CountdownController.cs` | Starting, Started, Tick, Ending | nothing |
| `UI/CountdownUIController.cs` | Starting, Tick, Ended | nothing |
| `TempleRunAutoEventFlow` | 2 chain entries (StartRequested→Starting, Ending→Ended) | — |
| `TempleRunGameFlowBridge` | 2 mappings (GameStarting→CountdownStartRequested, CountdownEnded→GameStarted) | — |

**Zero files straddle the cut.** No other TempleRun code subscribes to or publishes any
countdown event. (`CountdownCancelled` is referenced by nothing — one of task L13's dead
members; the extraction should drop it or give it a publisher, not copy it forward blind.)

### The seam design: what the countdown's end *means*

Applying §3 to the extraction: `CountdownEnded` must not be relayed as `GameStarted`. In
gameplay vocabulary, the end of the countdown means exactly one thing — **the player is
released**. So the seam is:

```
GameFlow:  GameStarting ──bridge──► Countdown: CountdownStartRequested … Tick … CountdownEnded
Countdown: CountdownEnded ──bridge──► TempleRun: PlayerActivateRequested
                                                 └─chain─► PlayerActivating ─chain─► PlayerActivated
```

with a new three-rung **`PlayerActivate*` ladder** in TempleRun's player-lifecycle
category (suggested values 14–16, adjacent to the fail/death/revive block; safe to add,
§2). Both chain links start chained per the ladder philosophy — a spawn-in animation or
"grace period" later breaks one, with no controller edit. Swap TempleRun for another
runner and the countdown still ends in a `PlayerActivateRequested` on *that* runner's
bus — the stub is one mapping.

This has a consequence that is really the point: **`TempleRunStarted` today conflates
"the run's systems are up" with "the player is go"**, because both happen post-countdown.
Once activation is its own event, `TempleRunStartRequested` can be bridged from an
*earlier* session milestone (systems spin up during the ceremony — track already does,
via its Sticky level mirror), and each of the ~9 `TempleRunStart*` subscribers must
declare which of the two it meant. First-pass classification — **shipped whole, all nine
rows as predicted**; the "As built" column is the state at `a9ae5a2`:

| Subscriber | Means | Retarget to | As built |
|---|---|---|---|
| `GameTime` (run clock) | player go — the clock must not run under the countdown | `PlayerActivated` | ✅ `PlayerActivated` |
| `DistanceController` | player go | `PlayerActivated` | ✅ `PlayerActivated` |
| `AIController` (arms the autopilot) | player go | `PlayerActivated` | ✅ `PlayerActivated` |
| `TurnCollisionDetector` (arms failure) | player go | `PlayerActivated` | ✅ `PlayerActivated` |
| `SegmentAdvanceTrigger` | systems up (inert until distance moves) | keep `TempleRunStarted` | ✅ `TempleRunStarted` |
| `PlayerLifeController` | systems up (failure impossible before motion) | keep | ✅ `TempleRunStarted` |
| `Metronome` | player go — the beat paces the run; and pre-activation `CurrentSpeed` is 0, so its tick interval divides by zero (play-test verdict 2026-09-04) | `PlayerActivated` | ✅ `PlayerActivated` |
| `SetMusicPlayer` | systems up — music under the countdown is a feature (play-test confirmed) | keep | ✅ kept — though on `TempleRunStartRequested`, not `TempleRunStarted`; the analysis lumped it in with the `*Started` subscribers. Same side of the cut, so the verdict held |
| `LaneChangeController` (`TempleRunStarting`, lane init) | systems up | keep | ✅ `TempleRunStarting` |

And GameFlow's side of the old handshake: `GameStarted` becomes GameFlow-owned — chained
from `GameStarting` in its own table (its consumers, `GameState`'s flag and
`GameFlowUIPanelController`'s overlay swap, now fire at ceremony start rather than
ceremony end; the HUD appearing under the countdown overlay is expected and should be
confirmed in the play test). `GameStarted → TempleRunStartRequested` stays, now firing
pre-countdown, which is what makes the retarget table above necessary. If the retarget
churn is judged too large for one phase, the fallback is the minimal seam —
`CountdownEnded → TempleRunStartRequested` directly, preserving today's post-countdown
semantics in one translated hop instead of three — with the `PlayerActivate*` split
deferred to its own task. The fallback still removes the round-trip; it just leaves the
conflation in place.

> **Outcome:** the full version shipped, not the fallback (§9 Q2). `GameFlowAutoEventFlow`
> now carries `(GameStarting, GameStarted)`, and the HUD-under-the-countdown-overlay
> consequence was play-tested and accepted.

### Why "own domain" beats the two alternatives

- **Status quo (countdown stays a TempleRun category) + handshake fix.** As long as the
  countdown lives inside TempleRun, *some gameplay event* must trigger `GameStarted` — the
  smell (#1: a gameplay detail decides a session milestone, defeating the swap-the-runner
  promise) is structural, not a naming problem. Renaming the bridged event moves the
  arbitrariness; it doesn't remove it.
- **Move countdown into GameFlow.** Boundary-wise defensible (KNOWN_ISSUES #2 calls it
  session ceremony, kin to the loading screen) — but it grows the already-largest enum,
  buries the seam again, and was ruled out by the owner for this analysis.
- **Own domain.** Ceremony that is neither GameFlow's business logic nor gameplay:
  - Smell #1 resolved by §3's seam: no gameplay or ceremony event decides `GameStarted`
    at all; GameFlow chains it itself.
  - Smell #2 resolved by *choosing*: the writeup's complaint is "no one chose". This is a
    choice, recorded in the Domain Registry.
  - Smell #3 resolved by the move itself: `CountdownUIController` and `Countdown.uxml`
    land in the same domain folder.
  - Decision gate: criterion 2 passes cleanly (stub = one bridge mapping). Criterion 1
    passes (session-ceremony lifetime, not app flow, not gameplay). Criterion 3 is
    honestly **weak** — seven events, unlikely to grow. The compensating argument is
    pedagogical: this is a teaching repo, `/add-event-domain` has no in-repo worked
    example, and a seven-event domain a student can hold in their head is worth more here
    than enum purity. If that argument is rejected, fall back to "move to GameFlow", not
    to the status quo.

### As built

The sketch below was written as a forward proposal and became
[COUNTDOWN_DOMAIN.md](COUNTDOWN_DOMAIN.md); every bullet shipped as written unless marked.
Verified against `main` @ a9ae5a2.

- ✅ `Assets/Countdown/Scripts/Events/CountdownEvents.cs` — `[EventEnum]`, members
  `CountdownStartRequested/Starting/Started/Tick/Ending/Ended` (values 0–5). `Cancelled`
  **dropped**, with the reason recorded in the enum's own doc comment so a future reader
  doesn't restore it blind. Bus alias `CountdownBus`. No self-prefixing (§3): the members
  are already native.
- ✅ `CountdownAutoEventFlow` (2 chains, as today — the `Ending → Ended` open link keeps its
  "GO! flash goes here" seam). Two bridges: `CountdownGameFlowBridge`
  (GameFlow→Countdown: `GameStarting → CountdownStartRequested`) under
  `Assets/GameFlow/Scripts/CountdownSpecific/`, and `Countdown2TempleRunBridge`
  (Countdown→TempleRun: `CountdownEnded → PlayerActivateRequested`) — one-directional,
  hosted under the Countdown domain (the more application-level of that pair), mirroring
  how `Input2TempleRunAutoEventBridge` sits at the input seam.
- ✅ New in TempleRun: `PlayerActivateRequested/PlayerActivating/PlayerActivated` (14–16) +
  2 chain entries; the retarget table above.
- ✅ GameFlow edits: remove the two countdown mappings from `TempleRunGameFlowBridge`; add
  `(GameStarting, GameStarted)` to its own chain table.
- ✅ Moves: `CountdownController`, `CountdownUIController`, `Countdown.uxml` into
  `Assets/Countdown/`. Deletes: TempleRun's countdown category (values 30–36 — safe, no
  serialized refs) and its 2 chain entries. Each `.cs` moved **with its `.meta`**, so the
  GUIDs — and therefore the existing scene wiring — survived.
- ✅ Hosting: flow + both bridges belong in `Game_Boot_2_Play` (session lifetime — the
  countdown must exist before the gameplay scene finishes loading, since `GameStarting`
  fires during the load handshake). **Three manual Inspector steps**, all in that scene —
  landed as three components on one `CountdownDomain` GameObject.
- ✅ TempleRun's remaining contract: it receives `TempleRunStartRequested` (systems up) and
  `PlayerActivateRequested` (go), and cannot tell whether a countdown, a cutscene, or
  nothing at all sat between them. That sentence is the one that goes in the talk.

**Two things the sketch left open, and how they resolved:**

- **`CountdownUIController`'s scene stayed put.** "Verify during implementation" resolved
  to: it is still a component in `TempleRunGameplay.unity`, because that is the scene
  owning the overlay's `UIDocument`. So the *code* changed domains while the *scene
  hosting* did not — which is correct and worth stating plainly, because it looks like a
  violation and is not. Domain isolation is a rule about **event references**, not about
  which scene a component sits in; `CountdownUIController` references only
  `CountdownEvents`. The alternative — moving the UIDocument too — would buy nothing and
  cost a scene-load ordering dependency.
- **The countdown grew a duration of its own.** Not anticipated by the sketch: the
  ceremony's length used to be `TempleRunConstants.CountdownSeconds`, a gameplay constant.
  A domain that owns its own ceremony must own its own timing, so it became a serialized
  field on `CountdownController` and the constant was deleted. This is the small,
  generalizable tell that an extraction is real — **the seam is not only events; it is also
  the configuration each side reads.** Phase C should expect the same for track tuning.

## 5. Candidate: TrackPCG — right split, wrong moment

### The evidence

The track vocabulary (`SplineSegment*`, `CurrentSpline*`, `TrackSegment*`, `ActiveTrack*`,
`Segment*`, `SegmentGeometryReady`, ~33 members) already behaves like a half-formed
domain: it has its own scene (`TempleRunTrackPCG`), its own publishers
(`TrackManager`, `PathProvider`), and its own consumers (spawners, visuals). The decision
gate passes on paper: own lifecycle ✓, a family of events ✓, and the stub test is the
repo's own marketing — "swap in a new track generator".

But the member-reference map shows **ten files sitting across the cut**, and they are
precisely COUPLING_AUDIT's exhibit list:

| File | Movement-side events | Track-side events |
|---|---|---|
| `Player/TurnController.cs` | Turn ladder | `ActiveTrackChanging` |
| `Track/TurnCommitController.cs` | `Turn*Starting/Started` | `SegmentRequested`, `ActiveTrackChanging` |
| `Track/SegmentTransitionController.cs` | `Turn*Started` | `CurrentSpline*`, `SegmentExited`, `SegmentGeometryReady` |
| `Player/TeleportController.cs` | `Turn*Ending`, `Teleport*` | `CurrentSplineChanging` |
| `Player/MoveCharacterByDistance.cs` | (writes the transform) | `CurrentSplineChanging` |
| `Player/AIController.cs` | `Turn*Starting` | `ActiveTrackChanging` (as a turn-window proxy — audit item 1) |
| `Player/TurnCollisionDetector.cs` | `Turn*Started`, failure | `ActiveTrackChanging` |
| `Track/CoinSpawner.cs`, `ObstacleSpawner.cs`, `PowerUpSpawner.cs` | `TeleportEnded` | `SplineSegmentCreated` |

A split executed today would have to route every one of those crossings through a bridge
*as it stands* — canonizing the exact seams the audit exists to redraw (`AIController`
listening to a generation event as a gameplay signal; the turn flow threading four
components; `SegmentRequested` published from inside the turn ladder at a
breadth-first-order-dependent position). The bridge table would be long, apologetic, and
wrong within one audit.

### The recommendation: make the split the audit's acceptance test

Run COUPLING_AUDIT.md as specified, with one addition to its deliverables: the vocabulary
map (its deliverable #1) should be drawn as **the bridge table a TrackPCG domain would
need** — every player→track and track→player crossing named as a would-be mapping, each
passing §3's translation test, each with a payload that survives pass-through (a bridge
cannot synthesize data, so e.g. the turn commit must publish a `Direction`-carrying
*movement* event — `TurnCommitted(Direction)`, say — for the track side's
`SegmentRequested` to be bridgeable). The audit succeeds when that table is short and
every row reads as a contract (`TurnCommitted(Direction)` → `SegmentRequested`;
`DistanceUpdated(float)` → track interest; `SplineSegmentCreated(SplineSegmentData)` →
spawners/visuals; run-lifecycle mirrors) rather than as an apology.

Whether to then *actually* extract the domain is a judgment call to make at that point:
the audit may deliver 90% of the value (visible seams, replaceable generator) without the
registration overhead. Extraction also makes a strong capstone student task — the audit
having laid the seam, the split is mechanical and every step is teachable. Either way,
resolve the cross-bus timing question (§2) first; the turn choreography is the code most
sensitive to it.

## 6. Candidate: Movement domain(s) — no

Jump, slide, dash, and lane are already model categories: each is a two-file clique
(gate controller + arc/motion controller) touching only its own ladder plus the run
lifecycle. There is nothing to decouple — no other system needs to *not know* about
jumping; the systems that must be swappable around movement already are:

- **Input** is replaceable at `UserInitiatedEvents` (the `AIController` proof).
- **Track** becomes replaceable via §5.
- Movement itself *is* the game. The stub test fails conceptually: a stubbed movement
  domain is a runner where nothing runs.

The turn ladder is the one movement system entangled with the track, and that is §5's
problem, not a reason to give movement its own bus. Verdict: keep as categories; spend the
effort on the audit.

## 7. Candidate: decomposing GameFlow — no (with one future exception)

The instinct — GameFlow bundles level selection, menus, loading, scenes, session, pause,
config, save, quit — is correct as a description. But the reference map shows the
categories are one *choreography*, not separable contexts:

- **Every panel listens to its siblings.** `MainMenuPanelController` subscribes to
  `LevelSelectorShowing`, `GameScenesLoading`, `GameplayNotReady`;
  `LevelSelectorPanelController` to `MainMenuShowing`, `GameScenesLoading`;
  `LevelSelectorController` publishes `MainMenuShowRequested`. Cutting UI-shell domains
  apart turns "panels hide each other" into bridge traffic between domains that are all
  the same screen.
- **`GameState` observes five candidate domains** (menus, selector, session, pause,
  config). Under a split it is either a five-domain violation or a privileged observer —
  and the rules deliberately have no such concept.
- **The chain table is mostly cross-category** — six of its orchestration entries
  (`LoadingScreenHidden→GameplayReady`, `GameplayReady→MainMenuShowRequested`,
  `LevelSelected→GameScenesLoadRequested`, `GameScenesLoaded→GameStartRequested`,
  `GameEnding→GameScenesUnloadRequested`, `GameEnded→GameplayReady`) *are* the app's
  spine. Turning the spine into bridges adds hops and scene-hosted components to the
  boot path for no replaceability gain — nobody stubs "scenes load".
- The decision gate's criterion 1 already rules on this: app flow is the thing the gate
  says is *not* a new domain. And GameFlow is where the serialization risk lives
  (enum ints in two components, two scene strings), making it the most expensive enum to
  disturb.

**The one defensible future carve-out: Meta/Progression** — `LevelSelected`,
`LevelApplied`, `LevelUnlocked`, `LevelProgressSaved` plus `LevelConfigApplier`,
`LevelProgressManager`, `LevelRegistry`, and the currently-dead Save/Load category (8 of
task L13's unreached members; a split should delete or adopt them, per the audit's
"reduce, don't raise" constraint). It passes the stub test ("always level 1, nothing
saved") and criterion 3 if cloud save/achievements ever arrive. Two reasons to wait:
today it drags `GameEnding` (session) and the selector UI choreography with it, and in
RUGS this is exactly the territory of the existing **UGS domain** — extracting it here
first would force the RUGS port to reconcile two competing answers to the same question.
Revisit when persistence actually grows; don't split ahead of the need.

## 8. Phasing

Each phase ends compile-clean (`dotnet build Assembly-CSharp.csproj`), play-tested from
`0_BootStrap_Game_Only` with event logging on, and `/audit-events`-clean.

**Phase A — Countdown extraction + the `PlayerActivate*` seam** — ✅ **complete.**
Merged 2026-09-04 as PR #30, `main` @ a9ae5a2. Implementation spec:
[COUNTDOWN_DOMAIN.md](COUNTDOWN_DOMAIN.md). Shipped the full retarget table (not the
fallback) and the §3 renames `RunInitializeRequested` / `TrackLevelApplied` pulled forward
from Phase B. The same branch also carried an unrelated owner ruling — the removal of the
`EventId<T>` static-field mint pattern across 13 files (no overlap with the countdown
files; it is in this PR only because it was ready). The checklist as executed:

1. ✅ Implementation spec written ([COUNTDOWN_DOMAIN.md](COUNTDOWN_DOMAIN.md));
   `CountdownCancelled` dropped; the retarget table confirmed rather than reduced to the
   fallback.
2. ✅ Enum, flow, both bridges, the `PlayerActivate*` ladder, and the 12-place registration
   (CLAUDE.md's four tables, the skills, ARCHITECTURE / EVENTS / TRACKS / README).
3. ✅ Manual Inspector step done: `CountdownAutoEventFlow`, `CountdownGameFlowBridge`, and
   `Countdown2TempleRunBridge` all sit on one `CountdownDomain` object in
   `Game_Boot_2_Play`.
4. **Partly done — one item outstanding.** The run trace was play-tested and confirmed:
   countdown shows and ticks, the player is frozen until GO, pause holds the countdown,
   music-under-countdown is intended, the metronome starts at GO. `dotnet build` clean and
   the `/audit-events` isolation greps clean. **Not recorded as done: the stub test** —
   disable the countdown objects, map `GameStarting → PlayerActivateRequested`, confirm a
   run still starts. That is the experiment that actually *demonstrates* the replaceability
   this phase was for, and it is a five-minute check; it is also the ready-made classroom
   demo for [EXERCISE_DRAW_THE_BOUNDARY.md](../EXERCISE_DRAW_THE_BOUNDARY.md). Worth doing
   before the talk, whether or not anything else in Phase B moves.
5. ✅ Fallout landed in the same PR: KNOWN_ISSUES' countdown section now reads "RESOLVED";
   TALK_OUTLINE, EXERCISE_DRAW_THE_BOUNDARY, ARCHITECTURE, EVENTS and TRACKS updated.
   **RUGS:** the freeze turned out not to block a reviewed domain addition — the port
   landed in its own RunnerUGSTemplate PR (§9 Q4).

**Phase B — COUPLING_AUDIT execution** — **analysis done 2026-09-04; execution in progress — Phases 1–2 landed (PRs #35, #37), Phases 3–6 not started.**
The plan is [TRACK_PLAYER_DECOUPLING.md](TRACK_PLAYER_DECOUPLING.md), six phases, none of
them needing a manual Inspector step. Both additions this section asked for were delivered:
its §2 draws the vocabulary map **as the would-be TrackPCG bridge table** (four rows, each a
contract), and the surviving §3 rename rides along as its Phase 5 — where it turns out to be
simpler than assumed. The 320-block this section proposed retargeting onto is itself
unwired (0 references for `DifficultySettingsApplied`/`Changing`/`Changed`), so the work is a
rename of the live `TempleRunDifficulty*` members plus a delete of the duplicates, and it
*reduces* the dead-member count. The cross-bus timing question is still open and is that
plan's Phase 6 — Phase A did not answer it, since the countdown split cut no synchronous
choreography.

**Phase C — TrackPCG extraction, or the deliberate decision not to.** Gate: Phase B's
bridge table is short and contract-shaped; the timing question is answered; owner decides
between "extract now", "extract as student capstone task", and "the audit was enough".
Not scheduled here.

**Explicitly not planned:** movement domains, GameFlow UI/session/scene domains,
Meta/Progression (parked with a revisit trigger: persistence growth or RUGS convergence).

## 9. Questions for the owner

*Original numbering is preserved through the regrouping, so "§9 Q2" cited elsewhere in this
doc and in the PR history still points at the same question.*

### Answered by Phase A

1. **Countdown: accept the "own domain despite weak criterion 3" trade (teaching value)?**
   The fallback if not is GameFlow, not the status quo — the status quo cannot satisfy
   "GameFlow owns started".
   → **Yes.** Extracted, and it is now the repo's `/add-event-domain` worked example. The
   weak criterion 3 (won't grow) held true and did not hurt: six members, one screen of
   code, and the domain reads as complete rather than stunted.
2. **The `PlayerActivate*` seam moves `TempleRunStartRequested`/`GameStarted` to
   *pre-countdown* and retargets four subscribers (the retarget table in §4). Ship that
   inside Phase A, or take the documented fallback?**
   → **Shipped in full, not the fallback.** All nine classifications survived play test;
   the one surprise was a *bug the split exposed* rather than caused — `Metronome` had been
   dividing by a `CurrentSpeed` of 0 during the countdown. Conflated events hide arithmetic
   like that, which is the argument for the split stated better than the analysis stated it.
4. **RUGS: is the auto-event-flow freeze there absolute (Phase A becomes ERT-only), or may
   a reviewed domain addition touch it?**
   → **Not absolute.** The port landed in its own RunnerUGSTemplate PR. Read the freeze as
   "no unreviewed edits", not "no edits" — future phases can plan on porting.

### Still open

3. **Should Phase C (TrackPCG extraction) be reserved as a student capstone task rather
   than done in-repo?** The audit's bridge-table deliverable is worth having either way.
   Phase A does not settle this, but it does make the question cheaper to answer: the
   extraction cost is now measured rather than estimated — one enum, one flow, two bridges,
   one Inspector step per hosted component, and a 12-place registration, all of which a
   student can follow from a worked example that did not exist when this was first asked.
   The open half is the *risk*: TrackPCG cuts a synchronous choreography (the turn commit)
   where the Countdown split cut none, so the cross-bus timing question in §2 is a real
   precondition for a student attempt, not paperwork.

### New, raised by Phase A

5. **Does the "own configuration" tell generalize?** The countdown could not be a real
   domain while its duration lived in `TempleRunConstants`. If that is a rule and not a
   coincidence, TrackPCG's extraction implies moving track tuning out of the shared
   difficulty table too — which would touch `DifficultySettings`, and therefore collides
   with the difficulty-prefix retirement now scheduled in Phase B. Worth deciding before
   Phase B starts, since the two want to edit the same fields.
   → **Answered: yes — a domain owns its own data** (owner ruling, 2026-09-04). Applied in
   [TRACK_PLAYER_DECOUPLING.md §5](TRACK_PLAYER_DECOUPLING.md#5-position-on-blackboard-deliverable-4),
   where it settles the `Blackboard` question: configuration follows the concern that reads
   it, and shared mutable state must name an owner or stop existing. The collision predicted
   above did not materialise — finding #11 there shows the difficulty retirement is a rename
   of live members plus a delete of unwired ones, touching no tuning fields.
