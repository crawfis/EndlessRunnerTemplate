# GitHub Copilot instructions

> Thin pointer, kept in sync with `GEMINI.md`. The real guidance lives in AGENTS.md
> — extend that, not this file.

This repository keeps its AI-agent guidance in one root file, written for **any** coding
agent (not just the tools it is named after):

**[AGENTS.md](../AGENTS.md)** holds the mandatory rules (event system, domain isolation, skills
workflow), the code conventions, and a map of the reference docs under `docs/`
(`docs/ARCHITECTURE.md`, `docs/CODEBASE_MAP_AND_KEY_FILES.md`,
`docs/EVENT_BUS_PATTERNS_AND_PAYLOADS.md`, `docs/UNITY_CLI_AND_PIPELINE_PACKAGE.md`).

Read it before changing code. The non-negotiable core:

- **ALL cross-system communication goes through the typed event bus** (`EventsFor<T>`) —
  never direct method calls, `FindObjectOfType`, `SendMessage`, or cross-scene
  `GetComponent`.
- **Domain isolation:** code under `Assets/TempleRun/**` may reference only
  `TempleRunEvents` / `UserInitiatedEvents`; code under `Assets/GameFlow/**` only
  `GameFlowEvents`; code under `Assets/Countdown/**` only `CountdownEvents`. Cross-domain
  event references live ONLY in the four bridges: `TempleRunGameFlowBridge.cs`,
  `Input2TempleRunAutoEventBridge.cs`, `CountdownGameFlowBridge.cs`,
  `Countdown2TempleRunBridge.cs`.
- **Every `Subscribe` (usually in `Awake()`) has a matching `Unsubscribe` in `OnDestroy()`.**
- **Events come first.** Add or change events by following the step-by-step procedures in
  `.claude/skills/<name>/SKILL.md` (plain markdown, tool-agnostic): `list-events`,
  `add-event`, `add-auto-chain`, `add-bridge-mapping`, `add-event-domain`, `audit-events`,
  `generate-segments`. When a doc mentions a slash command such as `/add-event`, that
  means: follow the corresponding skill file.
