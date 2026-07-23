# Spec: migrate TempleRun track data from JSON to ScriptableObjects

> **Status:** proposed, not started.
> **How to use:** this doc is written to double as a session prompt — paste it whole into a fresh
> Claude Code session, or point one at the file. Read [CLAUDE.md](../../CLAUDE.md) first; the
> event-system rules are mandatory.

## Goal

Replace the JSON-authored track data with ScriptableObjects. The template should be simple and
idiomatic Unity; JSON was arguably a wrong turn. Every other config in the project is already
SO-based — track data is the odd one out.

## Why

Unity's `JsonUtility` binds by exact field name and silently drops mismatches, with no error. That
hazard has produced three real bugs. All are fixed, but all were structural rather than careless:

1. C# field `ToPivotDistance` vs JSON key `EntranceDistance` — every turn segment fell back to
   `ToPivotDistance = Length`, corrupting turn geometry.
2. `Direction` declared as an enum but authored as a string. `JsonUtility` serializes enums as
   integers and ignores a string aimed at an enum field, so every segment defaulted to value 0 —
   `Direction.Left`. The track only ever turned left, and straights were built as turns (emitting a
   degenerate zero-length exit sub-spline). Worked around with a `DirectionString` field plus an
   explicit parse.
3. The same failure mode is documented in [KNOWN_ISSUES.md](../KNOWN_ISSUES.md).

Native SO serialization removes all three: enums get an Inspector dropdown, renames are
compile-time safe, and key/field drift becomes impossible. `TrackSegmentDefinition.DirectionString`
and `TrackSegmentLibrary.ParseDirection` should disappear entirely.

Secondary wins: no `Resources/` build bloat, compile-time-safe refactors, and most of the 602-line
editor window replaced by free Inspector UI.

## Where JSON lives today

Verified — it is only this.

**Data** (6 files; the entire contents of `Assets/TempleRun/Resources/`):

| File | Contents |
|---|---|
| `TrackSegments_Registry.json` | 17 segment definitions |
| `TrackLevel_0{1..5}_*.json` | 5 level rulesets — tag filters only; `Segments`/`Connections` are empty |

**Load paths:**

| File | Use |
|---|---|
| `Scripts/Track/TrackSegmentLibrary.cs` | 4 × `JsonUtility.FromJson` |
| `Scripts/Config/Blackboard.cs:129-131` | `Resources.Load<TextAsset>` → `FromJson` |
| `Scripts/Track/TrackManager.cs:116` | `Resources.Load<TextAsset>` for the registry |
| `Editor/TrackLevelEditorWindow.cs` | 602 lines; reads/writes the `.json` directly |

**Do not touch:** `GameFlow/Scripts/Config/LevelProgressManager.cs:52-61`. It uses `JsonUtility` to
serialize a save blob into `PlayerPrefs` — not authoring data, no `.json` file, idiomatic. It stays.

## The seam that already exists

`LevelConfig` is already a ScriptableObject, and `LevelConfig.cs:38` holds `TrackLevelResourcePath`
— a **string path** into `Resources`. That stringly-typed hop is the only bridge between the SO
world and the JSON world. Replace it with a direct SO reference and `Assets/TempleRun/Resources/`
becomes empty and can be deleted. (The only other `Resources/` folder is UI Toolkit fonts — leave
it alone.)

Match the style of the existing SOs: `TempleRunGameConfig`, `Jump`/`Slide`/`Dash`/`Lane`/`CoinConfig`,
`PowerUpDefinition`, `SpawnPrefabRegistry`, `LevelConfig`, `LevelRegistry`. Instances live in
`Assets/TempleRun/Scriptables/`.

## Open design question — decide before writing code

Asset granularity:

- **One SO per segment** — 17 assets, individually referenceable, clean git diffs, easy to add one.
- **One registry SO** holding a `List<TrackSegmentDefinition>` — mirrors today's structure, single
  asset to manage.

This drives everything downstream. Decide deliberately rather than inheriting the JSON shape by
default. Propose the full SO shape — segments, registry, level rulesets, and how `LevelConfig`
references them — before implementing.

## Hard requirements

- **Preserve `TrackSegmentLibrary.Normalize(definition)`** as the single data boundary. It is the
  one place raw authored data becomes trustworthy, and downstream code assumes its invariants
  rather than re-checking them:

  | Invariant | |
  |---|---|
  | `Length` | `== ToPivotDistance + ExitDistance` |
  | `Straight` | `ExitDistance == 0`, `TurnFailureDistance == MaxValue` |
  | `Left`/`Right`/`Either` | `ExitDistance > 0`, `TurnFailureDistance < Length` |
  | `TeleportDistance` | `> 0` exactly when `ExitDistance > 0` |

  Every construction path must route through it — including `TrackManager`'s inline procedural
  fallback, which previously skipped it and ended up with `TurnFailureDistance == 0`, failing the
  turn on the segment's first frame. See [TRACKS.md](../TRACKS.md#normalization-rules-normalize-run-once-per-definition).

- **Write a one-shot importer** (editor menu item) converting the 6 JSON files into SO assets. Do
  not re-author 17 segments by hand. All 17 currently normalize to known `Length` and
  `TurnFailureDistance` values — the importer must reproduce them exactly. That comparison is the
  regression check.

- **No defensive guards.** Fix the architecture, fix the implementation, or disallow the invalid
  state. Do not add null/zero/epsilon checks or `try`/`catch`.

- **Do not modify the auto-event flow files** (`TempleRunAutoEventFlow.cs`,
  `GameFlowAutoEventFlow.cs`, `TempleRunGameFlowBridge.cs`) or any event enum. This migration
  should require zero event changes.

- **Port everything to `../RunnerUGSTemplate`.** `Assets/TempleRun` is currently **109/109 files
  byte-identical** between the two repos and must stay that way. RUGS files are CRLF — compare with
  `diff --strip-trailing-cr`. Script GUIDs are aligned across both repos: copy source without
  touching existing `.meta`, and copy `.meta` alongside genuinely new files.

## RUGS / RemoteConfig

RUGS needs JSON for UGS RemoteConfig, but that does not force JSON into the base template. Target
layering: SOs are the authored defaults in both projects, and RUGS applies a RemoteConfig JSON
patch on top at runtime. Don't build that overlay now — just don't design it out.

## Verification

```
dotnet build Assembly-CSharp.csproj -v q --nologo
dotnet build Assembly-CSharp-Editor.csproj -v q --nologo
```

Newly added `.cs` files are absent from Unity's generated `csproj` until the editor regenerates it,
which surfaces as spurious `CS0246` errors. Grep the `csproj` for the new paths before believing a
failure.

Then play-test from `Assets/GameFlow/Scenes/Boot/0_BootStrap_Game_Only`: the track should show mixed
left/right turns and genuine straights, and a missed turn should kill the player.
