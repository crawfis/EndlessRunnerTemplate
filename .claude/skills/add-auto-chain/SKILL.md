---
name: add-auto-chain
description: Add an auto-event chain entry so one event automatically triggers another within the same domain. Use when a Requested event should automatically fire a Starting event, or similar same-domain progressions.
allowed-tools: Read, Edit, Grep, Glob
argument-hint: <SourceEvent> -> <TargetEvent>
---

# Add Auto-Chain

Add an auto-event chain mapping within a single event domain. Auto-chains fire automatically when the source event is published.

## Arguments

- `$ARGUMENTS` - Source event -> Target event (e.g., `GameFlowEvents.MyFeatureRequested -> GameFlowEvents.MyFeatureStarting`)

## Auto-Flow Files

| Domain | File |
|--------|------|
| **GameFlow** | `Assets/GameFlow/Scripts/Events/GameFlowAutoEventFlow.cs` |
| **TempleRun** | `Assets/TempleRun/Scripts/Events/TempleRunAutoEventFlow.cs` |

Note: `UserInitiatedEvents` does NOT have an auto-flow — input events are always handled by subscribers directly.

## CRITICAL: Always use dictionaries

**NEVER add individual `SubscribeToEvent` / `UnsubscribeToEvent` calls in auto-flow or bridge classes.** All event mappings MUST go into the appropriate dictionary. The `SubscribeToAllEnumEvents` handler will pick them up automatically. Individual subscriptions break the declarative pattern and create maintenance burden.

## Procedure

### Step 1: Verify both events are in the same domain

Auto-chains only work within a single domain. If the source and target are in different domains, tell the user to use `/add-bridge-mapping` instead.

### Step 2: Verify events exist

Read the enum file and confirm both events exist. If not, tell the user to run `/add-event` first.

### Step 3: Read the auto-flow file

Read the appropriate `*AutoEventFlow.cs` to understand:
- The dictionary variable name (e.g., `_autoGameFlow2GameFlowEvents`)
- Existing mappings and their comment structure
- Where the new mapping logically belongs

### Step 4: Check for circular chains

**CRITICAL**: Trace the full chain to ensure no infinite loops:
- If A -> B is being added, check: does B -> ... -> A exist anywhere?
- Check both auto-chains AND bridge mappings that could create a loop
- If a cycle is detected, STOP and warn the user

### Step 5: Add the mapping

Add the new entry to the dictionary. Place it:
- Near related mappings (same feature group)
- With a comment explaining the chain purpose
- Following the existing comment block style

### Step 6: Document what is NOT auto-chained

If the feature has events that are intentionally NOT auto-chained (e.g., events published by specific controllers after async work), add a comment explaining this:
```csharp
// MyFeatureStarting -> MyFeatureStarted: Published by MyFeatureController (after async work)
```

### Step 7: Summarize

```
Added auto-chain:
  [Event] -> [Event]  (in [AutoFlowClass])

Full chain from this feature:
  [FeatureRequested] -> [FeatureStarting] (auto)
  [FeatureStarting] -> [FeatureStarted] (published by controller)

Not auto-chained (intentional):
  [FeatureStarting] -> [FeatureStarted]: requires async completion
```

## Common Patterns

**Immediate progression** (no async work):
```csharp
{ GameFlowEvents.PauseRequested, GameFlowEvents.Pausing },
{ GameFlowEvents.Pausing, GameFlowEvents.Paused },
```

**Async progression** (auto-chain the request, controller publishes completion):
```csharp
{ GameFlowEvents.GameScenesLoadRequested, GameFlowEvents.GameScenesLoading },
// GameScenesLoading -> GameScenesLoaded: Published by scene loader after async load
```

**Orchestration** (cross-phase):
```csharp
{ GameFlowEvents.GameplayReady, GameFlowEvents.MainMenuShowRequested },
{ GameFlowEvents.GameScenesLoaded, GameFlowEvents.GameStartRequested },
```
