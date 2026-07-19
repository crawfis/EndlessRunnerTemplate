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

### Step 2: Read auto-chain mappings

Read the relevant auto-flow file(s) and extract all dictionary entries.

| Domain | File |
|--------|------|
| GameFlow | `Assets/GameFlow/Scripts/Events/GameFlowAutoEventFlow.cs` |
| TempleRun | `Assets/TempleRun/Scripts/Events/TempleRunAutoEventFlow.cs` |

### Step 3: Read bridge mappings

Read bridge files and extract cross-domain mappings:
- `Assets/GameFlow/Scripts/TempleRunSpecific/TempleRunGameFlowBridge.cs`

### Step 4: Format output

For each domain, output a table grouped by category:

```
## [Domain] Events ([count] events)
Publisher: EventsPublisher[Domain].Instance

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

### Step 5: Show available value ranges

At the end, show the next available value ranges for adding new events:

```
## Available Value Ranges

| Domain | Last Used | Next Available Range |
|--------|-----------|---------------------|
| GameFlow | 123 (QuitCompleted) | 130+ |
| TempleRun | 285 (TeleportEnded) | 290+ |
| UserInitiated | 2 (PauseToggle) | 3+ |
```

### Step 6: Show flow summary (if `all`)

When listing all domains, include the cross-domain flow:

```
## Cross-Domain Event Flow

UserInput -> TempleRun:
  (no bridges - handled by direct subscription in controllers)

TempleRun -> GameFlow (via TempleRunGameFlowBridge):
  [list all TempleRun -> GameFlow mappings]

GameFlow -> TempleRun (via TempleRunGameFlowBridge):
  [list all GameFlow -> TempleRun mappings]
```
