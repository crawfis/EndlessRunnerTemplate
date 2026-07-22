# Plan: Migrate this project's UI from `UIDocument` to `PanelRenderer`

**Status:** proposal / ready to execute
**Why:** the project is on **Unity 6.5** (`6000.5`), where `UIDocument` is obsolete and
`PanelRenderer` is the native replacement. This is also the fix for the blank-until-toggled Main
Menu quirk in [../KNOWN_ISSUES.md](../KNOWN_ISSUES.md) — Panel Renderer's `UIReloaded` callback and
content persistence remove the class of first-render bugs we worked around with `style.display`.

Follow the portable steps and API mapping in
[../playbooks/uidocument-to-panel-renderer.md](../playbooks/uidocument-to-panel-renderer.md); this
doc is the project-specific scope, order, and verification.

## Scope

**7 controllers** reference `UIDocument`, in two patterns:

| Controller | Pattern | Notes |
|-----------|---------|-------|
| `GameFlow/Scripts/UI/MainMenuController.cs` | query | `root.Q<Button>` Play/Quit/(SignOut hidden) |
| `GameFlow/Scripts/UI/LevelSelectorController.cs` | query | `root.Q` LevelContainer + BtnBack |
| `TempleRun/Scripts/UI/GUIController.cs` | query | HUD labels in `Awake` |
| `TempleRun/Scripts/UI/CountdownUIController.cs` | query **+ show/hide** | `Q<Label>("Countdown")` + display toggle |
| `GameFlow/Scripts/UI/MainMenuPanelController.cs` | show/hide | `menuUI` display toggle |
| `GameFlow/Scripts/UI/LevelSelectorPanelController.cs` | show/hide | `_levelSelectorUI` display toggle |
| `GameFlow/Scripts/UI/GameFlowUIPanelController.cs` | show/hide (×2) | `gameOverUI` + `loadingUI`, `SetActive` helper |

**Scenes with `UIDocument` components** (swap in the Inspector, not YAML):
- `GameFlow/Scenes/Boot/Game_Boot_1_UI.unity` — MainMenu, LevelSelection, Loading, GameOver, Feedback
- `TempleRun/Scenes/Gameplay/TempleRunGuiOverlay.unity` — HUD
- `TempleRun/Scenes/Gameplay/TempleRunGameplay.unity` — countdown UI (confirm which object owns it)

## Target patterns for this codebase

- **Field:** `UIDocument x` → `PanelRenderer x`.
- **Query controllers** (MainMenu, LevelSelector, GUI, Countdown): move the `rootVisualElement.Q<>`
  calls into an `OnUIReload(PanelRenderer, VisualElement root)` handler; cache `root`; register in
  `OnEnable`, unregister in `OnDisable`; make button wiring idempotent.
- **Show/hide controllers** (MainMenuPanel, LevelSelectorPanel, GameFlowUIPanel, Countdown): replace
  `rootVisualElement.style.display = …` with `panel.enabled = show`. Delete the `style.display` code
  added earlier — `enabled` + content persistence supersedes it. For `GameFlowUIPanelController.SetActive`,
  drop the `gameObject.SetActive` + `display` combo in favour of `panel.enabled`.
- **Initial visibility** (e.g. `MainMenuPanelController` reading `GameState.IsMainMenuActive` in `Awake`):
  set `panel.enabled` from that flag in `Awake`/`Start` (safe — `enabled` doesn't need the tree), and
  do any element wiring in the callback.

## Phased execution

Each phase is small and independently verifiable. **Code phases can be automated; the scene component
swaps and reference re-wiring must be done in the Unity Inspector** (hand-editing the migrated YAML is
what caused the earlier reference-nulling — see KNOWN_ISSUES).

**Phase 1 — Code: query controllers.** Convert MainMenuController, LevelSelectorController,
GUIController, CountdownUIController to the callback pattern. Compiles but references are still
`UIDocument`-typed in the scene until Phase 3 — so gate this with Phase 3 or expect temporary missing
refs. (Recommendation: do Phases 1–2 together on a branch, then Phase 3 in-editor in one pass.)

**Phase 2 — Code: show/hide controllers.** Convert MainMenuPanelController, LevelSelectorPanelController,
GameFlowUIPanelController to `panel.enabled`. Remove the `style.display` workarounds.

**Phase 3 — Scenes (in Unity Inspector).** For each panel GameObject: record PanelSettings/SourceAsset/
SortOrder → remove `UI Document` → add `Panel Renderer` → reassign those three → re-wire the controller
field(s) to the new `PanelRenderer`. Save. Do Game_Boot_1_UI first, then the two gameplay scenes.

**Phase 4 — Verify (play-test).**
- Boot → **Main Menu renders on first show, no toggle needed**; Play/Quit work.
- Level selector: cards populate; Back works; show/hide on navigation.
- Loading + Game Over overlays show/hide at the right times.
- Countdown shows 3-2-1 then hides; HUD updates.
- No `NullReferenceException`; no "No Theme Style Sheet"/"missing UI Document" warnings.
- `/audit-events` clean (subscription hygiene unaffected).
- Grep: zero remaining `UIDocument` / `rootVisualElement` in `Assets/**/*.cs`.

## Risks & notes

- **Semantic change, not behaviour-preserving.** Content persistence and `enabled`-based show/hide
  differ from destroy-on-disable + `display`. Phase 4 play-testing is the gate, not static checks.
- **Callback timing.** Element wiring now happens on `UIReloaded`, which may fire after `Awake`/first
  frame. Anything assuming elements exist in `Awake` must move to the callback.
- **`parentUI` hierarchy.** If panels are nested, verify `enabled=false` on a parent doesn't hide a
  child unexpectedly; if it does, toggle the specific child panel or use cached-root `style.display`.
- **Do the scene swaps in-editor.** Re-wiring in YAML is fragile (renumbered ids); the Inspector is the
  supported path.
- On completion, flip [../KNOWN_ISSUES.md](../KNOWN_ISSUES.md)'s UIDocument section to "resolved:
  migrated to PanelRenderer" and remove the `style.display` note.
