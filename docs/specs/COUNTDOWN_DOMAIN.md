# Implementation spec: the Countdown domain (Phase A of DOMAIN_DECOMPOSITION.md)

**Status:** approved for implementation. Analysis and rationale live in
[DOMAIN_DECOMPOSITION.md](DOMAIN_DECOMPOSITION.md) §3–§4; KNOWN_ISSUES' three countdown
smells are the motivation. This file is the exact change list.

**Verified preconditions** (do not re-derive):
- No `.unity`/`.asset`/`.prefab`/`.uxml` file references any countdown event name,
  `TempleRunScenesReady`, `TempleRunLevelApplied`, `TempleRunStarted`, or `PlayerActivate*`
  — every rename/move below is serialization-safe.
- `CountdownController` and `CountdownUIController` are hosted in `TempleRunGameplay.unity`
  (script GUIDs `ffe56579…`, `162538dd…`). Moving the `.cs` **with its `.meta`** preserves
  the GUID and therefore the scene wiring — do not create new metas for moved files.
- `TempleRunGameFlowBridge` (GUID `b4fe4b66…`) is hosted in `Game_Boot_2_Play.unity`; its
  GameObject block is the pattern to replicate for hosting the new components.
- `TempleRunEvents` members are safe to renumber/rename/delete; `GameFlowEvents` members
  must keep their values.

## The target flow

```
GameFlow:  GameScenesLoaded → GameStartRequested → GameStarting
             ├─(new chain)──► GameStarted ──bridge──► TempleRun: TempleRunStartRequested…Started   (systems up, pre-countdown)
             └─(bridge)─────► Countdown: CountdownStartRequested → Starting → Started → Tick… → Ending → Ended
Countdown: CountdownEnded ──bridge──► TempleRun: PlayerActivateRequested → PlayerActivating → PlayerActivated   (player go)
```

## 1. New files (each new `.cs` gets a new `.meta` with a fresh random GUID, LF endings, format copied from a sibling meta)

### `Assets/Countdown/Scripts/Events/CountdownEvents.cs`
Namespace `CrawfisSoftware.Countdown.Events`, `[EventEnum]`:
```csharp
CountdownStartRequested = 0,
CountdownStarting = 1,
CountdownStarted = 2,
CountdownTick = 3,        // keeps its current payload behavior (whatever CountdownController publishes today)
CountdownEnding = 4,
CountdownEnded = 5,
```
`CountdownCancelled` is deliberately **dropped** (nothing referenced it — L13 dead member;
note this in the header comment). No payload declarations unless `CountdownController`
turns out to declare one today (it does not in `TempleRunEvents`).

### `Assets/Countdown/Scripts/Events/CountdownAutoEventFlow.cs`
`internal class CountdownAutoEventFlow : AutoEventFlowBase<CountdownEvents, CountdownEvents>`,
chain table carried over from TempleRunAutoEventFlow's countdown block **including its
comments** (the "GO! flash / start-line delay goes here" seam note):
```csharp
(CountdownEvents.CountdownStartRequested, CountdownEvents.CountdownStarting),
(CountdownEvents.CountdownEnding,        CountdownEvents.CountdownEnded),
```

### `Assets/GameFlow/Scripts/CountdownSpecific/CountdownGameFlowBridge.cs`
Namespace `CrawfisSoftware.GameFlow.Events`.
`internal class CountdownGameFlowBridge : AutoEventFlowBase<GameFlowEvents, CountdownEvents>`
(one direction only, so the base class works — no need for the two-dispatcher pattern):
```csharp
(GameFlowEvents.GameStarting, CountdownEvents.CountdownStartRequested),
```
Comment: session milestone → ceremony trigger; the ceremony's outcome goes to gameplay,
not back here — GameFlow owns `GameStarted` via its own chain.

### `Assets/Countdown/Scripts/TempleRunSpecific/Countdown2TempleRunBridge.cs`
Namespace `CrawfisSoftware.Countdown.Events`.
`internal class Countdown2TempleRunBridge : AutoEventFlowBase<CountdownEvents, TempleRunEvents>`:
```csharp
(CountdownEvents.CountdownEnded, TempleRunEvents.PlayerActivateRequested),
```
Comment: the translation seam — in gameplay vocabulary, the countdown's end means exactly
"release the player". TempleRun cannot tell whether a countdown, a cutscene, or nothing
sat between `TempleRunStartRequested` and `PlayerActivateRequested`.

## 2. File moves (`git mv`, ALWAYS moving `.cs`+`.meta` / `.uxml`+`.meta` together)

| From | To | Edits after move |
|---|---|---|
| `Assets/TempleRun/Scripts/Player/CountdownController.cs` | `Assets/Countdown/Scripts/CountdownController.cs` | namespace → `CrawfisSoftware.Countdown`; bus alias → `using CountdownBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.Countdown.Events.CountdownEvents>;`; all `TempleRunEvents.Countdown*` → `CountdownEvents.Countdown*` |
| `Assets/TempleRun/Scripts/UI/CountdownUIController.cs` | `Assets/Countdown/Scripts/UI/CountdownUIController.cs` | namespace → `CrawfisSoftware.Countdown.UI`; same bus retarget |
| `Assets/GameFlow/UI Toolkit/UI/UXML/Overlays/Countdown.uxml` | `Assets/Countdown/UI Toolkit/Countdown.uxml` | First check the uxml for relative `src=` stylesheet/template references; if any exist, either move the referenced asset too (with meta) or fix the path. GUID-based references need nothing. |

New folders need folder `.meta` files too (`Assets/Countdown.meta`, etc. — copy the format
of an existing folder meta, fresh GUIDs).

## 3. `TempleRunEvents.cs` edits

1. **Delete** the whole `// ---------- Countdown ----------` category (values 30–36).
2. **Add** to the player-lifecycle category:
```csharp
// Bridged from the Countdown domain: the ceremony's end, translated into player terms.
// Both links below are chained in TempleRunAutoEventFlow; a spawn-in animation or grace
// period later breaks one, with no controller edit.
PlayerActivateRequested = 14,
PlayerActivating = 15,
PlayerActivated = 16,
```
3. **Rename** (values and attributes unchanged): `TempleRunScenesReady` →
   `RunInitializeRequested` (302); `TempleRunLevelApplied` → `TrackLevelApplied` (304).
   Update the adjacent comments to native vocabulary ("begin the run's initialization",
   "the selected track level" — see DOMAIN_DECOMPOSITION §3). Leave the
   `TempleRunConfigApplied`/`TempleRunDifficulty*` members **untouched** (deferred to
   Phase B; they carry a "name is baked into assets" warning).

## 4. `TempleRunAutoEventFlow.cs`
- Remove the countdown chain block (both entries + its comments — they migrate to
  `CountdownAutoEventFlow`).
- Add, in the player-lifecycle area:
```csharp
(TempleRunEvents.PlayerActivateRequested, TempleRunEvents.PlayerActivating),
(TempleRunEvents.PlayerActivating,        TempleRunEvents.PlayerActivated),
```
with a comment noting both links are deliberately chained seams (per the ladder
philosophy in CLAUDE.md).

## 5. `TempleRunGameFlowBridge.cs`
- Remove `(TempleRunEvents.CountdownEnded, GameFlowEvents.GameStarted)` from
  TempleRun→GameFlow and `(GameFlowEvents.GameStarting, TempleRunEvents.CountdownStartRequested)`
  from GameFlow→TempleRun (both replaced by the Countdown domain's bridges).
- Update the two renamed targets: `TempleRunLevelApplied` → `TrackLevelApplied`,
  `TempleRunScenesReady` → `RunInitializeRequested`.
- Update the class/table comments accordingly (the file's "absorbed from GameController"
  notes about the countdown no longer apply).

## 6. `GameFlowAutoEventFlow.cs`
- Add to the GAME SESSION section:
```csharp
(GameFlowEvents.GameStarting, GameFlowEvents.GameStarted),
```
with comment: GameFlow owns its own milestone; the countdown ceremony runs in parallel
off `GameStarting` (CountdownGameFlowBridge) and releases the player via the Countdown →
TempleRun bridge — no gameplay or ceremony event decides `GameStarted` any more
(KNOWN_ISSUES countdown smell #1).
- Update the big timeline comment: the COUNTDOWN section and the `GameStarted` lines now
  reflect the new flow (GameStarted fires at ceremony start; countdown lines move to the
  Countdown domain; player release is `PlayerActivateRequested`).

## 7. Retargets — `TempleRunStarted` meant two things; these four files meant "player go"

In each: retarget the `TempleRunEvents.TempleRunStarted` subscribe + unsubscribe + handler
naming to `TempleRunEvents.PlayerActivated`. Keep every `TempleRunEnded` /
`TempleRunStartRequested` / `TempleRunStarting` usage as is. Read each file before
editing; preserve reset semantics and comment style.

| File | Note |
|---|---|
| `Assets/TempleRun/Scripts/GameTime.cs` | the run clock must not advance during the countdown |
| `Assets/TempleRun/Scripts/Player/DistanceController.cs` | distance starts at release |
| `Assets/TempleRun/Scripts/Player/AIController.cs` | the autopilot arms at release |
| `Assets/TempleRun/Scripts/Player/TurnCollisionDetector.cs` | failure detection arms at release |
| `Assets/TempleRun/Scripts/Audio/Metronome.cs` | added after the play test: the beat paces the run, and pre-activation `CurrentSpeed` is 0 — a tick scheduled at `TempleRunStarted` divides by it and stalls forever after one click |

Files that deliberately KEEP `TempleRunStart*` (systems up during the ceremony):
`SetMusicPlayer` (music under the countdown — play-test confirmed as intended),
`PlayerLifeController`, `SegmentAdvanceTrigger`, `LaneChangeController`. Do not touch them.

## 8. Renamed-member call sites
`Assets/TempleRun/Scripts/Track/TrackManager.cs`: `TempleRunScenesReady` (×2) →
`RunInitializeRequested`; `TempleRunLevelApplied` (×1) → `TrackLevelApplied`.
Then `grep -r "TempleRunScenesReady\|TempleRunLevelApplied\|TempleRunEvents.Countdown"`
must return zero hits in `Assets/`.

## 9. Compile check (Editor may be open)
`Assembly-CSharp.csproj` is Unity-generated and now stale: **edit it directly** (it is
gitignored) — fix the paths of the moved files and add `<Compile Include="...">` entries
for the five new `.cs` files — then `dotnet build Assembly-CSharp.csproj` and fix every
error. A build that never saw the new files does not count as passing.

## 10. Scene wiring — `Assets/GameFlow/Scenes/Boot/Game_Boot_2_Play.unity`
Add ONE new root GameObject `CountdownDomain` carrying THREE MonoBehaviour components:
`CountdownAutoEventFlow`, `CountdownGameFlowBridge`, `Countdown2TempleRunBridge`.
Hand-editing rules:
- Copy the YAML shape of the existing `TempleRunGameFlowBridge` host object in this scene
  (GameObject + Transform + MonoBehaviour documents). The new MonoBehaviours have no
  serialized fields beyond the standard header.
- `m_Script` guids come from the three new `.meta` files written in step 1.
- Choose fileIDs that collide with nothing in the file; wire GameObject ↔ components ↔
  Transform consistently; append the Transform's fileID to the `SceneRoots` `m_Roots`
  list (this scene has one).
- LF line endings, exactly matching Unity's YAML style — never touch anything else in the
  scene file.

## 11. Out of scope (do NOT do here)
- The `TempleRunConfigApplied` / `TempleRunDifficulty*` retirement (Phase B).
- Any Blackboard, turn-system, or track/player coupling change (COUPLING_AUDIT).
- Docs/skills registration (handled by a separate task; this spec is code + scene only).
- Committing: leave everything in the working tree.

## Acceptance
1. `dotnet build` clean (with the csproj fixed per step 9).
2. Greps clean: no `CountdownEvents` reference outside `Assets/Countdown/` +
   `Assets/GameFlow/Scripts/CountdownSpecific/`; no `TempleRunEvents.Countdown*`,
   `TempleRunScenesReady`, `TempleRunLevelApplied` anywhere; no `GameFlowEvents` reference
   in `Assets/Countdown/` except `CountdownGameFlowBridge` is GameFlow-side so Countdown
   folder has none.
3. Scene diff of `Game_Boot_2_Play.unity` shows exactly one new object + components +
   SceneRoots entry.
4. Report the full play-test checklist for the owner (countdown shows, ticks, player
   frozen until GO, HUD/overlay timing, pause during countdown, second run after death).
