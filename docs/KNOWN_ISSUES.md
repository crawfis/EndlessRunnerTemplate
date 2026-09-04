# Known Issues & Unity Caveats

Environment-level quirks that are not bugs in this codebase but will bite you if you don't
know about them — plus, at the end, the architecture smells that *are* ours.

## Unity 6000.5: UIDocument → Panel Renderer migration — RESOLVED (migrated)

**Status: resolved.** All UI is migrated from `UIDocument` to `PanelRenderer` (merged to `main`
under Unity 6000.5.2f1; the project is now on 6000.5.7f1). The notes below are kept as the record of what bit us, so the pattern is not
re-broken when adding panels. Reusable guide:
[playbooks/uidocument-to-panel-renderer.md](playbooks/uidocument-to-panel-renderer.md);
per-scene record: [Phase 3 checklist](specs/PANEL_RENDERER_PHASE3_CHECKLIST.md).

**Original symptom.** Opening the project in 6000.5 auto-migrated the scene's `UIDocument`
components (YAML `u!114`) into `PanelRenderer` (`u!1931382934`) and **nulled** the `UIDocument`-typed
controller references → blank panels / `NullReferenceException`. A panel (in practice the Main Menu)
also rendered blank on first show until its GameObject was toggled.

**The rules that make PanelRenderer behave (do not regress these):**
- **Show/hide via `root.style.display`, keep every `PanelRenderer` enabled.** Do **not** toggle
  `PanelRenderer.enabled` for show/hide, and do **not** author a panel's `Enabled` checkbox off.
  Disabling tears the visual tree down; a panel disabled before its first init (in `Awake` **or**
  authored disabled) hits **Unity bug UUM-146174** — `UIReloaded` never fires on a later enable, so
  it's blank until a manual toggle. Controllers force `enabled = true` in `OnEnable` as a backstop.
- **No `rootVisualElement`** — cache `root` from the `UIReloaded` callback; re-cache queried elements
  on every callback.
- **Do the component swap in the Inspector, never in YAML** (Unity renumbers ids and nulls refs).

**Boot/return event flow (non-UGS):** showing the menu depends on `GameplayReady`, which this
template drives via the durable auto-chains `LoadingScreenHidden → GameplayReady` (boot) and
`GameEnded → GameplayReady` (post-game) in `GameFlowAutoEventFlow.cs` — not via the
`Test_AutoFireEvent*` scene helpers.

## Build-order dependency for scene unloading

`UnloadNonActiveScenes._lastSceneIndexToKeep` assumes:
- the entry scene (`0_BootStrap_Game_Only`) is **build index 0**, and
- the gameplay scenes are **last** in the Build Settings scene list.

On `GameEnded` it unloads every scene with `buildIndex > _lastSceneIndexToKeep`. If you reorder
the Build Settings list, re-check the keep-index or you'll unload the wrong scenes (or fail to
unload gameplay). See [ARCHITECTURE.md](ARCHITECTURE.md#load--unload-mechanics).

## JsonUtility binds by exact field name (historical — track data is now ScriptableObjects)

Unity's `JsonUtility` maps JSON keys to C# fields **by exact name** and silently drops keys it
doesn't recognize — no error, no warning. It also writes **enums as integers** and ignores a string
aimed at an enum field. Track segments were once authored as JSON, and both hazards bit:

- A C# field and its registry JSON key drifted apart (`ToPivotDistance` vs `EntranceDistance`), so
  every turn segment silently fell back to `ToPivotDistance = Length`, corrupting turn geometry.
- An authored `"Direction": "Left"` bound to a `Direction` enum field bound nothing at all, leaving
  every segment at value 0 — `Direction.Left` — so the track only ever turned left and straights
  were built as turns.

**Both are gone:** track data now lives in ScriptableObjects (`TrackSegmentSO`,
`TrackSegmentRegistrySO`, `TrackLevelSO`). Native SO serialization gives enums an Inspector
dropdown, makes renames compile-time-safe, and makes key/field drift impossible. See
[TRACKS.md](TRACKS.md#the-data-model).

The rule still applies to any *remaining* `JsonUtility` use — notably the PlayerPrefs save blob in
`LevelProgressManager` (which round-trips a matching C# type, so it is safe). If you add a new
`JsonUtility`-serialized field, keep key and field name identical and never aim a string at an enum.

## Architecture smells: the countdown straddles the GameFlow/TempleRun boundary — RESOLVED (Countdown extracted)

**Status: resolved (2026-09-04).** All three misplacements below were fixed structurally, by
extracting the countdown into its own **Countdown** event domain (`CountdownEvents`,
`Assets/Countdown/`) rather than by patching the bridge table. Analysis:
[specs/DOMAIN_DECOMPOSITION.md](specs/DOMAIN_DECOMPOSITION.md) §3–§4; change list:
[specs/COUNTDOWN_DOMAIN.md](specs/COUNTDOWN_DOMAIN.md). What each smell became:

- **#1** — GameFlow now chains `GameStarting → GameStarted` in its own table, so no gameplay
  or ceremony event decides a session milestone; the ceremony's end is translated into
  gameplay's own words instead, `CountdownEnded → PlayerActivateRequested`.
- **#2** — The home was chosen: neither GameFlow nor TempleRun, but its own session-ceremony
  domain, recorded in the Domain Registry.
- **#3** — Controller and UXML now live together: `CountdownController`,
  `CountdownUIController`, and `Countdown.uxml` are all under `Assets/Countdown/`.

The original writeup is kept below as course material — the smell, why it mattered, and why
it was worth writing down before it was fixed. Read it as the "before" picture; the "after"
is [ARCHITECTURE.md](ARCHITECTURE.md#a-run-end-to-end) and
[EVENTS.md](EVENTS.md#countdownevents-session-ceremony).

---

Unlike the entries above, these are ours, not Unity's. Three related misplacements, all
visible on one sequence diagram (the talk deck points at them on the "One run, end to end"
slide). None of them breaks the game today; each one breaks a promise the architecture makes.

**1. `CountdownEnded → GameStarted` — a gameplay detail decides a session milestone.**
`TempleRunGameFlowBridge.cs` maps
`(TempleRunEvents.CountdownEnded, GameFlowEvents.GameStarted)`, and the reverse table maps
`(GameFlowEvents.GameStarted, TempleRunEvents.TempleRunStartRequested)`. So "the game has
started" is decided *inside gameplay*, bridged into GameFlow, and bridged straight back:

```
GameFlow: GameStarting ─► TempleRun: CountdownStartRequested ─► 3… 2… 1…
          GameStarted  ◄─ TempleRun: CountdownEnded
          GameStarted  ─► TempleRun: TempleRunStartRequested
```

This defeats the replaceability claim the domain split exists for. Swap TempleRun for a
runner with no countdown — the thing [EXERCISE_DRAW_THE_BOUNDARY.md](EXERCISE_DRAW_THE_BOUNDARY.md)
and the domain-isolation rule promise you can do — and nothing ever publishes
`GameStarted`, so the session hangs before the first step. GameFlow should own "started";
gameplay should only report that it is *ready*.

**2. The countdown's domain is arbitrary.** A "3… 2… 1…" before a run is session ceremony,
the same category as the loading screen and the game-over overlay — both of which live in
GameFlow. It sits in TempleRun by accident of history, not because a boundary argument put
it there. Either home is defensible; what is not defensible is that no one chose. This is a
live question for the *Draw the Boundary* exercise.

**3. The UXML did not follow the code.** `CountdownUIController` is in TempleRun
(`Assets/TempleRun/Scripts/UI/`), but the assets it drives are still under GameFlow:
`Assets/GameFlow/UI Toolkit/UI/UXML/Overlays/Countdown.uxml` and
`Gameplay/HUD.uxml`. The controllers were moved across the boundary; the visual assets were
not. `GameFlowUIPanelController` even carries the comment recording the move ("countdown UI
is now managed by TempleRun `CountdownUIController`").

**Why this is worth keeping written down rather than quietly fixing.** The event
architecture did not prevent any of this — it made it *visible*. The whole cross-domain
surface is 19 lines in two bridge files, so the flaw is one readable line in a table
(`TempleRunGameFlowBridge.cs`) rather than a coupling buried in a controller, and the fix is
an edit to that table plus a folder move. A codebase where systems called each other
directly would have the same flaw and no place to point at it.
