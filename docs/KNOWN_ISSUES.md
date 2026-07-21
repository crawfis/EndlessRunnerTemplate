# Known Issues & Unity Caveats

Environment-level quirks that are not bugs in this codebase but will bite you if you don't
know about them.

## Unity 6000.5: UIDocument → Panel Renderer migration

**Symptom.** Opening the project can migrate the scene's `UIDocument` components (YAML class
`u!114`) into the newer **"Panel Renderer"** component (`u!1931382934`). When it does, the
`UIDocument`-typed serialized references in the UI controllers (e.g.
`MainMenuController._uiDocument`, `MainMenuPanelController.menuUI`,
`LevelSelectorController._uiDocument`) are **nulled**, and the affected panels render blank or
throw `NullReferenceException` in `OnEnable`.

**Also seen:** a panel (in practice the Main Menu) rendering blank on first show until its
GameObject is deactivated and reactivated. This is the same UI Toolkit rendering path.

**Why.** This is Unity's own serialization/rendering migration in 6000.5, not something in this
project. Unity itself surfaces the recommendation: *"Consider migrating to Panel Renderer, the
updated UI rendering component… The UI Document component will continue to be available but no
longer receive new features."* The rendering quirk reproduces in the source project too.

**What this template already does about it.**
- Panel show/hide uses `style.display = DisplayStyle.Flex/None` (not `rootVisualElement.visible`),
  which reliably repaints and fixed the loading, game-over, level-selector, and countdown panels.
- The scenes are committed with clean `u!114` UIDocuments and correct references.

**If a panel still renders blank / loses its UIDocument reference:**
1. Don't hand-edit the `.unity` YAML to "fix" a UIDocument reference — the migration renumbers
   the component ids, so a hand-wired id points at the wrong component after the next open.
   Re-assign the reference in the **Inspector** instead (drag the panel's UI Document onto the
   controller field), or restore the scene from a clean commit.
2. The durable fix, when you're ready, is to **migrate the panels to Panel Renderer** per
   Unity's recommendation and re-wire the controller references to the new components.

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
