# Timebox 3 and Beyond — The Production Rhythm

> CSE 5912 Capstone. The complete Timebox 3 assignment, assembled from the course
> pages on 2026-08-27. Hand this file to your AI assistant as context before asking it to
> help you plan, estimate, or draft anything the assignment asks for.

## Overview and the Rhythm

*CSE 5912 Capstone · Every timebox from here*

This document covers Timebox 3 and every timebox after it. **The rhythm does not change; only
the goal does.** Each timebox: pick one sentence's worth of goal, build it, put it in front of
players, ship a build someone else can run, and show what changed.

Three things are different from Timebox 2:

|  | Timebox 2 | Timebox 3 onward |
|----|----|----|
| **Design** | generate the whole backlog from nothing | re-rank the backlog you have, one timebox ahead |
| **Build** | about half your hours | about two thirds |
| **New work** | — | capture, before/after, a video diary, and light marketing |

That last row is the addition, and it is small: a couple of hours per timebox, owned by one
person. It exists because **the record of how your game changed is worth as much as the change
itself** — for your final presentation, your postmortem, and anyone you ever want to show this
to.

Everything else carries forward unchanged from Timebox 2: the definition of done, the review
ring, the 48-hour freeze, the estimates-versus-actuals discipline, the agent contract, and the
retro that produces three actions with owners.

## The Effort Budget

Unchanged: **10–12 hours per person per week.** The split moves toward building.

```
total person-hours   = members × weeks × 11
build hours          = total × 0.7
plan to              = build hours × 0.7      <-- the number your timebox may hold
```

| Timebox length  | Person-hours | Build hours | You may plan                    |
|-----------------|--------------|-------------|---------------------------------|
| **Two weeks**   | ~132         | ~92         | **~65 hours of estimated work** |
| **Three weeks** | ~198         | ~139        | **~95 hours of estimated work** |

Six-person team; run the same three lines for yours. The remaining 30% is still not slack — it
is the merge that fought back and the estimate that was wrong.

Capture, the devlog, and marketing come out of the non-build hours and should cost **about two
hours per timebox, for one person**. If they cost more, cut them, not the sprint.

## What is Due Every Timebox

| Deliverable | Hat |
|----|----|
| **Sprint goal** — one sentence, testable, set before tasks are assigned | scrum master + design owner |
| **Re-ranked backlog** — MVP for this timebox, with estimates on those stories only | design owner |
| **The build** — tagged, and runnable from a fresh clone on someone else's machine | integrator |
| **Estimates vs. actuals**, and your velocity as a trend across timeboxes | scrum master |
| **Capture set** — stills and a clip at the tag, in the shared archive | capture owner |
| **Before/after** — the same view at the last tag and at this one | capture owner |
| **Video diary** — 60–120 seconds | devlog owner |
| **Living pitch** — one paragraph, updated | design owner |
| **Playtest report** — three or more outside testers, and one change you shipped because of them | playtest owner |
| **Systems map** — updated where reality diverged from it | tech lead |
| **Agent run log**, and any change to the contract | AI lead |
| **Retro** — three actions with owners and dates; last timebox's actions reviewed | scrum master |
| **Presentation, printed 4-up handout, peer evaluations** | deck owner + presenters |

## Design Now That You Have a Backlog

Design does not stop; it shrinks and changes shape.

- **Design one timebox ahead, not five.** Enough detail for what you are about to build, and a
  ranked list behind it.
- **Planning is re-ranking, not re-generating.** The backlog already exists. What changes is
  the order, and it changes because playtests told you something.
- **Feed the backlog from playtests.** Every session produces stories. Add them; they are the
  best-evidenced items you have.
- **Retire stories the game outgrew.** Move them to "not this game" with a one-line reason. A
  backlog nobody prunes is a graveyard nobody reads.
- **Keep the systems map current.** When the code diverges from the map, update the map — then
  say where it diverged and why in the presentation. That is a genuinely interesting slide and
  it is the difference between an architecture and a drawing.
- **Spikes for real unknowns only.** If nobody on the team has done it, size a spike. If three
  people have opinions but no one has tried it, that is also a spike.

## Capture Everything

Screenshots and clips are the cheapest thing you will ever make and the only thing you cannot
make later. **You cannot re-shoot the greybox once the art lands.**

- **One capture owner per timebox** (rotate it, or fold it into the demo owner's hat).
- **Every tag gets a capture set:** five to ten stills and one 30–60 second clip. Unity
  Recorder is already available to you.
- **Fix the setup so comparisons mean something.** Same resolution, same scene, same camera
  position, same lighting preset, every time. A before/after where the camera moved is not a
  before/after; it is two pictures.
- **Name files predictably:** `2026-03-14_v0.4_arena-first-pass_01.png` — date, tag, subject,
  index.
- **Store them outside the game repository** unless your LFS budget is healthy — a shared drive
  or a small separate repo. Never delete anything.
- **Capture the ugly stages and the failures.** Greybox, broken physics, the bug where the
  player launched into orbit. These are the most compelling material in a final presentation
  and the funniest thirty seconds of any devlog.

**Before and after** is then free, and it is required every timebox: the same view at the
previous tag and at this one, side by side, as the second slide of your presentation. Keep the
comparisons in a running reel — one pair per timebox — and by the end of the course you have
the entire semester in ninety seconds that cost you nothing extra.

## The Video Diary

**60 to 120 seconds, once per timebox, owned by one rotating member.** Cut from captures you
already have, so the real cost is the edit.

A structure that works: what we set out to do · what we shipped · what broke · what we learned
· what is next. Voiceover or captions both fine — readable beats polished, and honest beats
either. The timebox where something went badly makes the best episode.

Post it where it can be linked — a devlog entry, an unlisted video, the project wiki — and link
it from the deck. **Every episode gets played twice**: as the teaser that opens the
presentation it was made for, and again as the recap at the top of the next one. Make something
you would be happy to show twice. At the end of the course these episodes are your postmortem,
already written.

## Marketing, lightly

A couple of hours a timebox, not a workstream. The point is to practice describing your game to
someone who does not already know it.

- **A living pitch:** one paragraph, rewritten every timebox. If it got *harder* to write this
  time, your game is drifting — that is useful information, cheaply bought.
- **Who is it for, and where are they already?** One honest sentence. "Everyone" is not an
  audience.
- **A page:** title, tagline, three bullets, three images, one GIF. An itch.io page is free and
  takes an hour, and having a public URL changes how a team talks about its own game.
- **A press-kit folder that accretes:** logo, key art, best stills, GIFs, one-sentence and
  one-paragraph descriptions, and the credits and license register you have been keeping since
  Timebox 1.
- **Key art can be a composed greybox shot.** A good camera angle and one accent color is
  enough to have something to show while the art is still cubes.

## Graduating from Greybox

Art replaces greybox **when the metrics are locked and the loop is fun**, not when someone gets
bored of cubes.

- **Art respects the greybox numbers.** Jump height, reach, gap width, character scale — the
  greybox decided those, and the model conforms to them.
- If art *forces* a metric to change, that is a design decision, not an art decision: make it
  deliberately and **re-playtest**.
- **One system at a time.** Replace the player, ship, capture, playtest. Then the environment.
  Replacing everything at once means you cannot tell which change made it worse.
- **Keep a greybox scene alive** for testing and for the before/after comparisons.
- **Never art something you might cut.** Check the ranking first.

## Running the Timebox

Compressed from Timebox 2; the mechanics are unchanged.

- **One-sentence sprint goal**, fixed before assignment, on a slide.
- **Estimate before, actual after**, on every task. By Timebox 4 you have three data points —
  that is a velocity *trend*, and a trend is the first plan you can actually trust.
- **Nothing In Progress more than two days.** One task per person at a time.
- **A "cut first" set decided at planning**, cut in daylight at a standup, reported honestly.
- **The 48-hour freeze**, the tag, and a build verified from a fresh clone by someone who did
  not make it.
- **The retro reviews last timebox's three actions before writing three new ones.** Actions
  nobody revisits are theatre.

## The Presentation

**35–40 minutes plus about 25 minutes of questions.** New presenters each time.

**Two presenters carry the whole thing.** They deliver every block, they drive the demo, and
they answer every question — including questions about systems they did not build. They are
presenting *as* the team, not introducing it. Each hat still owns its material and briefs the
presenters beforehand; nobody else takes the mic. This is deliberate: preparing to explain
someone else's system is how the team ends up understanding its own project, and it is why the
presenters rotate every timebox.

The running order is a suggestion; the right column is what should be covered.

| Time | Block | Who | Must cover |
|----|----|----|----|
| Time | Block | Who | Must cover |
| --- | --- | --- | --- |
| 0:00–0:04 | **Where we were, where we are** (4m) | presenter 1 | last timebox's one sentence and whether you hit it; **play last timebox's episode** as the recap, then **play the new episode as this talk's teaser**; the before/after pair |
| 0:04–0:16 | **What we implemented** (12m) | both | the technical block — see below. This is the bulk of the talk |
| 0:16–0:25 | **Demo** (9m) | presenter 2 | the loop as it stands now, played end to end, driven by a presenter |
| 0:25–0:29 | Design and playtest (4m) | presenter 1 | what testers did, what you changed because of it, how the backlog was re-ranked, what you cut |
| 0:29–0:34 | **Process and metrics** (5m) | presenter 2 | the numbers — see below — plus velocity trend, estimates vs. actuals, the scope you cut, and the top risks |
| 0:34–0:37 | Agentic engineering (3m) | presenter 1 | the run log in one slide, what changed in the agent contract, what you stopped letting it do |
| 0:37–0:40 | Next timebox and risks (3m) | presenter 2 | the next one-sentence goal, the top three risks, and what you need from the class |

**Both videos open the talk.** Last timebox's episode is the recap; the new one is the teaser
that sets up everything the next thirty minutes explains. Two minutes total, and the room knows
exactly what it is about to hear about. The first time you run this order you will not have a
previous episode — open with the before/after pair alone and start the series here.

### The technical block is the talk

Twelve minutes on **what you implemented and how it works** is the centre of every production
presentation. The class is here to learn how you did it, not merely that you did it.

- **Go system by system.** What it does, how it works, what it touches, what it deliberately
  does not touch.
- **Show the thing, not a screenshot of the thing.** The updated systems map, the data model,
  the state machine, the message flow, a short and genuinely interesting piece of code.
- **Name the hard problem.** What turned out to be harder than you estimated, what you tried,
  what finally worked — or what you are still stuck on. A well-explained failure teaches the
  room more than a clean success.
- **Say where the systems map diverged from the code,** and what you did about it.
- **Cover the tests** you wrote for it and what your Definition of Done demanded.
- **Offer one thing another team could steal**, and say what it cost you to build.

**The presenters did not build all of this**, and explaining it anyway is the exercise. Whoever
built each system briefs the presenter who will explain it — that briefing is where a team
discovers which parts of its own codebase only one person understands.

### The metrics slide

Show the numbers, for the **team and for each person**, as of the freeze tag:

| Member | Commits | PRs opened | PRs reviewed | Lines +/− | Issues closed |
|--------|---------|------------|--------------|-----------|---------------|

GitHub gives you most of it — Insights → Contributors for commits and lines, the pull request
list filtered by author and by reviewer, the board for closed issues.
`git shortlog -sn --no-merges` takes two seconds for the commit column.

Then say what the table **does not** capture, because you will be asked. A day of deleting dead
code shows as negative lines. The person who reviewed twenty pull requests has a modest commit
count and was arguably the most useful member of the team. Whoever ran the playtests, cut the
video, or built the deck may barely appear at all. Present the numbers honestly *and* name the
work that does not show up in them — that pairing is the actual skill.

If a row is genuinely near zero, it is a retro conversation the week before, not a surprise in
front of the class.

**Demo the loop, not the features.** One continuous session a stranger could follow beats six
disconnected clips — every single timebox.

## Easy to Forget

- Capture *before* you replace anything. The greybox does not come back.
- Same camera, same resolution, same lighting — or the before/after says nothing.
- The tag, and a build someone else actually ran.
- The video diary, cut and posted.
- **Both** episodes queued and ready — last timebox's as the recap, the new one as the teaser.
- The living pitch, rewritten — not copied from last time.
- Last timebox's three retro actions, reviewed out loud before new ones are written.
- Printed 4-up handout and peer evaluations, before class.
