---
name: list-events
description: List all events in the endless runner event system, grouped by domain and category. Shows enum values, auto-chain mappings, bridge mappings, and subscriber/publisher locations. Use to understand the current event landscape before adding features.
allowed-tools: Read, Grep, Glob
argument-hint: [domain|all]
---

# List Events

Display a comprehensive view of all events in the event system.

## Arguments

- `$ARGUMENTS` - Optional domain filter: `GameFlow`, `TempleRun`, `UserInitiated`, or `all` (default)

## Procedure

### Step 1: Read the requested enum file(s)

| Domain | File |
|--------|------|
| GameFlow | `Assets/GameFlow/Scripts/Events/GameFlowEvents.cs` |
| TempleRun | `Assets/TempleRun/Scripts/Events/TempleRunEvents.cs` |
| UserInitiated | `Assets/TempleRun/Scripts/Events/UserInitiatedEvents.cs` |

> The authoritative domain list is every enum marked `[EventEnum]` under `Assets/`. If a
> grep for `\[EventEnum\]` turns up enums beyond these three (a domain added via
> `/add-event-domain`), include them in the listing and update this table.

### Step 2: Read auto-chain mappings

Read the relevant auto-flow file(s) and extract every `(From, To)` entry from the
`ChainTable` arrays. One source may map to several targets — list each pair.

| Domain | File |
|--------|------|
| GameFlow | `Assets/GameFlow/Scripts/Events/GameFlowAutoEventFlow.cs` |
| TempleRun | `Assets/TempleRun/Scripts/Events/TempleRunAutoEventFlow.cs` |

### Step 3: Read bridge mappings

Read bridge files and extract cross-domain mappings:
- `Assets/GameFlow/Scripts/TempleRunSpecific/TempleRunGameFlowBridge.cs`
- `Assets/TempleRun/Scripts/Events/Input2TempleRunAutoEventBridge.cs`

### Step 4: Format output

For each domain, output a table grouped by category:

```
## [Domain] Events ([count] events)
Bus: EventsFor<[Domain]Events>

### [Category Name]
| Event | Value | Auto-Chain | Bridge | Notes |
|-------|-------|------------|--------|-------|
| FeatureRequested | 130 | -> FeatureStarting | | |
| FeatureStarting | 131 | | | Published by controller |
| FeatureStarted | 132 | | -> OtherDomain.X | |
| FeatureFailed | 133 | | | |
```

**Auto-Chain column**: Show `-> TargetEvent` if this event auto-triggers another.
**Bridge column**: Show `-> Domain.Event` if this event bridges to another domain.
**Notes**: Show `(target of auto-chain from X)` or `(target of bridge from Domain.X)` for events that are targets.

When regenerating `docs/EVENTS.md` from this output, preserve and refresh the Domain
Registry table at the top of that file (mirrored from CLAUDE.md's Architecture Overview)
before the per-domain sections.

### Step 5: Show available value ranges

At the end, show the next available value ranges for adding new events:

```
## Available Value Ranges

| Domain | Last Used | Next Available Range |
|--------|-----------|---------------------|
| GameFlow | 138 (LevelProgressSaved) | 140+ |
| TempleRun | 350 (SegmentGeometryReady) | 360+ |
| UserInitiated | 8 (UserDashRequested) | 9+ |
```

(The counts above are a snapshot — always recompute from the enum files.)

### Step 6: Show flow summary (if `all`)

When listing all domains, include the cross-domain flow:

```
## Cross-Domain Event Flow

UserInput -> TempleRun (via Input2TempleRunAutoEventBridge — the ONLY permitted
subscriber to UserInitiatedEvents; controllers subscribe to the TempleRun event):
  [list all UserInitiated -> TempleRun mappings]

TempleRun -> GameFlow (via TempleRunGameFlowBridge):
  [list all TempleRun -> GameFlow mappings]

GameFlow -> TempleRun (via TempleRunGameFlowBridge):
  [list all GameFlow -> TempleRun mappings]
```
