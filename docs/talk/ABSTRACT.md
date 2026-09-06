# Abstract — "It's *Just* an Endless Runner"

Submission copy for conferences, meetups and course catalogs. Pick the length the CFP asks
for; they are all the same talk.

**The pitch is the trajectory, not the architecture.** Every version below leads with the
lived problem — *why does the seventh feature take five times longer than the second?* — and
with what the attendee walks out able to do. The technique is named once, late, and quietly.
Program committees see a great many "clean architecture" talks; they see almost none that
promise an artist they can ship their own runner. Lead with that.

Numbers are verified against `main` — re-check them with the fact sheet in
[TALK_OUTLINE.md](../TALK_OUTLINE.md#fact-sheet-verified-numbers) before submitting, since a
stale number is the one thing an audience will catch.

---

## Title

**It's *Just* an Endless Runner** — what a "simple" game is actually made of

*Alternate subtitles, depending on the room:*
"— and how to build one that gets **easier** as it grows" (programmer-heavy) ·
"— a runner you can make your own" (art- and design-heavy)

*Alternate titles, if the CFP wants punchier:* "The Weekend Project That Wasn't" ·
"Why Your Third Month Is Slower Than Your First" · "Nobody Calls Anybody"

---

## Long form (~295 words — the default; fits a 300-word cap)

> "It's just an endless runner. Weekend, tops."
>
> Every developer has said some version of that, and every developer has been wrong. The
> interesting question is *why* — it isn't that the mechanic is hard. It's that the second
> feature takes a day, the seventh takes a week, and nobody can point at the moment it went
> bad. Meanwhile your artist is waiting on a programmer to wire up a particle, and five
> people are queued to edit the same file.
>
> This talk opens up a deliberately plain runner — capsule player, flat track, seven verbs —
> and counts what's really inside. Two thirds of it is menus, loading and pause, not running.
> Then the decision that quietly sets a project's trajectory: **how the pieces are allowed
> to talk to each other.** Not a framework. One rule, kept without exception.
>
> **If you write code,** you'll leave with a test to run on your own project: does each
> finished feature make the next one *cheaper*, or more expensive? And a way to make the
> answer cheaper — adding a jump sound becomes one new file instead of an edit to the file
> five people share.
>
> **If you make art, sound or levels,** you'll leave with something more direct: a runner you
> can make *yours* without engine surgery. New look, new world, new tracks — authored in the
> Inspector, with an AI agent writing the connective code. The weekend estimate is badly
> wrong for building a runner. It's about right for making this one your own.
>
> Four live demos, including an AI that takes over the player mid-run and an agent that ships
> an artist's feature without touching gameplay code. Plus the part most talks skip: where we
> broke our own rule, and what it cost.
>
> Both repos are open source.

## Medium form (~145 words)

> Why does the second feature take a day and the seventh take a week? Nothing got harder —
> the pieces just got tangled, and now every new idea has to negotiate with every old one.
>
> This talk takes apart a deliberately plain endless runner to show the one decision that
> sets a project's trajectory: how its pieces are allowed to talk to each other. Programmers
> leave with a test to run on their own codebase — *does each finished feature make the next
> one cheaper?* — and a way to make the answer yes. Artists and designers leave with
> something more direct: a runner they can make their own, new look and new world and new
> tracks, without ever opening a gameplay script.
>
> Four live demos, including an AI that takes over the player and an AI agent that ships a
> feature without touching gameplay code.

## Short form (~60 words — for a schedule grid)

> The second feature takes a day; the seventh takes a week. This talk shows the one decision
> that sets that trajectory — and hands artists and designers a runner they can make their
> own, without opening a gameplay script. Four live demos, including an AI that takes over
> the player mid-run. Open source, so you can start from it.

## One-liner

> Why your third month is slower than your first — and how to build a game, or hand one to
> your artists, so it isn't.

---

## What attendees will walk out able to do

Framed as outcomes rather than concepts, which is what most CFPs are actually asking for.

1. **Run one diagnostic on their own project.** Does each finished feature make the next one
   cheaper or more expensive? It's answerable in an afternoon, and the answer predicts the
   next six months better than any estimate.
2. **Change that answer.** The practice that makes features *additive* rather than
   negotiated — plus, honestly, what the discipline costs to keep and where it needs
   process rather than a compiler.
3. **Hand real work to non-programmers.** Which seams let an artist, a sound designer or a
   level designer ship something visible without engine surgery — and how to describe those
   seams so they're usable rather than theoretical.
4. **Draw boundaries that make pieces replaceable instead of editable** — demonstrated live
   by replacing the player, the contributor, and the backend, none of which the code was
   designed to allow.
5. **Write onboarding docs that work on models too.** The same properties that get a new
   hire productive in week one are what let an AI agent contribute without breaking things.

## The artist and designer version of this talk

Worth spelling out in the submission when the venue is art- or design-heavy, because it's
the part that makes this talk unusual. The template is built so the following need **no
engine changes at all**:

- **Re-skin it.** Visuals, audio and environment live in their own scenes and only react to
  what gameplay announces. Replace the art; the game underneath never notices.
- **Re-world it.** Track segments and whole level rulesets are authored assets edited in the
  Inspector — a designer adds a segment or a level without a programmer and without a queue.
- **Re-feel it.** Trails, particles, camera shake, screen effects, a whole sound pass: each
  one is a new file that listens for something that already happens. Nobody has to open the
  gameplay code, and nothing that already works can break.
- **Get the glue written for you.** The repo ships its conventions in plain markdown that any
  AI coding tool can read, so "make coins sparkle when collected" is a brief you hand to an
  agent, and you spend your time on the curve, the material, the timing and the mix.

Be honest about the ceiling: the plain runner is a *starting point*, not a product. The talk
says so, and a companion tutorial series
([TUTORIAL_SERIES.md](../TUTORIAL_SERIES.md)) is the proposal for closing that gap.

## Audience and level

**Intermediate, and deliberately mixed** — this is a talk for a whole team, not a track for
one discipline. Designers, artists and producers get the "what does this let me do" version
throughout; five slides in the middle are explicitly marked as programmer-depth and
signposted so everyone else knows they can relax for six minutes. No Unity expertise is
required to follow the argument — the examples are Unity, the lesson isn't.

## Format

- **45 minutes + Q&A** (default), with four live demos, each with a recorded fallback.
- **30-minute cut** available; the cut map is the last slide of the deck's appendix.
- **Half-day workshop variant** replaces the two demo slides in Act IV with hands-on time —
  pairs, one small task each, agent-assisted. Plan in
  [TALK_OUTLINE.md](../TALK_OUTLINE.md#if-you-run-act-v-as-a-hands-on-exercise-workshop--class-variant).
- **A/V needs:** the presenting machine runs Unity 6.6 for the demos; the slides are a
  single self-contained HTML file. Network is nice-to-have (one demo calls an AI agent), not
  required.

## For the technical reviewer

Some CFPs route to a technical reviewer who will want the mechanism named. One paragraph,
kept out of the audience-facing copy:

> The rule is publish/subscribe applied with zero exceptions: systems never call each other,
> they publish named events. Concretely, the template carries **210 named events across four
> isolated domains, 152 scripts, 15 scenes** loaded additively, with **49 auto-chain rules and
> 19 cross-domain bridge mappings declared as data** rather than code. The talk covers the
> typed bus, the request→starting→started naming ladder and its one deliberately manual
> validation gate, control flow as inspectable tables, and delivery policy for late
> subscribers — and it owns the costs: no ctrl-click from cause to effect, silent failure when
> nothing is listening, and no compiler enforcement, which is paid for with a naming
> convention, a generated catalog, and an audit run before merge.

## Speaker bio (trim to the CFP's word limit)

> Roger Crawfis is a professor at The Ohio State University, where he teaches the game
> capstone sequence. He maintains two open-source Unity teaching templates — an endless
> runner and its live-services sibling — that student teams use to ship a game in a
> semester. <!-- Add: research background, prior credits, previous talks. -->

## Submission notes

- **Lead with the trajectory, not the architecture.** "Why is month three slower than month
  one" is a problem everyone in the room has lived. "Event-driven architecture" is a solution
  they've heard pitched before.
- **The artist promise is the differentiator.** Most architecture talks are for programmers
  only. This one gives a program chair a session that art, design and engineering can all
  sit in — say so explicitly if there's a "why this talk" field.
- **If the CFP wants "why you?":** the demos are the answer. An AI player, an AI contributor
  and a cloud backend all shipping through seams nobody designed for them is unusual, and
  it's demonstrable on stage rather than asserted.
- **Don't oversell.** The honesty slide — our own bug, drawn on our own diagram — is worth
  mentioning in the submission. Committees notice a speaker who volunteers what didn't work.
