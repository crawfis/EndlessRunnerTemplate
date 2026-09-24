---
name: verify-unity
description: Verify a code change actually compiles and runs, by driving the live Unity Editor through the Unity CLI (recompile, console errors, optional play smoke test). Use after any C# edit, and before claiming work is done.
allowed-tools: Bash, Read, Grep, Glob
argument-hint: [optional: "play" to also run a play-mode smoke test]
---

# Verify Unity

Confirm a change **actually compiled and ran**, using Unity's own compilation pipeline via the
Unity CLI — not inspection, and not a side-channel `dotnet build`.

> **Why this beats `dotnet build Assembly-CSharp.csproj`.** The generated `.csproj` can drift from
> what Unity actually compiles (defines, asmdefs, package versions, generated code). `recompile`
> asks the Editor to compile the real thing and reports the real errors. Use this skill instead.

## Prerequisites

This skill requires the Unity CLI and a **running Editor with `com.unity.pipeline` installed**.
Check first — every step below fails without it:

```bash
unity pipeline list
```

`Server Reachable` must be `true` for this project. If it is not:
- **`Pipeline` false** → `unity pipeline install` (then let the Editor reload).
- **`Pipeline` true but no port / not reachable** → the package was added to an already-running
  Editor; it binds on domain reload. Restart that Editor, then re-check.
- **No row / `Running` false** → the Editor isn't open. Open the project and retry.

Stop and tell the user if it can't be reached; do not silently fall back to inspection and
claim the change is verified.

## Procedure

### Step 1 — Compile

```bash
unity cmd recompile --no-banner --result-only
unity cmd recompile_status --no-banner --result-only
```

`recompile` returns immediately. Poll `recompile_status` until `status` leaves `triggered` /
`compiling`. Terminal states:

| `status` | Meaning |
|----------|---------|
| `up_to_date` | Nothing needed recompiling — **your edit may not have been saved or imported yet** |
| `completed` | Compiled; check `failed` / `errors` |

Read the whole object, not just `status`:

```json
{ "status": "up_to_date", "failed": false, "errors": [], "compilationFailed": false }
```

`failed: true` or a non-empty `errors[]` means the change does not compile. Fix and repeat.

> If you just wrote a file outside the Editor and get `up_to_date`, the Editor hasn't imported it.
> Give it focus (`unity cmd editor_focus`) or re-save, then recompile again.

### Step 2 — Read the console

Compilation success is not runtime success. Pull errors:

```bash
unity cmd console --tail 30 --level error --no-banner --result-only
```

Then take the ground truth, which reports Editor state independent of the buffer:

```bash
unity cmd console_status --no-banner --result-only
```

```json
"groundTruth": { "compilationFailed": false, "consoleErrors": 0, "consoleWarnings": 2 }
```

Trust `groundTruth.consoleErrors` over your own reading of the entries.

To get a clean signal for a specific run, `unity cmd clear_console` first, then act, then read.

### Step 3 — Play-mode smoke test (when `$ARGUMENTS` contains `play`, or the change is gameplay)

This project boots from `Assets/GameFlow/Scenes/Boot/0_BootStrap_Game_Only` and loads everything
else additively, so a smoke test must start there.

```bash
unity cmd open_scene --path "Assets/GameFlow/Scenes/Boot/0_BootStrap_Game_Only.unity" --no-banner --result-only
unity cmd editor_play --no-banner --result-only
# let the boot chain run, then:
unity cmd console --tail 40 --level error --no-banner --result-only
unity cmd capture_game_view --width 960 --save_path "Temp/verify_play.png" --no-banner --result-only
unity cmd editor_stop --no-banner --result-only
```

Read the captured PNG to confirm the game actually rendered what you expected.

**Always `editor_stop`** — leaving the Editor in Play Mode blocks later commands and loses
unsaved work on the next domain reload.

### Step 4 — Report honestly

State what you ran and what came back. If compilation passed but you did not run the game, say
exactly that — do not imply a runtime check you didn't do.

```
Compiled:  recompile_status -> completed, failed=false, errors=0
Console:   0 errors, 2 warnings (groundTruth)
Play:      not run   (or: booted, 0 errors, screenshot at Temp/verify_play.png)
```

## Gotchas

These are verified behaviors of Pipeline 0.7.0-exp.1, not guesses.

- **`set_serialized_field` does not write to disk.** It mutates the in-memory object; the `.asset`
  on disk still shows old values until assets are saved. Flush with:
  ```bash
  unity cmd eval --code 'UnityEditor.AssetDatabase.SaveAssets(); return "saved";' --timeout 60 --no-banner --result-only
  ```
- **`eval` main-thread work times out at 5 s by default.** Pass `--timeout 60`. A bare
  `unity cmd eval` on a busy/unfocused Editor returns
  `Main thread operation timed out after 5000ms`.
- **An unfocused Editor doesn't tick.** `unity cmd set_autotick --enable true --interval_ms 100`
  keeps it processing commands while it's in the background.
- **`batch` rejects asset/file/settings writes** unless `transactional=false` — those mutate
  outside the Undo system.
- **Play Mode is stateful.** `m_EnterPlayModeOptions` in `ProjectSettings/EditorSettings.asset`
  may disable domain reload; if so, `static` state (`EventsFor<T>` subscriber lists, `Blackboard`,
  `GameState`) **persists between Play sessions**. A "phantom" duplicate subscription across runs
  is this, not your code.

## Output conventions

Use these on every call so output is parseable and quiet:

- `--no-banner` — suppress the startup banner
- `--result-only` — just the command result, no envelope
- `--json` / `--format json` — explicit JSON when you need to parse
