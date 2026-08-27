# Timebox 2 — Design Wide, Build Narrow

*CSE 5912 Capstone · Design and first production sprint*

## Overview and objective

Timebox 1 proved you can work as a studio. **Timebox 2 decides what the game is — and then
proves the smallest piece of it.**

The timebox has two halves, in this order:

1. **Design wide.** Deliberately over-design. Generate far more game than you can possibly
   build: every mechanic you could do, every mode you want to do, every "wouldn't it be cool
   if." Turn it into a large body of user stories and a map of the **systems** those stories
   imply, with the **seams** between them drawn on purpose. Then converge — rank it, cut it,
   and write down what you are *not* making.
2. **Build narrow.** Take the smallest slice that proves your core loop and build it in
   greybox: cubes, capsules, flat colors, playable end to end by someone who has never seen
   it.

The discipline that makes this work is one sentence: **over-design in breadth, never in
depth.** A hundred stories on a board cost an afternoon. A hundred stories' worth of
speculative code costs the semester. YAGNI applies to your code, not to your thinking.

This is your project, so this document does not tell you what to make. It tells you how to
stay organized while you decide, and it names the practices this timebox is really about:

| Practice | What you are learning |
|---|---|
| **Design** | generating options before judging them, then cutting on purpose and in public |
| **Systems thinking** | naming responsibilities, drawing seams, and defending them against features you have not built |
| **Project management** | estimating, measuring your own velocity, and cutting scope deliberately rather than by accident |
| **Software engineering** | a definition of done, review discipline, a testing beachhead, and an architecture that survives its second feature |
| **Agentic engineering** | giving an AI a contract and a process instead of a chat window, then measuring what it actually did |

## The two halves and the design freeze

Design expands to fill whatever time it is given. So the switch from half one to half two is
**a date, not a feeling** — put it on the calendar at planning, before anyone is attached to
anything.

| Timebox length | Design wide until | Build narrow |
|---|---|---|
| **Two weeks** | end of day 4 | days 5–10 |
| **Three weeks** | end of week 1 | weeks 2–3 |

**At the design freeze**, three things become true and stay true:

- The MVP slice is chosen and does not change. New ideas are welcome — they go to the
  backlog, tagged for a later timebox, not into this slice.
- The systems map and its seams are agreed. Changing them after this point is a decision the
  whole team makes at a standup, not something one person does in a branch.
- Estimates exist for the MVP stories only, and the sprint is scoped to fit (below).

If you blow through the freeze date, that is worth reporting honestly in the presentation.
Every studio has done it; the ones that learn say so out loud.

## The effort budget and scope math

The expectation is unchanged: **10–12 hours per person per week, every timebox.** This
timebox splits it about evenly, front-loaded toward design.

|  | Share | Per person, per week |
|---|---|---|
| Design, stories, systems, playtests, documents, presentation | ~half | 5–6 h |
| Building the greybox slice | ~half | 5–6 h |

**The math you should actually do**, with your team size and your number of weeks:

```
total person-hours   = members × weeks × 11
build hours          = total × 0.5
plan to              = build hours × 0.7      <-- the number your sprint may hold
```

Worked out for a six-person team:

| Timebox length | Person-hours | Build hours | The sprint may hold |
|---|---|---|---|
| **Two weeks** | ~132 | ~66 | **~45 hours of estimated work** |
| **Three weeks** | ~198 | ~99 | **~70 hours of estimated work** |

Run the same three lines for your own team size. That 70% is not slack — it is the part of
every sprint that goes to the thing that broke, the merge that fought back, and the estimate
that was wrong. A sprint planned to 100% of capacity is a sprint that ends in a panic.

Notice how small the build number is next to the story list you are about to write. That gap
is the point of this timebox, not a failure of it: **you will design ten times what you
build, and choosing which tenth is the skill.**

## What's due

| Deliverable | Hat |
|---|---|
| **The idea sweep** — the raw, unfiltered list of everything the game could be | design owner, whole team |
| **User story backlog** — large, in a consistent format, on the board | design owner |
| **The ranking** — MVP / Stretch 1 / Stretch 2 / **Not this game**, with reasons | whole team, design owner records |
| **Systems map** — the systems your stories imply, what each owns, what it must never know | tech lead |
| **Seam list** — which systems talk, how, and which pairs are forbidden from touching | tech lead |
| **Acceptance criteria and estimates** — for the MVP stories only | design owner + whoever builds each |
| **Sprint goal** — one sentence, testable, fixed at the design freeze | scrum master + design owner |
| **The greybox build** — the core loop playable end to end from a fresh clone | everyone |
| **Estimates vs. actuals** and your resulting velocity | scrum master |
| **Definition of Done** — written down, and actually applied | tech lead |
| **Test beachhead** — automated tests covering the core loop's state transitions | tech lead |
| **Build check** — CI on pull requests, or a documented, scripted local build | integrator |
| **Agent contract** (`AGENTS.md` / `CLAUDE.md`), one project-specific skill, and the run log | AI lead |
| **Playtest report** — three or more outside testers, and one change you shipped because of them | playtest owner |
| **Risk register** — updated, with owners | producer or scrum master |
| **Retro** — three actions, each with an owner and a date | scrum master |
| **Tech debt log** — what you knowingly deferred, and why | tech lead |
| **Presentation, demo, printed 4-up handout, peer evaluations** | deck owner + presenters |

## Design wide: over-generate, then converge

### Diverge first, and do it badly on purpose

The first session produces **quantity, not quality**, and nobody is allowed to say "that
won't work" yet. Every mechanic, mode, enemy, verb, failure state, progression hook, and
silly idea goes on the wall. Aim high enough that it feels wasteful — for a six-person team,
**sixty to a hundred user stories** and **fifteen or more named systems** is a realistic
morning's work with the whole team in a room.

Judgment is the *second* pass, and it is much easier when there is a lot to judge. A team
that generates six ideas will build the third one because it is the only one left standing. A
team that generates sixty will notice that four of them are the same idea, that two of them
are the actual game, and that one of them is a semester of work disguised as a feature.

### Write them as user stories

One format, used by everyone:

```
As a <kind of player>, I want <to be able to do something>, so that <it matters to me>.
```

The "so that" clause is the one that earns its keep — it is where a mechanic without a
purpose exposes itself. Keep them small enough that one person could finish one in a sitting;
split anything that needs the word "and."

**Acceptance criteria and estimates go on the MVP stories only.** Writing Given/When/Then for
eighty stories you may never build is the exact waste this section is trying to teach you to
avoid.

### Then converge, in daylight

Rank every story into four buckets, as a team, in one meeting:

| Bucket | Meaning |
|---|---|
| **MVP** | the slice that proves the core loop. This is what you build this timebox |
| **Stretch 1** | the next timebox, if the loop works |
| **Stretch 2** | the game you would ship with more time |
| **Not this game** | good ideas that belong to a different project |

**The "not this game" list is a deliverable, not a bin.** Written down, it stops the same
argument from returning every second week, and it is genuinely interesting to present: what
you decided not to make says more about your design than what you kept.

Whatever survives into MVP has to answer one question: *does a player experience the core
loop without it?* If yes, it is not MVP.

### AI is a divergence engine

This is the design half's agentic-engineering work. Use an assistant for the part it is
genuinely good at — volume and breadth — and keep the judgment:

- "Here is our hook and our three pillars. Generate forty user stories a player of this game
  would want, in the format above. Do not filter for feasibility."
- "List every system a game like this typically needs, including the ones teams forget."
- "Act as a skeptical senior designer. What is missing from this list? What breaks when we
  have ten of these instead of one?"

Then **you** cut. Generated ideas are raw material, not decisions, and every story that
survives into MVP should have a human who can say why in one sentence.

## Systems and separation

The story list is not just a plan — it is the pressure test for your architecture. This is
the most transferable thing in the whole timebox: **the shape of your systems is decided
now, cheaply, on paper, or later, expensively, in code.**

**1. Name the systems, not the classes.** A system is a responsibility — scoring,
progression, spawning, input, save/load, audio, UI presentation. Ten to twenty is normal.
Group your stories under them; a story that fits under none is telling you about a system you
have not named.

**2. For each system, write three lines:** what it owns, what it needs to know, and **what it
must never know**. The third line is the one that does the work. "Scoring must never know
what a player is" is a design decision with teeth.

**3. Draw the seams.** Which systems talk, in which direction, and by what means — a call, an
event, a shared data asset. Then mark the pairs that are **forbidden** from talking directly.
Those forbidden pairs are your architecture; everything else is arrangement. The runner
template makes the same choice explicitly: three event domains, no cross-domain references
except in one bridge file, so gameplay can be rewritten without touching UI.

**4. Apply the replace test.** Name one system you could delete tonight and rewrite tomorrow
without touching any other. If you cannot name one, you have drawn a diagram, not a
separation.

**5. Decide what is data, not code.** Which numbers belong in ScriptableObjects or config
files so they can be tuned without a rebuild — and, later, by someone who is not a
programmer. Anything you expect to playtest and change belongs here.

**6. Pressure-test against what you are not building.** Take three stories from **Stretch 2**
— features you have deliberately deferred — and walk them through your systems map. Which
seams hold? Which one would force you to edit five systems? That is the whole payoff of
over-designing: an architecture validated against features that do not exist yet, while
changing it is still free.

Deliverables: a one-page systems map, the seam list including forbidden pairs, and a note of
what the replace test and the pressure test told you. All three are presentation slides.

## The greybox rule

**Build it out of cubes.** Primitives, capsules, flat colors, default materials. No modeling,
no texturing, no shader work, no animation polish, no Asset Store shopping trip. The endless
runner you learned on shipped as a capsule running over grey boxes, and that was deliberate.

Why this is not a limitation:

- **Art hides bad design.** A beautiful level that isn't fun reads as "almost there." A grey
  level that isn't fun reads as "not fun yet," which is the information you need.
- **Grey is ten times cheaper to change.** In this timebox you will move the jump height, the
  gap width, and the enemy spacing twenty times. Move them while they are numbers on a cube,
  not geometry in a model.
- **It removes the wrong argument from playtests.** Nobody says "the art looks unfinished"
  when the art is obviously a cube. They tell you about the *game* instead.
- **Greybox is where the metrics get decided.** Player height, run speed, jump arc, reach,
  the width of a safe gap. Those numbers become the contract your art has to respect later —
  which is exactly why they must exist before the art does.

What greybox **does** allow: one accent color for readability (the thing you can stand on,
the thing that kills you), a legible silhouette, placeholder audio, and UI that is plain but
functional. Readability is design, not decoration.

The **one exception**: if art direction is genuinely the risk your team needs to retire this
timebox, one person may run a look-dev **spike** — a single screenshot or short scene proving
the visual target. That is a spike, sized and on the board, not the sprint.

## Carrying Timebox 1 forward

The Timebox 1 repository was disposable; the process you built in it was not. On day one of
the new repository:

- Copy `.github/` across — issue forms, labels, the PR template, the review ring.
- Copy the coding standards, the technical standards doc, and the AI policy.
- Re-read the **charter**. It was written by people who had never worked together. Now you
  have. Which clause did you break most often? Fix the clause or fix the behavior, and say
  which in the presentation.
- Carry the **retro actions** from Timebox 1. An action nobody checks is theatre.
- **Rotate the hats.** New scrum master, new presenters, new demo owner. This timebox adds
  three worth naming: a **build/CI owner**, a **playtest owner**, and an **AI lead**.

Everything from Timebox 1 that was about *the runner* stays behind. Everything that was about
*how you work* comes with you.

## Running the sprint

**The sprint goal is one sentence**, fixed at the design freeze. "A player can enter a level,
fight three enemies, and die or reach the exit." It is testable, it fits on a slide, and every
task on the board either serves it or is explicitly labelled as not serving it.

**Board hygiene beats board beauty.**

- The big story backlog lives in Backlog, ranked. Only MVP stories are pulled into the sprint.
- Nothing sits In Progress more than two days. If it does, it is too big or someone is stuck —
  both are standup topics.
- Every task carries an **estimate in hours** before it starts and an **actual** when it
  closes. The gap between those two numbers is the single most valuable thing you will learn
  about your own team this semester.
- One task per person in progress. Two means neither is finishing.

**Cut scope, not corners, and cut it on purpose.** Decide at planning which MVP items are the
"cut first" set. When you are behind — and you will be — you cut from that list in daylight,
at a standup, and you say so in the presentation. Silently shipping something half-done is the
failure mode; a deliberate cut is professional practice.

**Velocity is a measurement, not a target.** Whatever you complete this timebox is the number
you plan the next one with. Inflating it now just moves the pain later.

**The retro produces three actions with owners and dates**, reviewed at the next retro. A
retro that produces feelings and no actions is a meeting.

## Engineering practice

**Definition of Done.** Write it down and hold to it. A reasonable starting point: it compiles
with no new warnings · it plays from a fresh clone · it has a test or a documented manual check
· it was reviewed by someone who did not write it · the board card is closed · anything
surprising about it is written down.

**Review discipline carries over.** The ring, the 24-hour SLA, nobody merging their own work.
Report your review turnaround in the presentation — it is a real engineering metric and it
tells you more about a team than lines of code do.

**A testing beachhead.** You do not need coverage; you need the tests that would have caught
last week's regression. Start with the core loop's state transitions — the thing that must
never break. A system with clean seams is unusually easy to test headlessly: publish the
inputs, assert the states. Five meaningful tests beat fifty trivial ones.

**A build check.** Ideally CI on every pull request. Be warned that Unity in CI needs a
license secret and can eat a day; if that becomes the sprint, stop and settle for a scripted
local build plus a documented pre-merge checklist, and say so. Automating the check matters
more than where it runs.

**Budget for refactoring** — about a tenth of your build hours — and keep a **tech debt log**:
what you deferred, why, and what it will cost. Debt you wrote down is a decision. Debt you
didn't is a surprise.

**Architecture survives its second feature, not its first.** When you add the second enemy,
the second weapon, the second level, notice what you had to touch. If one feature forced edits
in five systems, your seam list was wrong — and saying so, with the evidence, is a better
presentation than a diagram that was never tested.

## Agentic engineering

In Timebox 1 you gave AI a *role* in a chat window. In Timebox 2 you give it a **contract and
a process**, because an assistant that edits your repository is a teammate, and teammates work
inside rules. It shows up twice this timebox: as the divergence engine in the design half, and
as a contributor in the build half.

**1. Write the contract.** An `AGENTS.md` or `CLAUDE.md` at the root of your repo: what the
architecture is, what the conventions are, the folder map, what to do before and after a
change, and what is off-limits. Your systems map and seam list belong in it almost verbatim —
that is how an assistant learns to respect a boundary instead of routing around it. This is
the highest-leverage document you will write this timebox; the runner template ships a working
example.

**2. Encode your repeated operations.** Your story list will show you what you do over and
over: add an enemy type, add a weapon, add a UI panel, add a level. Encode at least one as a
project-specific **skill or slash command**, so it is done your way every time rather than
freshly improvised. The template's `/add-event` and `/audit-events` are that idea applied to
its event system.

**3. Agent work lands like any other work.** Through a branch, through a pull request, through
the review ring. **The author is accountable for every line** and must be able to explain it;
if they cannot, it does not merge.

**4. Guardrails.** Decide what agents may not touch: secrets, licensing and credits data,
third-party folders, build configuration, anything with money or a grade attached. Decide how
you verify: does it still play from a fresh clone, do the tests pass, does your own audit come
back clean.

**5. Keep a run log.** For each non-trivial agent task: what you asked, what it produced, what
you kept, what you rewrote, and whether it saved time or cost it. Ten honest rows are worth
more than a paragraph of enthusiasm, and they make the presentation block write itself.

**The failure mode to watch for** is a velocity chart that looks great and a codebase nobody
on the team can explain. If that is happening, your run log will show it before your grade
does. Reporting it honestly is a better outcome than hiding it.

## Playtesting

You cannot claim your loop is fun. Someone else has to — and a ninety-second greybox slice is
enough to find out.

- **Three or more testers from outside the team**, ten minutes each. Other capstone teams are
  right there and cost nothing.
- **Write the script first**: what you ask them to do, what you are trying to learn, and what
  you are not allowed to say. "Try to reach the exit" — then silence.
- **Watch, don't coach.** The moment you explain the controls, you have destroyed the data you
  came for. Note where they hesitate, what they try that you didn't anticipate, where they
  laugh, and where they give up.
- **Measure something**: time to understand the goal, time to first failure, number of
  attempts before the first success, whether they asked to play again.
- **Ship one change because of it**, and name that change in the presentation. That single
  slide — "they all did X, so we changed Y" — is the most convincing evidence in your deck
  that you are building a game rather than a program.

## The presentation

**Budget: 35–40 minutes plus about 25 minutes of questions**, as in Timebox 1. New presenters.
The running order is a suggestion; the right column is what has to be covered.

| Time | Block | Who | Must cover |
|---|---|---|---|
| 0:00–0:02 | The sprint goal | presenter 1 | the one sentence, and whether you hit it — say it plainly either way |
| 0:02–0:10 | **Design wide → design chosen** (8m) | design owner | the funnel: how many stories you generated, how many made MVP, what you cut and why; the "not this game" list; the pillars the survivors serve |
| 0:10–0:17 | **Systems and separation** (7m) | tech lead | the systems map, the seams and the forbidden pairs, the replace test, and what the Stretch 2 pressure test told you |
| 0:17–0:26 | **The greybox demo** (9m) | demo owner | the core loop, played end to end, by a human, live or recorded |
| 0:26–0:31 | Process and engineering (5m) | scrum master | estimates vs. actuals, velocity, scope cuts, risk register, Definition of Done, review turnaround, tests and the build check |
| 0:31–0:36 | Agentic engineering (5m) | AI lead | AI in the design half and in the build half, the contract, the skill you wrote, the run log, and the honest verdict |
| 0:36–0:40 | Playtest and next timebox (4m) | presenter 2 | what testers actually did, the change you shipped because of it, the next goal and its risks |

**Demo the loop, not the features.** A tour of six systems that never connect is the classic
failure here. One continuous play session that a stranger could follow beats six disconnected
clips.

**Show the funnel as a picture.** "Ninety-four stories → nineteen in MVP → fourteen shipped"
is a better slide than any list, and it is the clearest evidence that your team can choose.

**The freeze still applies**: 48 hours, no new features, and tag the build you demo.

## Easy to forget

- The design freeze date, on the calendar at planning — before anyone is attached to anything.
- Acceptance criteria and estimates on the MVP stories only.
- The "not this game" list, written down where the team can see it.
- The replace test, actually run, with a named system as the answer.
- Three Stretch 2 stories walked through the systems map before the freeze.
- An actual recorded next to every estimate.
- The `.github/` config, standards, and AI policy copied into the new repo on day one.
- Hats rotated — new scrum master, new presenters, new demo owner, plus build/CI, playtest, AI lead.
- Three outside playtesters, and the one change you made because of them.
- The retro's three actions, each with a name and a date on it.
- The freeze, the tag, and a build that runs from a fresh clone on someone else's machine.
- Printed 4-up handout and peer evaluations, before class.
