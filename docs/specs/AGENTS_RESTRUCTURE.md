# Plan: make AGENTS.md the guide and CLAUDE.md a thin Claude layer

**Status:** planned, not started. This repo goes first; RunnerUGSTemplate ports the result with
its own `docs/specs/AGENTS_RESTRUCTURE.md` once this one has merged.
**Baseline tag:** `pre-agents-restructure`, the 747-line CLAUDE.md as it shipped. Diff
`AGENTS.md`, `CLAUDE.md` and the new docs against that tag to see the before and after.
**Why:** students clone this template, and many work in Codex, Gemini, Copilot or Cursor rather
than Claude Code. Today AGENTS.md (80 lines) says "read CLAUDE.md in full before changing code", so
the rules everyone must follow live in a Claude-specific file of 747 lines, and the pointer runs the
wrong way. After this change AGENTS.md holds every rule in tool-neutral words, reference material
lives in `docs/`, and CLAUDE.md is `@AGENTS.md` plus a few Claude-only notes.

> **How to use:** this doc is written to double as a session prompt: paste it whole into a fresh
> Claude Code session, or point one at the file. Read AGENTS.md and CLAUDE.md first. It is a
> documentation change: no scene, prefab or asset changes, and the only C# edits are two comments
> that cite CLAUDE.md. Work on a branch named `agents-restructure`, one PR.

## Decisions (Roger, 2026-10-22)

1. **Size.** AGENTS.md may grow past the 200-line cap used in the research repos: aim for about
   160 lines, with a hard limit of 300 lines and 24 KB. CLAUDE.md stays under 30 lines.
2. **Links.** Update every link into CLAUDE.md in the same change. That is 35 files today, including
   the student-facing Canvas HTML in `docs/canvas/` and `docs/ai/timebox-*.md`. A heading that some
   link targets by anchor (`#domain-isolation-rule`, `#required-skills-workflow`,
   `#event-flow-architecture`, `#unity-cli-and-the-pipeline-package`, and any other you find) keeps
   its exact text in its new home, so only the file part of each link changes.
3. **New docs.** Three new files, named in full so a student can tell them apart from a listing:
   - `docs/CODEBASE_MAP_AND_KEY_FILES.md`: quick reference, critical paths, key files, folder tree,
     namespaces.
   - `docs/EVENT_BUS_PATTERNS_AND_PAYLOADS.md`: subscribe and publish snippets, typed payloads,
     delivery policy, auto-event flow.
   - `docs/UNITY_CLI_AND_PIPELINE_PACKAGE.md`: the whole Unity CLI section.

   Two existing files receive blocks: `docs/ARCHITECTURE.md` and `docs/KNOWN_ISSUES.md`. Where
   `docs/EVENTS.md` or another doc already holds a block, link to it instead of copying it. The
   CLI doc and the patterns doc must come out byte-identical to the UGS port's copies, so keep
   repo-specific lines out of them.
4. **Pointer files and skills.** `GEMINI.md`, `.github/copilot-instructions.md` (where present) and
   the `audit-events` and `add-event-domain` skills point at AGENTS.md and `docs/ARCHITECTURE.md`
   instead of CLAUDE.md.
5. **The `EventId<T>` ruling note.** Drop the "removed by owner ruling (2026-09, ...)" clause
   everywhere it appears (CLAUDE.md, the `audit-events` skill), the quoted reason included. The rule
   itself stays as one plain line in AGENTS.md: call sites never mint `EventId<T>` fields; declare
   the payload with `[EventPayload]` and go through the bus alias.
6. **Order.** This plan first, then the `pre-agents-restructure` tag in both repos, then a fresh
   session per repo: this repo first, RunnerUGSTemplate after.
7. **`Game_Boot_0_Initialization`.** CLAUDE.md lines 514-515 say it is not in Build Settings. Grep
   cannot confirm that; move the line to `docs/KNOWN_ISSUES.md` as written, and Roger checks it in
   the Editor.

## Steps

1. `git tag -l pre-agents-restructure` must print the tag; branch `agents-restructure` from `main`.
2. Read the [section map](#section-map) below. Line numbers are from the tag; if a block has moved,
   find it by its heading.
3. Create the three new docs, and append to `docs/ARCHITECTURE.md` and `docs/KNOWN_ISSUES.md`, by
   moving blocks verbatim. The only edits allowed while moving: the cuts the map lists, dropping
   version and date notes that have gone stale, and fixing links.
4. Rewrite AGENTS.md. Keep its four sections and add the moved rules under these headings, in this
   order: the 5 MANDATORY rules, Domain Isolation Rule (with its table), Required Skills Workflow,
   Events (naming, typed payloads, delivery, auto-chain, in one or two lines each), Code Style
   (fields, transforms, lifecycle, async), Unity Files (YAML and line endings, scene loading), and
   Safety. Replace "read CLAUDE.md in full" with a doc map row for each new doc.
5. Replace CLAUDE.md with the Claude layer (below).
6. Update every link: `git grep -n -I "CLAUDE\.md" -- ":!CLAUDE.md"`. Point each link at the file
   that now holds its heading. Then update the pointer files and skills (decision 4) and drop the
   ruling note (decision 5).
7. Run the [checks](#checks), then open one PR whose description lists what was cut and why.

## Section map

Actions: **AGENTS** (move into AGENTS.md), **DOC** (move into the named doc), **COVERED** (already
said elsewhere: delete here, and make sure the doc map or a link reaches it), **CUT** (history or a
count that drifts: delete). Short names: MAP = `docs/CODEBASE_MAP_AND_KEY_FILES.md`, PAT =
`docs/EVENT_BUS_PATTERNS_AND_PAYLOADS.md`, CLI = `docs/UNITY_CLI_AND_PIPELINE_PACKAGE.md`, ARCH =
`docs/ARCHITECTURE.md`, ISS = `docs/KNOWN_ISSUES.md`.

| CLAUDE.md lines | Heading | Action | Destination and notes |
|---|---|---|---|
| 1-7 | Title and intro | CUT | Replaced by `@AGENTS.md`; the old pointer is circular |
| 9-16 | Deep-dive docs, course material | COVERED | AGENTS.md doc map; add rows for `docs/ai/` and `EXERCISE_*` |
| 17-21 | Design records, "Phase 3 not started" | CUT | Status lives in `docs/specs/TRACK_PLAYER_DECOUPLING.md`; the doc map row covers the rest |
| 23-40 | Quick Reference, Essential Commands | AGENTS / DOC | Lines 27-30 to AGENTS; Play, logging, domains and track authoring to MAP |
| 42-52 | Critical Paths | DOC | MAP. `docs/ARCHITECTURE.md` (line 215) points back to CLAUDE.md for this: fix that link |
| 54-64 | MANDATORY rules 1-5 | AGENTS | Verbatim |
| 66-69, 77-85 | Domain Isolation rule and table | AGENTS | Keep the 7-row table; it is the rule |
| 70-75 | Replaceability | DOC | ARCH, next to "the big idea" (ARCHITECTURE.md line 7) |
| 87-98 | UserInitiated one-way funnel | AGENTS | Three lines: publish is open, subscribe is closed |
| 100-104 | Violations | AGENTS | Four bullets |
| 106-126 | Validation gates and auto-chains | COVERED | `docs/ADDING_A_MECHANIC.md` (lines 43-65) and `docs/EVENTS.md` (line 273); one rule line in AGENTS |
| 128-132 | How to fix; no asmdefs | AGENTS | Two lines |
| 134-152 | Required Skills Workflow | AGENTS / COVERED | 136-141 already in AGENTS.md; steps 143-152 to AGENTS |
| 154-165 | When to Use Each Skill | COVERED | The steps above plus each skill's description |
| 167-186 | Architecture Overview, Domain Registry | COVERED | Mirror table in `docs/EVENTS.md` and ARCHITECTURE.md line 59; the two invariants (181-186) to AGENTS |
| 188-204 | Bus aliases; singletons removed | DOC / CUT | Alias snippet to PAT; 199-204 CUT (history) |
| 206-238 | Subscribing, Publishing | DOC | PAT |
| 240-281 | Typed Payloads | AGENTS / DOC | Three rules to AGENTS (declare `[EventPayload]`, cast on the handler's first line, comment basic types); snippets to PAT; drop the ruling note (decision 5) |
| 283-302 | Delivery Policy, edge or level | DOC | PAT; one line in AGENTS. CUT the count "only two Sticky events" (it drifts) |
| 304-338 | Auto-Event Flow Pattern | DOC | PAT; CUT the "seven dispatch classes" count |
| 340-352 | Adding New Events | COVERED | `.claude/skills/add-event/SKILL.md`; add a doc map row so non-Claude tools find the steps |
| 354-359 | Event Naming Conventions | AGENTS | Five lines (also in `docs/EVENTS.md` line 22) |
| 363-387 | Namespaces | DOC | MAP, with its DebugLog note |
| 389-405 | Field naming, transforms, lifecycle | AGENTS | About 12 lines |
| 407-449 | Key Files Reference | DOC | MAP |
| 453-461 | Gotchas: Event Subscriptions | AGENTS | Repeats rule 4; keep only the named-struct-over-tuple line |
| 463-482 | Async and coroutines | AGENTS | Five bullets: `Awaitable`, pass the token, no `async void`, realtime waits, no pre-emptive catch |
| 484-487 | Scene Loading | AGENTS / COVERED | Two lines to AGENTS; build order is in ISS (line 34) |
| 489-493 | Auto-Event Flow gotchas | AGENTS | One line: dispatch is synchronous, so loops are possible |
| 495-500 | Singletons | DOC | ISS |
| 502-506 | Unity YAML and Line Endings | AGENTS | A tool-neutral hard rule |
| 508-515 | Dead and placeholder files | DOC / CUT | 511-513 to ISS (still commented out); 514-515 to ISS as written (decision 7); 509-510 CUT |
| 517-542 | CLI intro, Pipeline vs MCP | DOC | CLI |
| 544-561 | The verification loop | COVERED | `.claude/skills/verify-unity/SKILL.md`; the AGENTS rule says "verify after every C# edit" in tool-neutral words |
| 563-576 | Authoring assets | COVERED | `.claude/skills/generate-segments/SKILL.md` |
| 578-598 | CLI gotchas, "verified against 0.7.0-exp.1" | DOC | CLI under a version-free heading; the manifest pins 0.8.0-exp.1 now |
| 600-614 | Safety | AGENTS / DOC | Four bullets to AGENTS; the full text to CLI |
| 616-627 | Testing: verify, run the suite | COVERED / DOC | verify-unity; the `unity test` flags to CLI |
| 629-636 | Event logging, Play | COVERED | MAP quick reference (same lines as 31-34) |
| 638-658 | Gameplay and GameFlow steps | COVERED | Required Skills Workflow |
| 660-673 | Authoring track segments and levels | COVERED | `docs/TRACKS.md` (line 673 already links it) |
| 675-733 | Folder Structure | DOC | MAP; merge with ARCHITECTURE.md line 205 so the tree appears once |
| 735-747 | Event Flow Architecture | COVERED | ARCHITECTURE.md line 109 ("A run, end to end"); keep the heading text there for the anchor |

## The Claude layer

CLAUDE.md after the change, about 20 lines:

```
@AGENTS.md

# Claude Code notes

- Skills in .claude/skills run as slash commands: one line each (/add-event, /add-auto-chain,
  /add-bridge-mapping, /add-event-domain, /audit-events, /list-events, /generate-segments,
  /verify-unity).
- Run /verify-unity after every C# edit (the rule itself is in AGENTS.md).
- Long Unity or build output goes to a file; read its end.
- CLAUDE.local.md (untracked) holds instructor-only notes; never copy it into a tracked file.
```

## Checks

- **Nothing lost.** Every block of the tagged CLAUDE.md is in AGENTS.md, in a doc, or covered by
  the file the map cites, or is on the CUT list. On Roger's machine, run
  `python C:/Repos/Github/Crawfis-Software/UnityAssets/tools/check-lossless.py pre-agents-restructure:CLAUDE.md CLAUDE.md AGENTS.md docs/ .claude/skills/`
  and explain every miss in the PR as CUT or COVERED.
- **Links resolve.** Every `*.md#anchor` link in the repo reaches a heading. Check with a throwaway
  script (don't commit it) that lists each link whose file or heading is missing; it must list
  nothing.
- **No stray pointers.** `git grep -n -I "CLAUDE\.md"` shows only intended mentions: the doc map,
  the pointer files and the Claude-layer notes.
- **Sizes.** AGENTS.md at most 300 lines and 24 KB; CLAUDE.md under 30 lines.
- **Line endings.** `git ls-files --eol` shows no `w/mixed` on any changed file.
- **Compile.** The two C# files that cite CLAUDE.md in comments (`Assets/TempleRun/Scripts/Events/TempleRunEvents.cs`,
  `Assets/_Common/Utility/Wait.cs`) change their comments only; run `/verify-unity` once at the end.
