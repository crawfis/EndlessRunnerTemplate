---
name: generate-segments
description: Generate track segment definitions and add them to the segment registry JSON. Prompt-driven creation of segments by direction, length range, difficulty range, and tags.
allowed-tools: Read, Write, Edit, Grep, Glob
argument-hint: <description> (e.g., "20 easy left-turns, lengths 8-20")
---

# Generate Segments

Generate new track segment definitions and write them to the segment registry JSON file.

## Arguments

- `$ARGUMENTS` - Natural language description of segments to generate.
  - Examples: "20 easy left-turn segments, lengths 8-20"
  - "add 10 hard short segments for both directions"
  - "200 segments across all difficulties"

## Procedure

### Step 1: Read the current registry

Read `Assets/TempleRun/Resources/TrackSegments_Registry.json` to understand:
- Existing segment IDs (avoid duplicates)
- Existing tags in use
- ID naming pattern (`left_28`, `right_12`, etc.)
- Highest DifficultyRating and Length values
- Total current segment count

### Step 2: Parse the user request

Extract from the arguments:

| Parameter | Description | Default |
|-----------|-------------|---------|
| **Count** | How many segments to generate | Required |
| **Direction** | Left, Right, Both, or All (= Left + Right) | All |
| **Length range** | Min/max length in Unity units | 8–28 |
| **Difficulty range** | Min/max on 0–10 scale | 0–10 |
| **Tags** | Tags to apply (beginner, easy, medium, hard, or custom) | Infer from difficulty |
| **Weight** | Selection weight | 1.0 |
| **MaxRepeat** | Max consecutive repeats | 2 |
| **SpawnMode** | Procedural, Preset, or Hybrid | Procedural |

If anything is ambiguous, ask the user to clarify.

### Step 3: Generate segment definitions

Create segments following these rules:

**ID format**: `{direction}_{length}` or `{direction}_{length}_{tag}` for uniqueness, or `{direction}_{length}_v{N}` for variants.
- Examples: `left_18`, `right_12_v2`, `both_20_easy`

**Length distribution**: Spread lengths across the requested range.
- Small counts (≤10): evenly space across range
- Large counts: cluster around common lengths with variation

**Difficulty correlation**: By default shorter = harder, but respect the user's requested range.
- Lengths 24–28 → difficulty 0–2 (easy)
- Lengths 18–24 → difficulty 2–4
- Lengths 12–18 → difficulty 4–6
- Lengths 8–12 → difficulty 6–8 (hard)
- Lengths 4–8  → difficulty 8–10 (expert)

**Tag assignment**: Apply requested tags. If spanning difficulty ranges, use:
- difficulty 0–2 → "beginner"
- difficulty 2–4 → "easy"
- difficulty 4–6 → "medium"
- difficulty 6–8 → "hard"
- difficulty 8–10 → "expert"

**MaxRepeat**: Scale with difficulty:
- beginner/easy: MaxRepeat = 2
- medium: MaxRepeat = 3
- hard/expert: MaxRepeat = 4

**Schema fields per segment**:

```json
{
  "Id": "left_18_easy",
  "Direction": "Left",
  "Length": 18.0,
  "Weight": 1.0,
  "MaxRepeat": 2,
  "DifficultyRating": 3.0,
  "Tags": ["easy"],
  "Role": "Normal",
  "SpeedMultiplier": 1.0,
  "SpawnMode": "Procedural",
  "SpawnSlots": [],
  "BlockedLanes": [],
  "LaneHeights": [],
  "ActiveLanes": [],
  "VisualTheme": "",
  "SpawnSeed": 0
}
```

Only include the new schema fields (Role, SpeedMultiplier, SpawnMode, SpawnSlots, BlockedLanes, LaneHeights, ActiveLanes, VisualTheme, SpawnSeed) when the user explicitly requests them or they are non-default. Otherwise keep the JSON compact with just the core fields (Id, Direction, Length, Weight, MaxRepeat, DifficultyRating, Tags).

### Step 4: Validate no ID collisions

Check every generated ID against existing registry IDs. On collision, append `_v2`, `_v3`, etc.

### Step 5: Add to registry

Edit `Assets/TempleRun/Resources/TrackSegments_Registry.json`:
- Add new segment objects to the `Segments` array
- Maintain existing JSON formatting style (one object per line for compact segments)
- Keep logical grouping (left turns, right turns, both, etc.)
- Place new segments AFTER existing segments of the same direction

### Step 6: Summarize

Output a summary table and next steps:

```
Generated N segments:
  [id]  Dir=[L/R/B]  Length=[len]  d=[difficulty]  Tags=[tags]
  ...

Added to: Assets/TempleRun/Resources/TrackSegments_Registry.json
Registry now has M total segments.

Tip: Open CrawfisSoftware > Track Level Editor to assign these
segments to levels by enabling their tags.
```

## Examples

### "generate 6 easy left turns, lengths 10-24"

Generates 6 left-turn segments with lengths evenly distributed from 10 to 24, DifficultyRating in the 2-4 range, tagged "easy".

### "add 10 hard segments for both directions, lengths 6-14"

Generates 5 left + 5 right segments with short lengths (6-14), DifficultyRating 6-8, tagged "hard", MaxRepeat 4.

### "200 segments across all difficulties"

Generates a balanced set across all tags and both directions:
- ~40 beginner (lengths 24-28, difficulty 0-2)
- ~40 easy (lengths 18-24, difficulty 2-4)
- ~40 medium (lengths 12-18, difficulty 4-6)
- ~40 hard (lengths 8-12, difficulty 6-8)
- ~40 expert (lengths 4-8, difficulty 8-10)

Each group split evenly between Left and Right directions.

### "20 preset segments with spawn slots for a challenge section"

Generates 20 segments with SpawnMode="Preset" and populated SpawnSlots arrays for hand-crafted obstacle/coin placement.
