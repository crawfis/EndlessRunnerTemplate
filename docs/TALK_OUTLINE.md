# Talk Outline — "It's *Just* an Endless Runner"

A conference/meetup talk built from this repo, for a **general game-development audience**
(designers, artists, students, and programmers — with clearly marked technical slides).
The through-line: *look at everything that goes into a genuinely simple game, and how one
architectural rule keeps all of it tractable* — ending with AI agents doing student tasks
and a teaser for the UGS sibling repo.

Tool-agnostic: build it in PowerPoint, Slides, or reveal.js. The mermaid diagrams in
[ARCHITECTURE.md](ARCHITECTURE.md) can be pasted into any mermaid-capable tool or
screenshotted as-is. All numbers are verified against `main` @ `7a0c3bb` (2026-09-01) —
regeneration commands are in the [fact sheet](#fact-sheet-verified-numbers) at the bottom.

> **The built deck lives at [talk/its-just-an-endless-runner.html](talk/its-just-an-endless-runner.html)**
> — a self-contained HTML slide deck (open directly in a browser; no network needed).
> Keys: `←`/`→` advance (fragments first), `N` toggles speaker notes, `F` fullscreen,
> `Home`/`End` jump; print to PDF for a handout. The deck expands this outline to
> **32 slides**: it adds a spaghetti-wiring contrast pair in Act II ("How every tutorial
> wires it" + "It works. Then it rots."), a "What a direct call quietly assumes" slide
> after the one-rule slide (sole audience · shared fate on exceptions — the bus catches a
> throwing subscriber and keeps delivering, verified in the package source · existence,
> vs. fire-and-forget publishing), and turns the jump-sound payoff into an explicit
> side-by-side ("The test: add a jump sound"), so slide numbers below are offset from the
> deck's. Per-slide speaker notes are embedded in the deck itself; this file remains the
> planning reference (cut map, Q&A prep, demo checklist, fact sheet).

---

## Format and title

- **Length:** designed for **45 min + Q&A**. A [30-minute cut map](#30-minute-cut-map) is below.
- **Slides:** 28, four demos embedded. Tags: **[DEMO]** live moment, **[TECH]** programmer
  slide (skippable for a non-technical room), everything else plays to the whole audience.

**Title options** (first is recommended):

1. **"It's *Just* an Endless Runner"** — *what a simple game is actually made of.*
   Plays directly on the underestimation everyone in the room has lived.
2. **"Nobody Calls Anybody"** — *a whole game built on events.* Architecture-forward.
3. **"204 Events, One Rule"** — numbers-forward, punchy, invites the question.

## The spine

One sentence you should be able to say at any point in the talk: **a "simple" game is a
large system, and the one rule — systems never call each other, they publish events — is
what keeps the largeness from collapsing into a hairball.**

The talk earns that claim with an escalating sequence of *replaceability receipts*, each
walking through the same door (the event seam):

1. **Visuals and audio are replaceable** — they live in separate scenes and only subscribe.
2. **The player is replaceable** — an AI publishes the same input events as the keyboard (live demo).
3. **The whole game is replaceable** — the menus never learn what game they're running.
4. **The contributor is replaceable** — an AI agent ships an artist task through the same seams (finale demo).
5. **The machine is replaceable** — cloud services subscribe from a data center (the UGS teaser).

If a slide doesn't advance one of those, cut it.

---

## Act I — The Simplest Game You've Played (~7 min)

### Slide 1 — Cold open: play the game [DEMO D1]
- No title slide yet. Boot the game, play 30–60 seconds: turn, change lanes, jump, slide,
  dash, grab coins, hit a power-up, die.
- Say what they're seeing: capsule player, primitive obstacles, flat track — deliberately
  plain. "You've played this game in better clothes: Temple Run, Subway Surfers."
- Plant the question: **"How long would this take you to build?"** Let the room lowball it.
  Don't answer yet.

### Slide 2 — Title + who I am
- One line of framing: "This is an open-source Unity 6 template I built for teaching. Today
  I want to show you everything it's made of — and the one rule holding it together."
- Set expectations honestly: mostly for everyone; five marked slides in the middle are for
  the programmers; the ending is AI agents and live services.

### Slide 3 — The trope
- "It's just Flappy Bird. It's just a runner. Weekend, tops." Every dev has said it;
  every dev has been wrong.
- The gap between the elevator pitch (one sentence) and the build (this talk) is the
  subject. The pitch describes the *mechanic*; the build is the *product*.

### Slide 4 — The iceberg (the numbers slide)
Reveal progressively — start with what the player sees, then what it took:
- **What players see:** seven verbs. Run, turn, switch lanes, jump, slide, dash, collect.
- **What it took:** **204 named events** across **3 isolated domains** · **145 C# scripts**
  · **15 scenes** loaded additively · **38 auto-chain rules** and **19 bridge mappings**
  declared as data · **17 track-segment assets + 5 level rulesets** with zero code ·
  **7 UI panels** · and a **128-task catalog** of what it still takes to become a
  *polished* product (the catalog even continues into the cloud sibling as sections Q–X).
- Do NOT let this read as bloat. The pivot line: "The mechanic is a weekend. The game is
  the other 95% — and architecture decides whether that 95% builds on itself, or buries
  you." Then make it concrete: does each finished feature make the next one *cheaper* —
  a sound is one new subscriber — or more expensive — five more files to touch? Same 95%
  of work, opposite slopes.

### Slide 5 — Where the mass actually is
- Re-sort the same 204 events by *job*, not domain: **Playing it** (the verbs, crashes,
  pickups, dying, plus raw input) = 67 · **Building the world** (track segments, splines,
  geometry, recycling, teleport) = 29 · **Running the show** (all of GameFlow plus
  countdown, pause, run lifecycle, bridged difficulty) = 108.
- **The game you play is a third of the game you build.** Over half the vocabulary is
  session ceremony — loading screens, level select, countdown, pause, quit confirmation,
  save hooks. Nothing the trailer shows.
- Transition: "So how do 145 scripts across 15 scenes not turn into spaghetti? One rule."

---

## Act II — The One Rule and the Shape It Forces (~13 min)

### Slide 6 — The one rule
- **Systems never call each other. Ever.** A system that wants something to happen
  *publishes* a named event; systems that care *subscribe* and react.
- For the general audience: it's a radio broadcast, not a phone call. The jump button
  doesn't phone the player — it announces "jump requested" on a channel; whoever cares tunes in.
- Two real lines on screen (from ARCHITECTURE.md): the slide input publishes
  `UserSlideRequested`; the slide controller subscribes to `SlideStarting`. Neither file
  mentions the other. This is publish/subscribe — the observer pattern — applied with
  zero exceptions.

### Slide 7 — Anatomy of a jump
- Walk the chain as a diagram, one hop at a time:
  `space bar → UserJumpRequested → (input bridge) → JumpRequested → JumpController
  validates (already airborne? cooldown?) → publishes JumpStarting → JumpArcController
  drives the arc → JumpStarted`.
- Highlight the **gate**: the hop from *Requested* to *Starting* is deliberately manual —
  the controller earns it by validating. (Programmers get the full rule in Act III.)
- The payoff line, verbatim from the repo: **adding "play a sound on jump" is a new
  subscriber to `JumpStarted` — zero edits to either controller.** New behavior without
  asking permission from old code.

### Slide 8 — What the rule buys
- **Decoupling:** publisher and subscriber compile independently; swap either side.
- **Extension without modification:** features are *added*, not *edited in*. (That's the
  open/closed principle, wearing work clothes.)
- **Observability:** because everything is an event, the game can narrate itself…

### Slide 9 — [DEMO D2] The game narrates itself
- Flip on event logging (`CrawfisSoftware > Events > Log Events`), play 15 seconds, show
  the console: boot chain, menu events, countdown ticks, every jump and coin as a line.
- Talking point: debugging cross-system behavior by *reading the story* instead of
  stepping through call stacks.
- Bonus beat: the task catalog includes an in-game bug-report form that attaches a
  screenshot **plus the last N events** — press F2 when something feels wrong and the
  game tells you what just happened. Event architecture is a flight recorder for free.
- **The silence beat** (answer the "isn't 204 events over-engineering?" objection before a
  skeptic raises it). Most logged events have no listener, deliberately: we
  over-*designed* — named the full vocabulary up front — we did not over-*engineer*, since
  an unused event costs one line in an enum and no machinery. Real numbers from the repo:
  **204 events, ~138 with no subscriber, ~31 auto-chain targets that fire into an empty
  room on every single run.**
- Two flavors of silence, and only one is a hole:
  - **Never fires** — the `*Failed` / `*Cancelled` family (`SaveFailed`, `QuitCancelled`,
    `CountdownCancelled`). Correct behavior; that's the bad-day branch.
  - **Fires unheard** — a standing offer. Every auto-chain hop and every
    Requested→Starting→Started pair is a slot where juice, polish, analytics or
    accessibility can be *added*: collision ⇒ stop, plus rumble, on
    `PlayerFailingAtObstacle`, a fade
    on `LoadingScreenHiding`, a telemetry ping on `Saved`.
- Land it with the callback: `PlayerFailingAtObstacle` already fires on every death, so
  screen shake is **one new file in the SFX scene** — same shape as the jump sound.

### Slide 10 — Three domains, one door each
- Show the domain diagram (ARCHITECTURE.md mermaid): **UserInitiated** (raw input) →
  **TempleRun** (gameplay) ↔ **GameFlow** (app lifecycle).
- Events are grouped into three enums on three separate buses, and domain code may only
  touch its own domain's events. Crossing happens in exactly **one bridge per crossing** —
  two small files translate between vocabularies; everything else is walled off.
- Metaphor for the room: three departments that only communicate by posting memos through
  one mailroom. The mailroom's routing table is ~19 lines of data.

### Slide 11 — What isolation is *for*: replaceability
- Receipt 1: visuals, audio, environment are **separate scenes** that subscribe to logic
  events. Reskin the game without opening a single controller.
- Receipt 2: the input domain is a **one-way funnel** — anything may publish requests
  (keyboard, touch swipe, tilt, an AI, a replay file, a network peer), and only one bridge
  listens. No gameplay code is coupled to "a human pressed a key."
- Receipt 3: GameFlow never learns what game it's running — level selection crosses the
  bridge as *an integer*. The entire runner underneath the menus could be swapped for a
  different game and the menus wouldn't change a line.

### Slide 12 — [DEMO D3] The player is replaceable
- In the Inspector, tick one checkbox: `AIController._isEnabled`. Play. The character now
  turns itself at every wall — because the AI publishes the **same `UserInitiatedEvents`
  the keyboard does**.
- Honest laugh line (use it, it lands): it's a *perfect turner and a terrible jumper* — it
  only handles turns, so it will eventually die to an obstacle. A student task (C8) is to
  give it reaction time and misjudgment and make it a rival racer.
- The point, said slowly: **the player was replaceable before anyone wrote an AI.** It
  falls out of the architecture; nobody added a "bot mode."
- **Teaser (flag it as a teaser, not a receipt — we have not run the week).** The AI needs
  no keyboard, no window focus and no human, and every run already narrates itself into the
  event log. Pair those two and you have an automated playtest lab neither half was built
  for: leave it running a week, a million unattended runs of balance data and crash repros.
  *The endless runner, finally endless.* Task **L5** (play-mode test suite, drive the game
  headlessly) is where a team picks it up. If someone challenges the number: a run is under
  a minute, headless with timescale up, in parallel — it's arithmetic. The real payoff is
  the questions it answers — which segment kills people, whether level 4 is a wall, which
  auto-chain regressed overnight.

### Slide 13 — A run, end to end
- Show the boot-to-game-over sequence diagram (ARCHITECTURE.md): boot → menus → level
  select → scenes load → countdown → run → death → session unwind, with each hop labeled
  by domain and the bridge crossings visible.
- Point at the highlighted box first — *that* is the game: a character moving forward,
  turning, collecting coins. Then sweep the rest: it is all ceremony around that one line.
  The countdown alone is a request/starting/started/tick/ended chain, and "game started"
  crosses the bridge **twice** before the first step (GameFlow starts the countdown; the
  countdown ending starts the run).
- **Leave that clean.** Do not editorialize here — this exact diagram returns after the
  honesty slide with our own bug drawn on it, and the callback only lands if the room takes
  it as sound now.
- Close the act: "Every arrow on this slide is a named event you can log, reroute, or
  subscribe to. That's the whole trick. Now — five slides for the programmers."

---

## Act III — For the Programmers [TECH] (~7 min)

Tell the room: "Five slides of mechanism. If events aren't your thing, this is your
stretch break — Act IV has ScriptableObjects and Act V has robots."

### Slide 14 — [TECH] The bus: `EventsFor<T>`
- One static, lazily-initialized, typed bus per domain enum; aliased per file
  (`GameFlowBus`, `TempleRunBus`, `UserInputBus`). Subscribe in `Awake`, unsubscribe in
  `OnDestroy`, always.
- **No singleton GameObject, no execution-order attribute, no initialization race.**
- War story: the previous design used per-domain singleton components with
  `[DefaultExecutionOrder(-10000)]` — which only orders `Awake` *within one scene-load
  batch*, so it never protected the additively-loaded scenes that actually hosted them.
  Static + lazy made the whole failure class unrepresentable.

### Slide 15 — [TECH] Names are a state machine; gates are earned
- Every action follows `*Requested → *Starting/*ing → *Started/*ed`, with `*Failed` /
  `*Cancelled` off-ramps. The naming convention *is* the lifecycle documentation.
- The **validation-gate rule**: `*Requested → *Starting` is never auto-chained for
  movement. `Requested` is the bridge's *raw* translation — it fires whether or not the
  action is legal. The controller publishes `*Starting` itself once cooldown/airborne/
  lane-boundary checks pass. Auto-chaining it would fire before validation and silently
  defeat the gate.
- This is the #1 mistake juniors — and, foreshadowing Act V, AI agents — make in this
  codebase. That's why the rule is written into the machine-readable docs.

### Slide 16 — [TECH] Control flow as data: auto-chains
- Within a domain, event progressions are declared in a flat table of `(From, To)` pairs
  — 21 entries in GameFlow, 17 in TempleRun. Show a five-row excerpt.
- Pairs, not a dictionary — **one event may declare several consequences.** War story: the
  old dictionary allowed exactly one successor per event; developers who found a slot
  taken published the second consequence by hand inside controllers — which is how
  *failure logic came to publish pause events*. The data structure's ceiling became an
  architecture violation.
- Chains fire synchronously, in declaration order; cycles are a real hazard, which the
  audit tool checks for.

### Slide 17 — [TECH] Typed payloads and the sticky question
- Payload types are declared on the enum member (`[EventPayload(typeof(TrackSegmentInfo))]`);
  call sites mint an `EventId<T>` once into a `static readonly` field, and everything
  downstream is compiler-checked — no casts to get wrong. Mismatched type args across two
  call sites are reported at startup.
- Delivery policy: default `Transient`. An event is made `Sticky` (replayed to late
  subscribers) **only if it states something still true** — current state a latecomer can
  act on, like "the selected level is 3." Replaying an event that marks a *moment*
  ("menu hiding") to a late subscriber is actively wrong.
- The trap: `Paused`/`Resumed` each mark a moment, not the state — stickiness can't fix
  that; a late subscriber gets whichever half fired last. Carry the current state in one
  value-carrying event instead.
- The discipline stat: **2 sticky events out of 204.** And upgrades are evidence-driven:
  an in-editor audit (`Window > Events > Upgrade Audit`) reports which events actually
  had late subscribers after a play session. Measure, then upgrade.

### Slide 18 — [TECH] What it costs (the honesty slide)
- You can't ctrl-click from cause to effect. An event with no subscriber fails *silently*.
  And there are no assembly definitions, so **the compiler cannot enforce any of this.**
- The mitigations are process, not framework: strict naming, a checked-in event catalog,
  loggers/history, and an `/audit-events` sweep (missing unsubscribes, cross-domain
  leaks, chain cycles) run before every merge.
- The line to land: **architecture is a discipline you keep, not a library you install.**
- Then hand off to the callback: *"Let me show you what that bill looks like in our own code."*

### Slide 18b — [TECH] The same run, now with our own smell on it
- **Bring back the slide-13 run diagram, greyed out, with three arrows lit red.** Say
  "remember this?" and let the room recognize it before you explain anything.
- The bug: `TempleRunGameFlowBridge` maps `CountdownEnded → GameStarted`, and the reverse
  table maps `GameStarted → TempleRunStartRequested`. "The game has started" is decided
  inside gameplay, bridged into GameFlow, and bridged straight back. It works today — and
  it defeats the exact claim Act II spent three slides building: swap in a runner with no
  countdown and nothing publishes `GameStarted`, so the session hangs before the first step.
  GameFlow should own "started"; gameplay should only report that it is *ready*.
- Two softer smells telling the same story: the countdown is **session ceremony** (same
  category as the loading screen and game-over overlay, both in GameFlow) sitting in
  TempleRun by accident of history; and `Countdown.uxml` / `HUD.uxml` still live under
  `Assets/GameFlow/UI Toolkit/` while `CountdownUIController` is in TempleRun — the code
  changed domains and the assets never followed.
- The line to land: **the architecture didn't prevent this — it made it a red arrow you can
  point at.** The fix is one line in a bridge table, not a refactor.
- Don't wallow; two closers if you want them. "Where does the countdown belong?" is a real
  *Draw the Boundary* question with no settled answer, and all three smells are written up
  in [KNOWN_ISSUES.md](KNOWN_ISSUES.md). If asked why it wasn't just fixed before the talk:
  a template that shows its seams teaches more than one pretending it has none.

### Slide 19 — [TECH-lite] Patterns you already know, load-bearing
- Quick table (from ARCHITECTURE.md "Design vocabulary"): observer = the entire bus;
  bridge = the domain crossings; strategy = segment selection, path building, power-up
  effects; blackboard = shared run state; object pooling = the recycling track; MV
  separation = logic scenes vs. visual scenes.
- For students in the room: "Everything from your software-design course appears here
  with a job. This is what those lecture slides look like when they ship."

---

## Act IV — The Track Is Data (~3 min)

### Slide 20 — An endless world with zero code per level
- Three-stage pipeline, stages talking **only through events**: selection (`TrackManager`
  picks the next abstract segment from a tag/difficulty-filtered library) → geometry
  (`PathProvider` builds an Entrance → Pivot → Exit spline) → visuals (spawners build and
  recycle meshes, obstacles, pickups).
- Authoring is Inspector-only: 17 segment assets and 5 level rulesets as
  ScriptableObjects. A designer adds a segment or a whole level without a programmer.
- Two flourishes worth 15 seconds each: **"Either" T-junctions** — the track defers
  choosing until the *player commits* a direction, and only then builds the exit spline;
  and **pluggable selection strategies** — weighted difficulty, distance ramp, wave,
  authored sequence — one interface, four policies.

---

## Act V — The AI-Driven Studio (~9 min)

### Slide 21 — This codebase supports AI coworkers
- The repo ships `AGENTS.md` + `CLAUDE.md` (conventions any AI tool can read) and **seven
  skills** — step-by-step procedures stored as plain markdown, runnable as slash commands
  in Claude Code or followed as checklists by any other agent (Copilot, Cursor, Codex…).
- The mandated loop for *any* feature: `/list-events` → `/add-event` → implement →
  `/audit-events`. Events first, code second — the event definitions drive the
  architecture, for humans and machines alike.

### Slide 22 — Why AI works unusually well *here*
- The architecture is the guardrail. The event catalog is machine-readable ground truth;
  the domain rule gives an agent a small, closed vocabulary; the audit skill catches the
  coupling the compiler can't.
- Flip side, honestly: agents fail exactly like fast juniors — the classic being
  auto-chaining past a validation gate (slide 15). Which is why the rules live in files
  the agent reads, not in a senior's head.
- The reframe for the room: **clean architecture isn't just for humans anymore — it's a
  prompt.** The same properties that onboard a new hire onboard a model.

### Slide 23 — The artist's back door
- From the task catalog, verbatim: **"Art and audio tasks need no engine surgery.
  Visual/audio scenes subscribe to events."** Every polish task is a *new subscriber* —
  the event catalog is the API contract.
- The division of labor: the **agent** does discovery and wiring (find the event, write
  the subscriber, place it in the right scene, pass the audit); the **artist** does what
  they're actually expert in — curves, materials, timing, mix — in the Inspector.
- Menu of real tasks from the 128 (show as a table: task → events it subscribes to):
  - **F4 Dash & slide trails (S)** — `DashStarted/Ended`, `SlideStarted/Ended`
  - **C4 Ragdoll death (S/M)** — `PlayerDied` ("cheap drama")
  - **F5 Screen-space feedback (S)** — `ObstacleHit`, shield events, distance milestones
  - **F1 Pickup & power-up particles (S/M)** — `CoinCollected`, `PowerUpActivated/Deactivated`
  - **J2 SFX pass (M)** — a subscriber per sound, none wired into gameplay
  - **H1 Time-of-day over a run (M)** — `DistanceUpdated` driving sun and ambient
- Point at F1's own description: *"All listeners to existing events — zero gameplay
  edits, which is the point of the architecture."* The catalog says the quiet part out loud.

### Slide 24 — [DEMO D4] The finale: an agent ships an artist task
- **Live or time-lapse video** (see demo plan): hand an agent task **F4** — "dash and
  slide trails" — with the standard prompt. Watch it list events, write one subscriber
  script in the visuals scene, wire two TrailRenderers, run `/audit-events`, pass.
- Show before/after side by side, then show the diff: **zero lines changed in any
  controller.** The audit output is the applause line for the programmers; the trail is
  the applause line for everyone else.
- Back-pocket receipts: two pre-baked PRs (C4 ragdoll, F5 vignette) as screenshots +
  builds, in case the live run stalls or the room wants more.

---

## Act VI — The Teaser and the Close (~5 min)

### Slide 25 — So far, everything runs on one machine
- Recap the receipts in one breath: swapped visuals, swapped players, swappable game,
  AI-shipped features — all through the same event seams.
- "There's one more thing the seam can do: leave the machine."

### Slide 26 — Teaser: the UGS sibling (RUGS)
- **RunnerUGSTemplate** — the same runner with **Unity Gaming Services** integrated
  behind a game/service contract: the UGS domain arrives as packages and meets the game
  at a `GameServiceEvents` seam, so **cloud services never touch gameplay code**.
- Already working in it: **Authentication, Leaderboards, Achievements, Remote Config,
  Economy, Cloud Code.**
- The architectural punchline, delivered slowly: **the leaderboard is just another
  subscriber.** To the game, a cloud service is the same kind of thing as a sound effect
  — it reacts to the run ending. The domain pattern scaled from a keyboard to a data
  center without changing the rule.

### Slide 27 — What live services unlock (the menu, not the meal)
- Live tuning with Remote Config: retune difficulty and economy **without a rebuild** —
  then a real A/B test: two scoring models served to different cohorts, compared on the
  leaderboard.
- Server-authoritative scores with Cloud Code: don't trust the client — sanity-check the
  run's stat block (impossible speed, more coins than spawned) before writing the board.
  A genuine introduction to anti-cheat thinking.
- Cloud Save and the classic conflict: local progress vs. cloud progress — who wins?
  Seasonal events behind a feature flag: ship it, run it two weeks, retire it.
- And beyond: Friends, Lobby, Matchmaker, Relay + Netcode multiplayer, Vivox — RUGS's own
  Future Task Catalog continues this repo's lettering (sections Q–X), each entry with an
  AI hand-off brief. Fair warning included: UGS has a real setup tax; the first task is
  standing it up.

### Slide 28 — Close: three takeaways + links
1. **Simple games aren't small.** The mechanic is a weekend; the product is the mass —
   budget for the iceberg, not the elevator pitch.
2. **One rule, kept ruthlessly, beats a clever framework.** Everything in this talk —
   the logging, the audit, the swap-anything demos — fell out of "systems never call
   each other," enforced by convention, catalog, and audit rather than by compiler.
3. **Good seams pay three times.** The same door admits new code, new teammates (your
   artists), and new *kinds* of contributors — AI agents and cloud services included.
- CTA: both repos are open source — clone the template, do the ADDING_A_MECHANIC
  walkthrough, pick a task from the 128. Links + QR:
  `github.com/crawfis/EndlessRunnerTemplate` · `github.com/crawfis/RunnerUGSTemplate`.

---

## Demo plan

| # | Slide | What | Fallback |
|---|-------|------|----------|
| D1 | 1 | Play 30–60 s from boot | Pre-recorded capture |
| D2 | 9 | Event logger console during play | Screenshot of a logged run |
| D3 | 12 | Tick `AIController._isEnabled`, hands off the keyboard | Pre-recorded capture |
| D4 | 24 | Agent implements F4 (trails) end-to-end | Time-lapse video + pre-baked PR diffs |

**Prep checklist (do all of this on the presentation machine, the day before):**

- [ ] Verify **`CrawfisSoftware > Play Scene 0 Always` is ON** — it's a global editor
  preference, not a project setting; another project can have switched it off, and the
  boot chain won't run without it (this is README's own warning).
- [ ] Rehearse boot → menu → level select → run once end-to-end; pick and bookmark the
  demo level.
- [ ] For D3: find and bookmark the GameObject hosting `AIController` in the gameplay
  scene; pick an obstacle-light level so the "perfect turner" survives long enough to
  impress before its comic death. Decide whether the death is your segue (recommended).
- [ ] For D2: `CrawfisSoftware > Events > Log Events`, then **Clear Now** just before
  playing; bump console font for the projector; consider `Collapse` off.
- [ ] For D4: canned prompt saved in a text file (see workshop box below for the
  template); **timebox 6 minutes**; record the time-lapse fallback in advance; keep a
  finished branch as the ripcord (`git switch talk/f4-trails-done`), plus screenshots of
  the pre-baked C4/F5 PR diffs. The agent needs network — the video doesn't.
- [ ] Refresh the numbers slide if the repo has moved: `/list-events` regenerates the
  catalog; the count commands are in the fact sheet.

## If you run Act V as a hands-on exercise (workshop / class variant)

Replace slides 23–24 with 25–40 minutes of doing (works for the CSE 5912 room; also
works at a meetup if attendees bring laptops):

- **Setup:** pairs; each pair has the repo cloned and any agent available (the skills are
  plain markdown — Claude Code, Copilot, Cursor all qualify). Pairs pick one S-sized task
  from the artist menu (F4, F5, C4, G5, or two sounds from J2).
- **Prompt template:** "Read `AGENTS.md` and follow it. Implement task **F4** from
  `docs/STUDENT_TASKS.md`. Start with `/list-events` (or follow
  `.claude/skills/list-events/SKILL.md`). Do not modify any controller or gameplay
  script; new behavior must be new subscribers. Finish with `/audit-events` and show me
  the result."
- **Success bar:** the feature is visible in a run *and* the audit comes back clean.
- **Debrief question (the actual lesson):** *"What did the agent need to know, and where
  did it find it?"* Answer: the event catalog and the conventions docs — i.e., the
  architecture. The exercise teaches that documentation-as-contract is what makes both
  the codebase and the agent work.

## 30-minute cut map

- Drop slides 3 and 19; fold slide 8 into 7 (one payoff bullet).
- Act III → two slides: merge 14+15+16 ("the bus, the naming state machine, chains as
  data — and the gate rule"), keep 18 (costs) intact. Skip 17 unless the room is
  programmer-heavy.
- Slide 20 → 60 seconds, one visual.
- D4 as the 90-second time-lapse, never live. UGS teaser → single slide (26, absorbing
  one bullet from 27).
- Protect at all costs: 1, 4, 6, 7, 12, 22–24, 26, 28.

## Anticipated Q&A

- **"Why a central enum bus instead of C# events / UnityEvents / SO-events?"** Because
  events-as-data is what buys the tooling: the logger, the history/flight-recorder, the
  auto-chain tables, the generated catalog, the upgrade audit. You can't enumerate,
  table-drive, or diff `event Action`. The cost (indirection) is real and slide 18 owns it.
- **"Isn't this over-engineering for a runner?"** The direct-reference version works at
  small scale and then rots — every system knows every other system. And the receipts are
  the answer: the AI driver, the artist tasks, and the cloud sibling all shipped through
  seams nobody had to add later. (Also: it's a teaching codebase — showing the discipline
  at full scale is the point.)
- **"Performance of all this indirection?"** Events fire on state transitions, not per
  frame — the hottest is the distance tick. Dispatch is synchronous with no reflection at
  publish time. Honest answer: it has not been a bottleneck; profile before believing
  anyone's claims, including mine.
- **"Why no assembly definitions to enforce the domain rule?"** Deliberate simplicity for
  a student codebase — one assembly, fast onboarding. The trade is acknowledged in the
  docs: the compiler won't catch a violation, so review + `/audit-events` are the
  enforcement. (A good student argument-starter: would you add them?)
- **"Does the AI ever break the architecture?"** Yes — the same ways juniors do, headline
  case being auto-chaining past a validation gate. That's why the rules are in files the
  agent must read and why the audit runs before merge. The system assumes fallible
  contributors of every species.

## Slide-asset checklist

- Screenshot / capture: gameplay (D1 fallback), logged-events console, `AIController`
  Inspector with the checkbox, a `TrackSegmentSO` + `TrackLevelSO` in the Inspector, the
  level-selector UI, D4 before/after, the pre-baked PR diffs.
- Diagrams to lift from [ARCHITECTURE.md](ARCHITECTURE.md): domain flowchart, run
  sequence diagram, scene-composition tree.
- Code excerpts: the two-line publish/subscribe pair (slide 6), five rows of a chain
  table (slide 16), one `[EventPayload]` + `EventId<T>` mint (slide 17), five rows of the
  bridge mapping table (slide 10).
- Tables to lift: the design-vocabulary table (slide 19), the artist-task menu
  (slide 23) from [STUDENT_TASKS.md](STUDENT_TASKS.md).

## Fact sheet (verified numbers)

As of `main` @ `7a0c3bb`, 2026-09-01. Re-verify before the talk; `/list-events`
regenerates the event catalog.

| Claim | Value | How counted |
|-------|-------|-------------|
| Events total | **204** | 74 GameFlow + 121 TempleRun (explicit `= n` members) + 9 UserInitiated |
| C# scripts | **145** (160 with vendored ThirdParty) | `*.cs` under `Assets/`, excluding `ThirdParty/` |
| Scenes | **15** | `*.unity` under `Assets/` |
| Auto-chain entries | **38** | 21 in `GameFlowAutoEventFlow` + 17 in `TempleRunAutoEventFlow` |
| Bridge mappings | **19** | 10 in `TempleRunGameFlowBridge` (4 TR→GF + 6 GF→TR) + 9 in `Input2TempleRunAutoEventBridge` |
| Track data | **17 segments, 5 levels** | assets in `Assets/TempleRun/Scriptables/Track/` |
| Sticky events | **2 of 204** | `TempleRunLevelApplied`, `TempleRunDifficultySettingsApplied` |
| UI panels | **7 UXML** | `*.uxml` under `Assets/` |
| AI skills | **7** | `.claude/skills/*/SKILL.md` |
| Student tasks | **128** (sections A–P; RUGS continues Q–X) | [STUDENT_TASKS.md](STUDENT_TASKS.md) |

Count commands (PowerShell, from repo root): scripts
`(gci -r -filter *.cs Assets | ? FullName -notmatch ThirdParty).Count`; scenes
`(gci -r -filter *.unity Assets).Count`; events e.g.
`(sls '=\s*\d+' Assets\TempleRun\Scripts\Events\TempleRunEvents.cs).Count`
(UserInitiated has no explicit values — count its members directly).
