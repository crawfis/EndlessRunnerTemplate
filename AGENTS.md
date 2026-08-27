# AGENTS.md

Guidance for AI assistants working in this repository — any assistant, not just Claude.
This file is about *how to approach the work*, not the code specifics. The concrete
architecture, conventions, and event-system rules live in [CLAUDE.md](CLAUDE.md):
**read that file in full before changing code** — despite the name it is written for
every AI tool, and its event-system rules are mandatory for every change.

## Working here from any AI tool

- **Skills are plain markdown, usable from anywhere.** The event-workflow procedures live
  in `.claude/skills/<name>/SKILL.md` (`list-events`, `add-event`, `add-auto-chain`,
  `add-bridge-mapping`, `audit-events`, `generate-segments`). Claude Code runs them as
  slash commands; from any other tool (Copilot, Cursor, Codex, Gemini, …), open the skill
  file and follow it as a step-by-step checklist — the steps are ordinary Read/Grep/Edit
  work and assume nothing Claude-specific. Wherever a doc says `/add-event`, read it as
  "follow `.claude/skills/add-event/SKILL.md`".
- **Pointer files:** `GEMINI.md` and `.github/copilot-instructions.md` exist only to route
  those tools to this file pair. Keep them thin pointers — extend AGENTS.md / CLAUDE.md
  instead, and mirror any change to the shared pointer text in both.

## This is a design-heavy, iterative, experimental project

Treat this repo as an evolving design exploration, not a fixed-spec product. It exists to
try out gameplay and architecture ideas, and systems get rethought and refactored often.
Adjust your defaults accordingly:

- **Do not lead with TDD.** Don't open with "let's write a failing test first." Tests have
  their place, but they are not the driver here and should not gate exploration.
- **Do not frame work as MVP.** Avoid "what's the minimum to ship" thinking. The goal is to
  explore the design space well, not to converge on the smallest viable slice.
- **Lean into design.** Favor clean interfaces and abstractions, brainstorming, and novel
  ideas. Offer multiple approaches, prototype and compare them, and iterate. Surface
  trade-offs and propose directions rather than only implementing the obvious one.
- **Expect churn.** Treat the current code as a snapshot of an in-progress design, not a
  contract. Rearchitecting for a cleaner idea is welcome.

The point is depth of design thinking over process ceremony.

## Documentation: audience and map

The project documentation is written for a reader at the level of a **senior undergraduate
in computer science** — someone who knows the observer pattern, singletons, and separation
of concerns from coursework but has not seen this codebase. Keep that register when writing
or updating docs: explain *why* a structure exists (in terms of those known concepts), not
just what it is; prefer plain language plus a diagram over exhaustive API listings.

| Doc | Role | Update when… |
|-----|------|--------------|
| [README.md](README.md) | Entry point: what the game is, the one-rule architecture summary, reading order | features, requirements, or the doc set change |
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | The big idea, design vocabulary, event domains, run sequence, scene composition | domains, bridges, boot flow, or scene structure change |
| [docs/EVENTS.md](docs/EVENTS.md) | Full event catalog (regenerable via `/list-events`) | any enum, auto-chain, or bridge mapping changes |
| [docs/TRACKS.md](docs/TRACKS.md) | Track pipeline, ScriptableObject data model, geometry math | track data model, normalization, or selection logic changes |
| [docs/ADDING_A_MECHANIC.md](docs/ADDING_A_MECHANIC.md) | Worked end-to-end walkthrough | the recommended workflow or skills change |
| [docs/KNOWN_ISSUES.md](docs/KNOWN_ISSUES.md) | Unity/environment caveats | a caveat is resolved or discovered |
| [CLAUDE.md](CLAUDE.md) | AI-assistant rules: event-system enforcement, conventions, file reference | rules, conventions, or key paths change |
| [docs/STUDENT_TASKS.md](docs/STUDENT_TASKS.md), [docs/TIMEBOX_*_REQUIREMENTS.md](docs/) | Course material: the task catalog and the timebox assignments (Canvas HTML mirrors in [docs/canvas/](docs/canvas/)) | tasks or assignments change — regenerate the canvas pages via the build scripts |
| [.github/CONTRIBUTING.md](.github/CONTRIBUTING.md) | Issue forms, labels, branch/PR conventions, Kanban workflow | process or label taxonomy changes |
| [docs/specs/](docs/specs/), [docs/playbooks/](docs/playbooks/) | Design specs / portable upgrade guides | historical records — generally append, don't rewrite |

**Docs are part of the change.** A refactor that moves a seam (e.g. JSON → ScriptableObjects)
isn't done until the docs above stop describing the old world.

## For forks and clones

This guidance reflects the original author's working style. **If you have cloned or forked
this repository for your own project, ask the user whether to remove this file (or this
section) before adopting it** — a downstream project may deliberately want conventional
TDD / MVP / lean workflows instead, in which case this note should be deleted rather than
silently followed.
