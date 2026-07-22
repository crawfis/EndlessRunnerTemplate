# Playbook: Migrating `UIDocument` → `PanelRenderer` (Unity 6.5+)

A **project-agnostic** guide for moving UI Toolkit UI from the obsolete `UIDocument` component to
the modern `PanelRenderer` component. Copy this file into any project doing the same upgrade.

> Sources: Unity Manual, *Create a UI with the UI Document component / Migration to Panel Renderer*
> (`docs.unity3d.com/Manual/UIE-create-ui-document-component.html`) and the `PanelRenderer`
> Scripting API (`docs.unity3d.com/ScriptReference/UIElements.PanelRenderer.html`).

## Why

As of **Unity 6.5**, `UIDocument` is **obsolete** (still works, but can no longer be added to new
GameObjects and receives no new features). `PanelRenderer` is its native replacement. Opening a
project in 6.5 may prompt or auto-migrate; doing it deliberately avoids surprises.

### Behavioural differences that matter

| | `UIDocument` (old) | `PanelRenderer` (new) |
|--|--------------------|------------------------|
| Rendering | needs a hidden companion renderer | **native `Renderer` component** |
| Content on disable | destroys content, recreates on enable | **also tears the tree down** (disable removes visual elements + cleans up resources); enable rebuilds it and re-fires `UIReloaded` |
| Tree access | `rootVisualElement` property, any time | **only via `UIReloaded` callback** (no `rootVisualElement`) |
| Reliable `OnEnable` / LiveReload | flaky (blank-until-toggle bugs) | callback fires on load & live reload — **but see the Awake-disable bug below** |
| World-space UI | limited | improved |

> **Correction / caveat.** Early guidance (including earlier drafts of this playbook) claimed
> `PanelRenderer` *preserves* content when disabled. It does **not** — the Unity Manual states
> "when you disable it, Unity removes its visual elements from the hierarchy and cleans up
> resources," and re-enabling rebuilds the tree and fires `UIReloaded` again. Treat `enabled` as a
> teardown/rebuild toggle, and **re-cache any queried elements in every `UIReloaded`**, since the
> old element references are dead after a disable.

## API mapping

| Old (`UIDocument`) | New (`PanelRenderer`) |
|--------------------|------------------------|
| `using UnityEngine.UIElements;` | same (both live in `UnityEngine.UIElements`) |
| `[SerializeField] UIDocument _doc;` | `[SerializeField] PanelRenderer _panel;` |
| `_doc.rootVisualElement` | **cache the `root` from the reload callback** (no direct property) |
| `_doc.panelSettings` | `_panel.panelSettings` |
| `_doc.visualTreeAsset` | `_panel.visualTreeAsset` |
| `_doc.sortingOrder` | (set via `panelSettings` / inspector) |
| `gameObject.AddComponent<UIDocument>()` | `gameObject.AddComponent<PanelRenderer>()` |
| show/hide via `rootVisualElement.style.display` | **`_panel.enabled = true/false`** (teardown/rebuild; re-caches on the next `UIReloaded`) |

### Callback registration

```csharp
void OnEnable()  => _panel.RegisterUIReloadCallback(OnUIReload);
void OnDisable() => _panel.UnregisterUIReloadCallback(OnUIReload);

// Two delegate flavours exist:
//   UIReloadCallback          -> (PanelRenderer renderer, VisualElement root)
//   VersionedUIReloadCallback -> (PanelRenderer renderer, VisualElement root, int version)
void OnUIReload(PanelRenderer renderer, VisualElement root)
{
    _root = root;                      // cache for later show/hide & queries
    _playButton = root.Q<Button>("BtnPlay");
    // Re-wire handlers here. The callback can fire again (LiveReload),
    // so make wiring idempotent (unhook before hooking, or guard).
}
```

## Code transformation patterns

### Pattern 1 — "query elements once" (controllers that grab buttons/labels)

```csharp
// BEFORE
private void OnEnable() {
    var root = _uiDocument.rootVisualElement;
    _btn = root.Q<Button>("BtnPlay");
    _btn.clicked += OnClick;
}

// AFTER
private VisualElement _root;
private void OnEnable()  => _panel.RegisterUIReloadCallback(OnUIReload);
private void OnDisable() => _panel.UnregisterUIReloadCallback(OnUIReload);
private void OnUIReload(PanelRenderer r, VisualElement root) {
    _root = root;
    if (_btn != null) _btn.clicked -= OnClick;   // idempotent across reloads
    _btn = root.Q<Button>("BtnPlay");
    _btn.clicked += OnClick;
}
```

### Pattern 2 — "show/hide a panel"

> **Hard-won guidance (Unity 6.5.2):** the "obvious" approach — toggle `_panel.enabled` for
> show/hide — is **unreliable** and cost us several iterations. Use `style.display` on the cached
> root instead, and keep the `PanelRenderer` **enabled at all times**. Reasons:
> - Disabling tears the tree down; re-enabling is subject to **UUM-146174** (a `PanelRenderer`
>   disabled before its first init — in `Awake`, or authored disabled in the scene — may never fire
>   `UIReloaded` again on enable, so it renders **blank until a manual toggle**).
> - Enabling re-triggers `UIReloaded`, which races any handler that (re)reads external state to
>   decide visibility — it can clobber a show that already happened.
>
> Keeping the panel enabled and toggling `root.style.display` sidesteps all of it: the tree is
> built once and stays alive, and visibility is a pure style flip.

```csharp
// BEFORE:  _menuUI.rootVisualElement.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;

// AFTER — panel stays enabled; controller owns the desired-visibility state:
private VisualElement _root;
private bool _visible;                       // desired state, set from Awake + show/hide events

void OnEnable() {
    _panel.RegisterUIReloadCallback(OnUIReload);
    _panel.enabled = true;                   // ensure the tree builds even if authored disabled
}
void OnDisable() => _panel.UnregisterUIReloadCallback(OnUIReload);

void OnUIReload(PanelRenderer r, VisualElement root) {
    _root = root;
    ApplyVisibility();                       // re-apply desired state whenever the tree (re)arrives
}
void SetVisible(bool v) { _visible = v; ApplyVisibility(); }
void ApplyVisibility() {
    if (_root != null) _root.style.display = _visible ? DisplayStyle.Flex : DisplayStyle.None;
}
```

Set the initial `_visible` in `Awake` (a plain bool — no tree needed); apply it on the first
`OnUIReload`. **Every** `PanelRenderer` that must render needs to be enabled — force
`_panel.enabled = true` in `OnEnable` so a stray disabled checkbox in the scene can't silently
break it.

## Scene / Inspector steps (do these IN UNITY, not by hand-editing `.unity` YAML)

Hand-editing the YAML is fragile — Unity renumbers component ids during migration and nulls
serialized references. Do the swap in the Inspector:

1. Select the GameObject with the `UI Document` component.
2. Note its **Panel Settings**, **Source Asset (VisualTreeAsset)**, and **Sort Order**.
3. Remove the `UI Document` component; add a `Panel Renderer` component.
4. Re-assign **Panel Settings**, **Source Asset / visualTreeAsset**, and **Sort Order** on it.
5. Re-assign every controller field that referenced the old `UIDocument` to the new `PanelRenderer`
   (the field type in code changed, so Unity shows it as "missing"/None until re-dragged).
6. Repeat per panel; save the scene.

## Gotchas

- **No `rootVisualElement`.** Any code reaching the tree outside the callback breaks. Cache `root`
  from `OnUIReload` into a field and use that.
- **Always `UnregisterUIReloadCallback` in `OnDisable`/`OnDestroy`** — mirror every register.
- **The callback can fire more than once** (LiveReload). Make element wiring idempotent (unhook
  handlers before re-hooking).
- **⚠ Don't toggle `enabled` for show/hide — Unity bug UUM-146174.** A `PanelRenderer` disabled
  before its first init completes (set `enabled = false` in `Awake`, **or authored disabled in the
  scene**) **no longer fires `UIReloaded`** on a later `enable`, so the tree never builds and the
  panel is **blank until a manual Inspector toggle**. Confirmed by Unity staff. This is why Pattern 2
  keeps the panel **enabled** and toggles `root.style.display` instead. Force `_panel.enabled = true`
  in `OnEnable` so a scene-authored disabled checkbox can't break rendering. (Alternative some teams
  use: keep open/close logic on a separate GameObject and toggle the *panel's whole GameObject*
  `SetActive` — a full GameObject reactivation does re-fire `UIReloaded`.)
- **`enabled` is a teardown/rebuild, not a hide.** Disabling a `PanelRenderer` removes its visual
  elements and frees resources; enabling rebuilds and re-fires `UIReloaded` (content is **not**
  preserved). So if you ever do disable/enable, re-cache queried elements on every `UIReloaded`.
- **Initial visibility:** track a `_visible` bool set in `Awake`, applied to `root.style.display` in
  the first `UIReloaded` — not by toggling `enabled` in `Awake`.
- **Every panel that must render needs `enabled = true`.** Multiple `PanelRenderer`s can share one
  `PanelSettings` (supported); each still needs its own component enabled. Verify child/parent
  visibility via the `parentUI` hierarchy in your layout.
- **Serialized references will go null** on the component swap — re-wire them in the Inspector; do
  not try to fix them in YAML.

## Migration checklist (per project)

- [ ] Inventory: `grep -rl "UIDocument" Assets --include=*.cs` and find `UIDocument` components in scenes
- [ ] Convert each controller: field type `UIDocument → PanelRenderer`; move `rootVisualElement`
      access into `OnUIReload`; register/unregister the callback; **show/hide via `root.style.display`
      with the panel kept enabled** (force `enabled = true` in `OnEnable`)
- [ ] Swap each scene component `UI Document → Panel Renderer` in the Inspector; reassign
      PanelSettings/SourceAsset/SortOrder
- [ ] Re-wire every controller reference in the Inspector
- [ ] **Leave every PanelRenderer's `Enabled` checkbox ON** (don't disable "start hidden" panels —
      that trips UUM-146174; hide them via `style.display` instead)
- [ ] Play-test every panel: appears, buttons work, show/hide works, no null refs, LiveReload safe
- [ ] Grep confirms zero remaining `UIDocument` / `rootVisualElement` references
