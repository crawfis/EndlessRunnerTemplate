---
name: generate-segments
description: Generate track segment ScriptableObject assets (TrackSegmentSO) and register them in the TrackSegmentRegistrySO. Prompt-driven creation of segments by direction, length range, difficulty range, and tags.
allowed-tools: Read, Write, Edit, Grep, Glob
argument-hint: <description> (e.g., "20 easy left-turns, lengths 8-20")
---

# Generate Segments

Generate new track segment definitions as **ScriptableObject assets** and add them to the shared
`TrackSegmentRegistrySO`. Track data is authored as SOs, not JSON — one `TrackSegmentSO` asset per
segment, gathered into the registry (see [docs/TRACKS.md](../../../docs/TRACKS.md#the-data-model)).

Assets live in:
- Segments: `Assets/TempleRun/Scriptables/Track/Segments/<Id>.asset` (+ `.meta`)
- Registry: `Assets/TempleRun/Scriptables/Track/TrackSegmentRegistry.asset`

> These assets are created by the one-shot `CrawfisSoftware > Track > Import JSON -> ScriptableObjects`
> importer. If they don't exist yet, run that importer first (or say so and stop).

## Arguments

- `$ARGUMENTS` - Natural language description of segments to generate.
  - Examples: "20 easy left-turn segments, lengths 8-20"
  - "add 10 hard short segments for both directions"
  - "200 segments across all difficulties"

## Procedure

### Step 1: Read the current pool

- `Glob` `Assets/TempleRun/Scriptables/Track/Segments/*.asset` and read a few to learn the field
  layout, `Id` naming pattern (`left_28`, `right_12`, ...), tags in use, and existing ids (to avoid
  collisions).
- Read `Assets/TempleRun/Scriptables/Track/TrackSegmentRegistry.asset` — you will append the new
  segments to its `Segments` list.
- Read `Assets/TempleRun/Scripts/Track/TrackSegmentSO.cs.meta` and confirm the script guid is
  `8e4fd9825d0d99bd8f035cdcc48f65f0` (used in every generated `.asset`). If it differs, use the
  value from the `.meta`.

### Step 2: Parse the user request

| Parameter | Description | Default |
|-----------|-------------|---------|
| **Count** | How many segments to generate | Required |
| **Direction** | Left, Right, Both (= Left + Right), Straight, or Either | Both |
| **Length range** | Min/max total length in Unity units | 8–28 |
| **Difficulty range** | Min/max on 0–10 scale | 0–10 |
| **Tags** | Tags to apply (beginner, easy, medium, hard, expert, or custom) | Infer from difficulty |
| **Weight** | Selection weight | 1.0 |
| **MaxRepeat** | Max consecutive repeats | 2 |
| **Exit distance** | Post-pivot run-out for turns (see geometry below) | 1.0 |

If anything is ambiguous, ask the user to clarify.

### Step 3: Compute geometry (the 3-point model)

Every segment is Entrance → Pivot → Exit, and `Length = ToPivotDistance + ExitDistance`. The SO has
**no `Length` field** — author `ToPivotDistance` and `ExitDistance` so they sum to the intended
length. `Normalize` fills the rest (`TurnFailureDistance`, `TeleportDistance`) at load.

- **Straight** (`Direction: Straight`): `ExitDistance = 0`, `ToPivotDistance = length`.
- **Turn** (`Left` / `Right` / `Either`): `ExitDistance = <exit>` (default 1), `ToPivotDistance =
  length - <exit>`. Keep `ToPivotDistance > 0`.

Do **not** set `TurnFailureDistance` or `TeleportDistance` — leave them 0 so `Normalize` derives them.

**Direction enum value** (written as an int in YAML): `Left = 0`, `Right = 1`, `Straight = 2`,
`Either = 3`.

### Step 4: Distribute & tag

- **ID format**: `{direction}_{length}` (e.g. `left_18`), `{direction}_{length}_v{N}` for variants.
- **Length distribution**: small counts (≤10) evenly spaced; large counts clustered with variation.
- **Difficulty correlation** (default; respect an explicit request): shorter = harder.
  - 24–28 → 0–2 (beginner) · 18–24 → 2–4 (easy) · 12–18 → 4–6 (medium) · 8–12 → 6–8 (hard) · 4–8 → 8–10 (expert)
- **Tags**: 0–2 beginner · 2–4 easy · 4–6 medium · 6–8 hard · 8–10 expert (or the user's tags).
- **MaxRepeat**: beginner/easy 2 · medium 3 · hard/expert 4.

### Step 5: Write one asset per segment

For each segment, `Write` `Assets/TempleRun/Scriptables/Track/Segments/<Id>.asset`:

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 8e4fd9825d0d99bd8f035cdcc48f65f0, type: 3}
  m_Name: left_18
  m_EditorClassIdentifier:
  Id: left_18
  Direction: 0
  Weight: 1
  MaxRepeat: 2
  DifficultyRating: 3
  Tags:
  - easy
  ToPivotDistance: 17
  ExitDistance: 1
  TeleportDistance: 0
  TurnFailureDistance: 0
  TurnRadius: 0
  Role: Normal
  SpeedMultiplier: 1
  BlockedLanes: []
  LaneHeights: []
  ActiveLanes: []
  SpawnMode: Procedural
  SpawnSlots: []
  VisualTheme:
  SpawnSeed: 0
```

And `Write` its `.meta` at `<Id>.asset.meta` with a **unique** 32-hex guid (generate a distinct
random-looking hex string per asset; `Grep` the guid across `Assets/` to confirm it's unused):

```yaml
fileFormatVersion: 2
guid: <32-hex-unique>
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData:
  assetBundleName:
  assetBundleVariant:
```

### Step 6: Register the segments

`Edit` `TrackSegmentRegistry.asset` to append one reference per new segment to the `Segments`
sequence, using each asset's `.meta` guid:

```yaml
  Segments:
  - {fileID: 11400000, guid: <existing-segment-guid>, type: 2}
  - {fileID: 11400000, guid: <new-segment-guid>, type: 2}
```

A segment is only in a level's pool if a `TrackLevelSO` selects it — by a matching tag in
`ActiveSegmentTags`, or by its id in `ActiveSegmentIds`. Make sure the tags you assigned line up
with the levels that should use these segments.

### Step 7: Summarize

```
Generated N segments:
  [id]  Dir=[L/R/S/E]  Length=[len]  d=[difficulty]  Tags=[tags]
  ...

Wrote assets to: Assets/TempleRun/Scriptables/Track/Segments/
Registered in:    Assets/TempleRun/Scriptables/Track/TrackSegmentRegistry.asset  (M total)
```

Then remind the user:
- **Unity** will import the new assets on next focus; check the Console for errors.
- **RUGS parity:** copy the new `.asset` + `.meta` files and the edited `TrackSegmentRegistry.asset`
  into `../RunnerUGSTemplate` — do **not** re-generate there (that would mint different guids). See
  the port-to-RUGS project note.
- To include the segments in a level, add their tag/id to the relevant `TrackLevelSO` asset.

## Examples

### "generate 6 easy left turns, lengths 10-24"

6 left-turn segments (`Direction: 0`), lengths evenly 10→24 (`ToPivotDistance = length - 1`,
`ExitDistance = 1`), `DifficultyRating` 2–4, tagged `easy`.

### "add 10 hard segments for both directions, lengths 6-14"

5 left + 5 right, short lengths, `DifficultyRating` 6–8, tagged `hard`, `MaxRepeat` 4.

### "10 straight segments, lengths 8-16"

10 straights (`Direction: 2`, `ExitDistance: 0`, `ToPivotDistance = length`), tagged `straight`.
