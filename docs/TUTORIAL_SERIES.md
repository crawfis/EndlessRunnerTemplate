# Proposal — "The Other 95%": a build-along that turns the template into a good runner

**Status: proposal.** Nothing here is built yet. This is the design for a tutorial series
that extends the talk ([TALK_OUTLINE.md](TALK_OUTLINE.md),
[deck](talk/its-just-an-endless-runner-v2.html)) into something people do rather than watch.

The talk's own punch line is the title: *"the mechanic is a weekend — the other 95% is the
game."* The talk describes that 95%. This series builds it, in public, one session at a
time, on the template that already exists.

---

## The idea in one paragraph

The template is deliberately plain: capsule player, primitive obstacles, flat track. Every
runner tutorial on the internet stops roughly where this template starts — they teach you
to make a character move forward and hit a box. **Nobody teaches the part that makes a
runner good**, because that part is diffuse: camera, timing, feedback, fairness, pacing,
retry latency, and a reason to come back tomorrow. This series teaches exactly that part —
and because the template is event-driven, almost all of it arrives as *new files that
nothing else knows about*, which makes it teachable in bounded, single-session chunks that
never break the previous session's work.

## What makes this different from every other runner tutorial

Four things, and they are the whole reason to build it.

1. **Every session ends with a scorecard.** Files added · gameplay lines changed · events
   added · audit result. The running total across the series *is* the architecture argument
   — proven forty times instead of asserted once. When session 4 finally shows a nonzero
   "gameplay lines changed," that number is the lesson.
2. **Two lanes, same finish line.** Every session ships in a **by-hand lane** (you write the
   subscriber) and an **agent lane** (you hand the brief to an AI and review the diff). Same
   success criteria, same scorecard. This is the AI-driven-studio pedagogy made concrete,
   and it lets a solo learner move at a studio's pace without pretending the agent is magic.
3. **A rubric, so "good" is measurable.** "Make it feel better" is not a lesson. Each
   session states which rubric line it moves and how you'd know (see below).
4. **The robot playtests every session.** From session 3 on, the AI player runs the game a
   few hundred times headless overnight; the event log is the data. Tuning decisions are
   argued from death-per-segment numbers, not vibes. This delivers the talk's *"the endless
   runner, finally endless"* teaser with an actual artifact.

## The rubric: what "a good runner" means

Stated up front, in session 0, and referenced by every session afterwards. Each line maps to
a seam that already exists in the codebase — which is what makes them teachable here rather
than generic advice.

| # | The bar | How you know | The seam it lands on |
|---|---------|--------------|----------------------|
| **R1** | **Readable** — you see the decision ~1.5 s before you must make it | Measure it: sight distance ÷ current speed, at every speed tier | `TurnAvailableDistance` is a *distance*; make it a **time budget** |
| **R2** | **Forgiving** — early inputs still count; a graze isn't death | Buffered jump, coyote time, a stumble state | `JumpController` gate; a new `PlayerStumbling` chain |
| **R3** | **Escalating** — pressure grows and releases in waves | Deaths-per-minute climbs; players can't name why | `WaveSelector` exists and is **wired nowhere** |
| **R4** | **Worth risking** — a reason to take the dangerous lane | Coin lines that cost you safety; measured take-rate | `CoinSpawner` + `LaneConfig` |
| **R5** | **Instant** — under two seconds from death to running again | Stopwatch it. This is the genre's single biggest retention lever | Retry without reloading the scene set |
| **R6** | **Legible in defeat** — you know *why* you died | Playtesters can state the cause unprompted | `PlayerFailingAtObstacle`, run summary |
| **R7** | **Worth looking at** — one coherent visual and audio identity | A stranger can describe the game's look in a sentence | Visual/audio scenes, `VisualTheme` |
| **R8** | **Worth returning to** — a reason to open it tomorrow | Day-2 return in a real playtest cohort | Progression events in GameFlow |

Two of those rows are the good stuff: **R1 as a time budget rather than a distance** is a
real design insight most runner tutorials never state, and **R3's `WaveSelector` sitting
unused in the repo** is a ready-made lesson about pacing as a swappable policy.

---

## The sessions

Eleven sessions, ~2 hours each (≈22 hours). Sized for a 10–12 week course module, a two-day
workshop plus follow-up, or self-paced.

### S0 · Read your own game *(45 min warm-up)*
Clone, boot, play, turn on `Log Events`, and **annotate the log of your own death**. No
code. Deliverable: a marked-up transcript of one run, with the moment you made the mistake
circled. *Lesson: the game already tells you everything it did — most projects can't.*

### S1 · Camera and speed — the cheapest 40% of feel
Cinemachine rig with separate cameras for running / jumping / dashing / turning / death,
blended by state; Impulse on landings; FOV kick and speed lines. *(A14, F2)*
**Cinemachine 3.1.7 already ships as a dependency and is used nowhere** — the camera is a
plain child of the player. Rubric **R1, R7**. Scorecard target: **0 gameplay lines.**

### S2 · Juice hour — six features, six files ⭐
Trails on dash/slide (F4), pickup particles (F1), screen-space feedback (F5), hit-stop and
shake on `PlayerFailingAtObstacle`, coins that fly to the counter (I1), a landing thump.
Rubric **R6, R7**. Scorecard target: **6 features · 6 new files · 0 gameplay lines · audit
clean.** *This is the session that sells the whole architecture, and it is the one to build
first — see the recommendation below.*

### S3 · Sound is a subscriber
Full SFX pass with one listener per sound (J2), mixer buses with pause/death snapshots (J3),
layered music stems that build with speed (J1). Rubric **R6, R7**.
**Introduce the robot here:** first overnight AI run; read deaths-per-segment.

### S4 · Fairness — the session that *does* touch gameplay
Trigger-based obstacle hits with a stumble state (A3), near-miss detection (A9), jump input
buffering and coyote time. This one runs the full `/list-events → /add-event → gate →
controller → /audit-events` path from [ADDING_A_MECHANIC.md](ADDING_A_MECHANIC.md), and it
is where a learner hits the validation-gate rule head-on. Rubric **R2, R4**.
Scorecard: **nonzero gameplay lines — and that is the point.** A real mechanic costs real
edits; polish does not. Knowing which is which is the skill.

### S5 · A world worth looking at
Biome/theme switching driven by distance (B7), skybox and parallax backdrops (G1),
trackside dressing with pooling (G4), time-of-day over a run (H1). Rubric **R7**.
*Lesson: theming is data, and switching it is a subscriber to `DistanceUpdated`.*

### S6 · A track worth running
Authored set-piece segments (B8), write your own `ISegmentSelector` (B3), expose segment
`Connections` with dead-end validation (B9) — then **A/B two pacing policies using the
robot's death data**. Rubric **R1, R3**. *Lesson: strategy pattern, then measurement.*

### S7 · The retry loop — highest leverage in the series
Game-over celebration and progress-to-next-unlock (I4), **instant retry that restarts the
run without reloading the scene set**, revive (A12), run summary. And the set piece:
**fix the bug from the talk** — `CountdownEnded → GameStarted → TempleRunStartRequested`
crosses the bridge and comes straight back. GameFlow should own "started"; gameplay should
only report *ready*. Rubric **R5, R6**. *Lesson: re-entry is where lifecycle bugs live —
and the talk's own red arrow is now your exercise.*

### S8 · A reason to come back
Persistent best and a coin economy (section E), unlockables gated on progress, a daily
challenge built from a seed (G6). Rubric **R8**. *Lesson: progression is where
game-specific product logic piles up fastest — keep it in GameFlow, behind events, or it
metastasizes.*

### S9 · Playable by other people
A USS design system with zero per-panel hard-coded colors (I7), HUD polish (I1),
contextual first-run tutorial (I3), accessibility pass including a reduced-motion toggle
that actually kills the shake you added in S2 (I6), settings (I2). Rubric **R1, R6**.
*Lesson: the difference between programmer UI and a product.*

### S10 · Ship it, then find out you were wrong
WebGL build published to itch.io (K4), performance pass with a before/after frame-time
report (H4), touch tuning (K1), the F2 bug-report key (I13). Then **a real playtest with
real humans**, and read the event logs against what they told you. Rubric: all of it.
Deliverable: a public URL and a data-backed playtest report.

### Capstone (optional) · Go live
Move to [RunnerUGSTemplate](https://github.com/crawfis/RunnerUGSTemplate): leaderboard,
remote config, cloud save — the talk's fifth swap, made by the learner.

---

## What we'd have to build

| Artifact | What it is | Effort |
|---|---|---|
| `docs/tutorial/s0…s10.md` | One chapter per session: goal, before/after, rubric lines, events involved, steps, scorecard, agent brief, stretch tasks, failure modes | **L** — the bulk of it |
| **Git tags per session** | `tutorial/s02-start` and `tutorial/s02-done`, so a learner joins anywhere and diffs against reference | **M** — and the main ongoing maintenance cost |
| **`/tutorial-check` skill** | An 8th skill: runs `/audit-events`, diffs the working tree against the session's start tag, prints the scorecard | **S/M** — this is what makes "0 gameplay lines" *measured* rather than promised |
| **Headless playtest harness** | Task **L5** — drive the AI player N times with timescale up, dump the event log, summarize deaths per segment | **M** — needed from S3 on; also useful to the template on its own |
| **A permissive art + audio pack** | CC0 / CC-BY models, textures, SFX and music stems so S1–S5 don't stall on licensing | **M** — boring, and the #1 thing that kills art-side tutorials |
| **Recorded before/after clips** | 15 s per session; the series' marketing and its proof | **S** per session |

The `/tutorial-check` skill is the keystone. Without it the scorecard is a claim in prose.
With it, every learner *generates* the architecture's proof themselves, on their own
machine, at the end of every session — which is a far stronger teaching move than any slide.

## Honest risks

- **Tag maintenance.** Eleven session tags against an evolving template is real, recurring
  work, and every refactor of `main` risks breaking a chapter. Mitigation: cut the tutorial
  from a `tutorial/` branch that tracks tagged template releases, not `main`.
- **Unity version churn.** Cinemachine 3 already broke most 2.x tutorials; UI Toolkit and
  the PanelRenderer path are still moving. Budget a version-bump pass per release.
- **Asset licensing.** S1–S5 are the sessions most likely to stall. Solve the art pack
  before writing those chapters, not during.
- **Scope creep in S5 and S9.** Art and UI sessions have no natural floor. Timebox them by
  rubric line: ship the thing that moves R7 or R6, and put the rest in the stretch list.
- **Agent-lane drift.** Model behavior changes. Write the briefs against the repo's docs
  (`AGENTS.md`, the skills) rather than against any one tool, and the lane survives.

## Build this first

**Session 2, "Juice Hour," as a standalone 90-minute workshop.** Reasons:

- It is the single most persuasive hour in the whole series — six visible features, six new
  files, zero gameplay edits, all on screen at once.
- It needs no new infrastructure: two tags and a chapter. No headless harness, no art pack
  beyond a handful of particles.
- It is the natural hands-on companion to the talk, and it gives the talk's closing slide a
  real call to action instead of "clone it."
- It validates the format — scorecard, two lanes, rubric — before committing to eleven
  chapters.

After that, **S1 and S4** complete a "Feel" mini-course: three sessions, half a day, camera
→ juice → fairness. That trio is a complete, saleable workshop on its own and covers rubric
lines R1, R2, R4, R6, R7 — most of what makes a runner playable.

## How it feeds back into the talk

- **A closing CTA that isn't "clone it":** *"The talk is the trailer. The tutorial is the
  game."* One line on the final slide, pointing here.
- **A sequel talk with data:** *"The Other 95%: we built the good version — here's what it
  cost."* The scorecards across eleven sessions become the evidence: how many features were
  purely additive, where the architecture actually charged us, and which rubric lines were
  cheap versus expensive to move. That is a talk nobody else can give, because nobody else
  has the scorecards.
- **A better honesty slide.** By S7 the countdown bug is fixed *by learners*, which turns
  the talk's most self-aware moment into a before/after instead of a confession.

## Related material already in the repo

- [STUDENT_TASKS.md](STUDENT_TASKS.md) — the 128-task catalog every session draws from
- [ADDING_A_MECHANIC.md](ADDING_A_MECHANIC.md) — the recipe S4 runs end to end
- [TIMEBOX_1_REQUIREMENTS.md](TIMEBOX_1_REQUIREMENTS.md) — the course assignment this would
  slot alongside; the sessions are deliberately smaller than a timebox task
- [KNOWN_ISSUES.md](KNOWN_ISSUES.md) — the countdown smell S7 fixes
- [EXERCISE_DRAW_THE_BOUNDARY.md](EXERCISE_DRAW_THE_BOUNDARY.md) — the discussion exercise
  that pairs with S7's boundary argument
