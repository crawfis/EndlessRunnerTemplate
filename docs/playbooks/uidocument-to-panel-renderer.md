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
| Content on disable | **destroys** content, recreates on enable | **preserves** content in memory |
| Tree access | `rootVisualElement` property, any time | **only via `UIReloaded` callback** (no `rootVisualElement`) |
| Reliable `OnEnable` / LiveReload | flaky (blank-until-toggle bugs) | callback fires on load & live reload |
| World-space UI | limited | improved |

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
| show/hide via `rootVisualElement.style.display` | **`_panel.enabled = true/false`** (content persists) |

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

Prefer the component's `enabled` (leverages content persistence):

```csharp
// BEFORE:  _menuUI.rootVisualElement.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
// AFTER:   _menuPanel.enabled = show;
```

If you need element-level layout control instead of whole-panel toggling, cache `_root` from the
callback and keep using `_root.style.display` — but whole-panel show/hide should use `enabled`.

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
- **Initial visibility:** set it when the root first arrives (in the callback) or via `enabled`,
  not in `Awake` (the tree may not exist yet).
- **`style.display` vs `enabled`:** `enabled=false` now *preserves* content (no destroy/recreate),
  so it's the correct show/hide. Verify it doesn't hide child `PanelRenderer`s unexpectedly via the
  `parentUI` hierarchy in your layout.
- **Serialized references will go null** on the component swap — re-wire them in the Inspector; do
  not try to fix them in YAML.

## Migration checklist (per project)

- [ ] Inventory: `grep -rl "UIDocument" Assets --include=*.cs` and find `UIDocument` components in scenes
- [ ] Convert each controller: field type `UIDocument → PanelRenderer`; move `rootVisualElement`
      access into `OnUIReload`; register/unregister the callback; show/hide via `enabled`
- [ ] Swap each scene component `UI Document → Panel Renderer` in the Inspector; reassign
      PanelSettings/SourceAsset/SortOrder
- [ ] Re-wire every controller reference in the Inspector
- [ ] Play-test every panel: appears, buttons work, show/hide works, no null refs, LiveReload safe
- [ ] Grep confirms zero remaining `UIDocument` / `rootVisualElement` references
