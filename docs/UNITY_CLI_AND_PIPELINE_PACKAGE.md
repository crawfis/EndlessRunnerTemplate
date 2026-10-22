# Unity CLI and the Pipeline Package

This project has `com.unity.pipeline` installed, which runs a small HTTP command server **inside
the Editor**. The `unity` CLI talks to that server, so an assistant can compile, read the console,
inspect and author assets, and drive Play Mode — against the *real* Editor, not a guess.

**Use it. Do not verify a C# change by reading the code and asserting it compiles.**

```
unity pipeline list        # is the Editor up and reachable?
unity cmd                  # list every available command
unity cmd <name> --help    # arguments for one command
```

Add `--no-banner --result-only` to every call for clean, parseable output (`--json` when you need
to parse it).

## Pipeline vs MCP — one engine, two doors

`com.unity.pipeline` is the engine: the in-Editor server that registers the commands. There are two
clients for it — the `unity cmd` CLI, and `unity mcp`, which re-exposes the *same* commands as MCP
tools. Neither is more capable than the other.

**Write procedures against the CLI.** Skills here must be followable from any AI tool (see
[AGENTS.md](../AGENTS.md)), and a `unity cmd` line works in any of them; an MCP tool call only works in
a client that has the server wired up.

## The verification loop

After any C# edit — follow [`.claude/skills/verify-unity/SKILL.md`](../.claude/skills/verify-unity/SKILL.md) (`/verify-unity`):

```bash
unity cmd recompile --no-banner --result-only          # returns immediately
unity cmd recompile_status --no-banner --result-only   # poll until terminal
unity cmd console --tail 30 --level error --no-banner --result-only
unity cmd console_status --no-banner --result-only     # ground truth
```

`recompile` uses Unity's own compilation — its defines, its asmdefs, its package versions. That
makes it strictly better than running `dotnet build` on the generated `Assembly-CSharp.csproj`,
which can drift from what Unity actually builds. Prefer it.

`status: up_to_date` means *nothing needed compiling* — if you just wrote a file, the Editor
probably hasn't imported it yet. Give the Editor focus (`unity cmd editor_focus`) and retry rather
than reading it as success.

## Authoring assets

Let Unity create assets; never hand-write `.asset` / `.meta` YAML. `create_asset` mints a real GUID,
writes the correct field layout, and keeps the AssetDatabase consistent — which removes GUID
collisions and field drift as a class of bug. See `/generate-segments` for the worked pattern.

```bash
unity cmd create_asset --path "<path under Assets>" --type "<Namespace.Type>" --confirm true
unity cmd set_serialized_field --target "Assets/<full path>" --field <Name> --value <v>
unity cmd get_serialized_fields --target "Assets/<full path>"    # read back and confirm
```

Arrays and lists use Unity's serialized-property paths — `Tags.Array.size`, then
`Tags.Array.data[0]`. Object references take a handle: `'{"path":"Assets/…/Thing.asset"}'`.

## Gotchas

- **`set_serialized_field` does not write to disk.** It mutates the in-memory object only. The
  `.asset` keeps its old values — and git shows nothing — until you flush:
  ```bash
  unity cmd eval --code 'UnityEditor.AssetDatabase.SaveAssets(); return "saved";' --timeout 60
  ```
  Forgetting this produces assets that look correct in the Inspector and are empty on disk.
- **`eval` main-thread work times out at 5 s.** Pass `--timeout 60`, or you get
  `Main thread operation timed out after 5000ms`.
- **An unfocused Editor doesn't tick.** `unity cmd set_autotick --enable true --interval_ms 100`
  keeps it serving commands in the background.
- **`batch` rejects asset/file/settings writes** unless `transactional=false` — they mutate outside
  the Undo system.
- **Commands return a result object even when a value didn't land.** Always read back with
  `get_serialized_fields` before reporting success.
- **Play Mode may not reset statics.** If `m_EnterPlayModeOptions` in
  `ProjectSettings/EditorSettings.asset` disables domain reload, `static` state —
  `EventsFor<T>` subscriber lists, `Blackboard`, `GameState` — **survives between Play sessions**.
  A duplicate subscription that only appears on the second run is this, not your code.
- **Always `editor_stop`** after `editor_play`; a stuck Play Mode blocks later commands.

## Safety

Unity's own guidance is to treat this like remote code execution — it can do anything you can do in
the Editor. Accordingly:

- **Preview destructive work first.** Most mutating commands take `--dry_run true`; the genuinely
  destructive ones (`delete_asset`, `switch_build_target`, `clear_*`) require `--confirm true`.
  Run the dry run, read it, then commit to the change.
- **Respect the authoring root.** File and asset writes are confined to it (`get_authoring_root`;
  currently `Assets`). Don't widen it to work around a path problem.
- **`eval` / `run_script` are arbitrary code execution.** Reach for a registered command first and
  use `eval` only where none exists (the `AssetDatabase.SaveAssets()` flush is the standard case).
  Local development only.
- **Don't commit incidental churn.** Editor-driven work can dirty scenes and settings. Check
  `git status` before committing and stage only what the task intended.

## Running the tests

`unity test --mode EditMode` (or `PlayMode`) runs the project's tests and writes a report. Useful
flags: `--affected` (only tests a change can reach), `--rerun-failed`, `--retries <n>` to surface
flaky tests, `--report-format junit` for CI.
