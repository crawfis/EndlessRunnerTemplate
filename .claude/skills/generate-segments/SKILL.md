---
name: generate-segments
description: Generate track segment ScriptableObject assets (TrackSegmentSO) and register them in the TrackSegmentRegistrySO. Prompt-driven creation of segments by direction, length range, difficulty range, and tags.
allowed-tools: Bash, Read, Write, Edit, Grep, Glob
argument-hint: <description> (e.g., "20 easy left-turns, lengths 8-20")
---

# Generate Segments

Generate new track segment definitions as **ScriptableObject assets** and add them to the shared
`TrackSegmentRegistrySO`. Track data is authored as SOs, not JSON — one `TrackSegmentSO` asset per
segment, gathered into the registry (see [docs/TRACKS.md](../../../docs/TRACKS.md#the-data-model)).

Assets live in:
- Segments: `Assets/TempleRun/Scriptables/Track/Segments/<Id>.asset`
- Registry: `Assets/TempleRun/Scriptables/Track/TrackSegmentRegistry.asset`

**Unity creates these assets, not you.** This skill drives the running Editor through the Unity
CLI, so Unity mints the GUIDs, writes the YAML, and keeps the field layout correct. Never
hand-write a `.asset` or `.meta` for a segment — that is how GUID collisions and silent field
drift got introduced before.

## Prerequisites

A running Editor with `com.unity.pipeline` installed:

```bash
unity pipeline list
```

`Server Reachable` must be `true` for this project. If it isn't, see the prerequisites section of
[verify-unity](../verify-unity/SKILL.md) and stop — do not fall back to hand-written YAML.

## Arguments

- `$ARGUMENTS` - Natural language description of segments to generate.
  - Examples: "20 easy left-turn segments, lengths 8-20"
  - "add 10 hard short segments for both directions"
  - "200 segments across all difficulties"

## Procedure

### Step 1: Read the current pool

List the existing segments so you can match the id naming pattern and avoid collisions:

```bash
unity cmd find_assets --type TrackSegmentSO --limit 500 --no-banner --result-only
```

Read one to learn the current field layout and the tags in use, and read the registry you will
append to:

```bash
unity cmd get_serialized_fields --target "Assets/TempleRun/Scriptables/Track/Segments/<some>.asset" --no-banner --result-only
unity cmd get_serialized_fields --target "Assets/TempleRun/Scriptables/Track/TrackSegmentRegistry.asset" --no-banner --result-only
```

Note the registry's `Segments` `arrayLength` — new entries append **after** it.

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

**Direction enum value** (pass the int; Unity resolves it to the enum): `Left = 0`, `Right = 1`,
`Straight = 2`, `Either = 3`.

### Step 4: Distribute & tag

- **ID format**: `{direction}_{length}` (e.g. `left_18`), `{direction}_{length}_v{N}` for variants.
- **Length distribution**: small counts (≤10) evenly spaced; large counts clustered with variation.
- **Difficulty correlation** (default; respect an explicit request): shorter = harder.
  - 24–28 → 0–2 (beginner) · 18–24 → 2–4 (easy) · 12–18 → 4–6 (medium) · 8–12 → 6–8 (hard) · 4–8 → 8–10 (expert)
- **Tags**: 0–2 beginner · 2–4 easy · 4–6 medium · 6–8 hard · 8–10 expert (or the user's tags).
- **MaxRepeat**: beginner/easy 2 · medium 3 · hard/expert 4.

### Step 5: Create each asset

For each segment, create it and set its fields. The path passed to `create_asset` is relative to
the authoring root (`Assets`); every other command takes the full `Assets/...` path.

```bash
unity cmd create_asset \
  --path "TempleRun/Scriptables/Track/Segments/left_18.asset" \
  --type "CrawfisSoftware.TempleRun.TrackSegmentSO" \
  --confirm true --no-banner --result-only
```

`create_asset` returns the GUID Unity minted — you never invent one:

```json
{ "assetPath": "Assets/.../left_18.asset", "guid": "6c7b2951...", "fileId": 11400000 }
```

Then set the fields, using the full asset path as `--target`:

```bash
SEG="Assets/TempleRun/Scriptables/Track/Segments/left_18.asset"

unity cmd set_serialized_field --target "$SEG" --field Id               --value "left_18" --no-banner --result-only
unity cmd set_serialized_field --target "$SEG" --field Direction        --value 0         --no-banner --result-only
unity cmd set_serialized_field --target "$SEG" --field ToPivotDistance  --value 17        --no-banner --result-only
unity cmd set_serialized_field --target "$SEG" --field ExitDistance     --value 1         --no-banner --result-only
unity cmd set_serialized_field --target "$SEG" --field DifficultyRating --value 3         --no-banner --result-only
unity cmd set_serialized_field --target "$SEG" --field MaxRepeat        --value 2         --no-banner --result-only
unity cmd set_serialized_field --target "$SEG" --field Weight           --value 1         --no-banner --result-only
```

**`Tags` is a `List<string>`** — size it, then set each element:

```bash
unity cmd set_serialized_field --target "$SEG" --field "Tags.Array.size"    --value 1      --no-banner --result-only
unity cmd set_serialized_field --target "$SEG" --field "Tags.Array.data[0]" --value "easy" --no-banner --result-only
```

Leave `TeleportDistance`, `TurnFailureDistance` and `TurnRadius` alone (they default to 0, which is
what `Normalize` expects). `Role` defaults to `Normal` and `SpeedMultiplier` to 1.

### Step 6: Register the segments

Grow the registry's `Segments` array once, then assign each new segment by path. With `N` existing
entries and `M` new ones, resize to `N + M`:

```bash
REG="Assets/TempleRun/Scriptables/Track/TrackSegmentRegistry.asset"

unity cmd set_serialized_field --target "$REG" --field "Segments.Array.size" --value 42 --no-banner --result-only
```

Then, for each new segment at its index (starting at `N`), assign an object reference by path:

```bash
unity cmd set_serialized_field --target "$REG" \
  --field "Segments.Array.data[41]" \
  --value '{"path":"Assets/TempleRun/Scriptables/Track/Segments/left_18.asset"}' \
  --no-banner --result-only
```

Resizing only appends empty slots; existing entries keep their index and are not disturbed.

### Step 7: Save to disk — REQUIRED

**`set_serialized_field` only mutates the in-memory object.** Until assets are saved, the `.asset`
files on disk still hold their old values and git sees nothing. Flush once, at the end:

```bash
unity cmd eval --code 'UnityEditor.AssetDatabase.SaveAssets(); return "saved";' --timeout 60 --no-banner --result-only
```

The `--timeout 60` is not optional — main-thread work times out at 5 s by default and returns
`Main thread operation timed out after 5000ms`.

### Step 8: Verify

Never report success from the command envelopes alone — they return a result object even when a
value didn't land where you expected. Read back, and check the disk:

```bash
unity cmd get_serialized_fields --target "$SEG" --no-banner --result-only
unity cmd get_serialized_fields --target "$REG" --no-banner --result-only
git status --porcelain Assets/TempleRun/Scriptables/Track/
```

Confirm that:
- `Direction` resolved to the enum name you intended (`0` → `"Left"`)
- `Tags` holds the tags you set
- the registry's `arrayLength` grew by exactly `M`, with no `null` entries
- git shows the new `.asset` + `.meta` files

Then check the console:

```bash
unity cmd console --tail 20 --level error --no-banner --result-only
```

### Step 9: Summarize

```
Generated N segments:
  [id]  Dir=[L/R/S/E]  Length=[len]  d=[difficulty]  Tags=[tags]
  ...

Created in:    Assets/TempleRun/Scriptables/Track/Segments/   (Unity-minted GUIDs)
Registered in: Assets/TempleRun/Scriptables/Track/TrackSegmentRegistry.asset  (M total)
Saved:         AssetDatabase.SaveAssets()
Console:       0 errors
```

Then remind the user:
- **RUGS parity:** copy the new `.asset` + `.meta` files and the updated `TrackSegmentRegistry.asset`
  into `../RunnerUGSTemplate` — do **not** re-run this skill there (that would mint *different*
  GUIDs for the same logical segments). See the port-to-RUGS project note.
- To include the segments in a level, add their tag/id to the relevant `TrackLevelSO` asset. A
  segment is only in a level's pool if a `TrackLevelSO` selects it — by a matching tag in
  `ActiveSegmentTags`, or by its id in `ActiveSegmentIds`.

## Notes

- **Bulk runs.** For large counts, `unity cmd batch` can group operations, but asset creation
  mutates outside the Undo system, so it requires `transactional=false`. A straightforward loop of
  individual calls is simpler and easier to recover from — prefer it unless the count is large
  enough to matter.
- **Deleting a bad batch.** `unity cmd delete_asset --asset <path> --confirm true` removes an asset
  and its `.meta` cleanly, keeping the AssetDatabase consistent. Do this rather than deleting files
  by hand. Remember to shrink or reassign the registry's `Segments` array afterwards so it has no
  `null` entries.

## Examples

### "generate 6 easy left turns, lengths 10-24"

6 left-turn segments (`Direction: 0`), lengths evenly 10→24 (`ToPivotDistance = length - 1`,
`ExitDistance = 1`), `DifficultyRating` 2–4, tagged `easy`.

### "add 10 hard segments for both directions, lengths 6-14"

5 left + 5 right, short lengths, `DifficultyRating` 6–8, tagged `hard`, `MaxRepeat` 4.

### "10 straight segments, lengths 8-16"

10 straights (`Direction: 2`, `ExitDistance: 0`, `ToPivotDistance = length`), tagged `straight`.
