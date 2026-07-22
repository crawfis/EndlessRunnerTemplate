# Phase 3 — Scene component swap checklist (do in the Unity Inspector)

Phases 1–2 (the C# controller changes) are done on branch `feature/panel-renderer`. All 7
controllers now expect a **`PanelRenderer`** instead of a `UIDocument`. This checklist makes the
in-editor swap mechanical.

**Do NOT hand-edit the `.unity` YAML** — Unity renumbers component ids during the swap and nulls
serialized references (this is the exact failure recorded in `../KNOWN_ISSUES.md`). Do every step in
the Inspector.

## Per-GameObject procedure (repeat for each row below)

1. Select the GameObject (names are exact).
2. On its **UI Document** component, write down its three values from the table (already filled in
   below, but confirm against the scene): **Source Asset (UXML)**, **Panel Settings**, **Sort Order**.
3. **Remove** the `UI Document` component.
4. **Add Component → Panel Renderer**.
5. Re-assign on the new Panel Renderer: **Source Asset / Visual Tree Asset**, **Panel Settings**, and
   **Sort Order** (Sort Order may live on the PanelSettings in the new component — set it wherever the
   Panel Renderer exposes it; match the old value).
6. Re-wire the controller field(s) in the **Re-wire** column — the field type changed from
   `UIDocument` to `PanelRenderer`, so Unity shows it as `None (Panel Renderer)` until you drag the
   GameObject's new Panel Renderer onto it.
7. Save the scene.

Do **Game_Boot_1_UI** first, then the two gameplay scenes.

## Scene: `Assets/GameFlow/Scenes/Boot/Game_Boot_1_UI.unity`

| GameObject | UXML (Source Asset) | Panel Settings | Sort Order | Re-wire controller field(s) |
|---|---|---|---|---|
| **MainMenu** | `MainMenu.uxml` | `PS_Menu.asset` | 0 | `MainMenuController._panel` **and** `MainMenuPanelController.menuUI` |
| **LevelSelection** | `LevelSelector.uxml` | `PS_Menu.asset` | 0 | `LevelSelectorController._panel` **and** `LevelSelectorPanelController._levelSelectorUI` |
| **LoadingPanel** | `LoadingScreen.uxml` | `PS_Loading.asset` | 1000 | `GameFlowUIPanelController.loadingUI` |
| **Overlay-GameOver** | `GameOver.uxml` | `PS_Overlay.asset` | 0 | `GameFlowUIPanelController.gameOverUI` |
| **Feedback** | ⚠ *missing UXML (see note)* | `PS_Feedback.asset` | 0 | *none — no controller references it* |

Notes for this scene:
- **MainMenu** and **LevelSelection** each carry **two** controllers referencing the same panel (a
  query controller + a show/hide controller). Both fields must point at the one new Panel Renderer.
- **Feedback**'s UI Document already has a **broken/missing Source Asset** (its UXML GUID resolves to
  no file in the project — pre-existing, unrelated to this migration). No C# references it, so it is
  **out of scope**. You can migrate it to Panel Renderer for consistency, but there is no controller
  field to re-wire and the missing UXML must be fixed separately if you want it to render.

## Scene: `Assets/TempleRun/Scenes/Gameplay/TempleRunGuiOverlay.unity`

| GameObject | UXML (Source Asset) | Panel Settings | Sort Order | Re-wire controller field(s) |
|---|---|---|---|---|
| **UI** | `TempleRunDistances.uxml` | `New Panel Settings.asset` | 0 | `GUIController._panel` |

Note: this panel uses the default-named `New Panel Settings.asset`. Reassign it as-is (or rename it
for clarity — unrelated to this migration).

## Scene: `Assets/TempleRun/Scenes/Gameplay/TempleRunGameplay.unity`

| GameObject | UXML (Source Asset) | Panel Settings | Sort Order | Re-wire controller field(s) |
|---|---|---|---|---|
| **CountdownController** | `Countdown.uxml` | `PS_Overlay.asset` | 0 | `CountdownUIController._countdownPanel` |

*(All PanelSettings assets live in `Assets/Settings/GUI/`.)*

## Phase 4 — play-test gate (after the swaps)

Run through these; the migration is a **semantic** change (content persistence + `enabled` show/hide
replace destroy-on-disable + `style.display`), so static checks don't cover it:

- Boot → **Main Menu renders on first show, no toggle needed**; Play and Quit work.
- Level selector: cards populate, Back works, show/hide on navigation.
- Loading and Game Over overlays show/hide at the right times.
- Countdown shows 3-2-1 then hides; HUD distance/death labels update during play.
- No `NullReferenceException`; no "missing UI Document" / "No Theme Style Sheet" warnings.
- `/audit-events` clean (subscription hygiene is unaffected by this migration).
- Grep confirms zero `UIDocument` / `rootVisualElement` in `Assets/**/*.cs` (already true after Phase 2).

### ⚠ The one assumption to verify first

The MainMenu and LevelSelection panels each have a **show/hide controller that sets
`panel.enabled = false` in `Awake`** (MainMenu only when `GameState.IsMainMenuActive` is false;
LevelSelection always) **and a query controller that wires its buttons in the `UIReloaded` callback.**
This assumes a Panel Renderer still **loads its visual tree and fires `UIReloaded` even after a sibling
disabled it in `Awake`** (Panel Renderer preserves content when disabled, which implies the tree is
built at load regardless of `enabled`).

If in play-testing a panel that starts disabled comes up **blank or its buttons don't respond** (i.e.
`UIReloaded` never fired because it loaded while disabled), the localized fix is to apply the same
trick `CountdownUIController` already uses: let the panel start **enabled**, register the callback,
and set `enabled = false` **inside** the first `OnUIReload` (content is cached, then hidden). That
would move the initial-hide out of the panel controller's `Awake` and into the query controller's
callback for those two panels.

## On completion

When Phase 4 passes, flip the UIDocument section of `../KNOWN_ISSUES.md` to "resolved: migrated to
Panel Renderer" and delete the `style.display` workaround note.
