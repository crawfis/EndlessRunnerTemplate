# Known Issues & Unity Caveats

Environment-level quirks that are not bugs in this codebase but will bite you if you don't
know about them.

## Unity 6000.5: UIDocument → Panel Renderer migration — RESOLVED (migrated)

**Status: resolved.** All UI is migrated from `UIDocument` to `PanelRenderer` (merged to `main`,
Unity 6000.5.2f1). The notes below are kept as the record of what bit us, so the pattern is not
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

## JsonUtility binds by exact field name

Unity's `JsonUtility` maps JSON keys to C# fields **by exact name** and silently drops keys it
doesn't recognize — no error, no warning. A mismatch between a JSON key and its field name
therefore fails quietly (this is exactly how the old `ToPivotDistance` vs `EntranceDistance`
track bug hid for a while). When you add or rename a serialized field, keep the JSON key and the
field name identical. See [TRACKS.md](TRACKS.md#normalization-rules-normalizesegments-run-once-at-load).
