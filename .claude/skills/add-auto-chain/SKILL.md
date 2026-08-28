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

Note: `UserInitiatedEvents` does NOT have a same-domain auto-flow — raw input crosses into
gameplay only via `Input2TempleRunAutoEventBridge`, the one permitted subscriber to raw input.

A domain added via `/add-event-domain` gets its own `<Domain>AutoEventFlow` following the
same chain-table pattern (a subclass of `AutoEventFlowBase<TSource, TDest>`) — add it to the
table above when it exists.

## CRITICAL: Always use the chain table

**NEVER add individual `Subscribe` / `Unsubscribe` calls in auto-flow or bridge classes.** All event mappings MUST go into the class's `ChainTable` array — a flat list of `(From, To)` pairs. The shared `EventChainDispatcher` subscribes to everything and dispatches automatically. Individual subscriptions break the declarative pattern and create maintenance burden.

One source event may declare **several** targets (multiple pairs with the same `From`).
Targets fire synchronously in declaration order, so keep a multi-target group together and
comment why the order matters.

## Procedure

### Step 1: Verify both events are in the same domain

Auto-chains only work within a single domain. If the source and target are in different domains, tell the user to use `/add-bridge-mapping` instead.

### Step 2: Verify events exist

Read the enum file and confirm both events exist. If not, tell the user to run `/add-event` first.

### Step 3: Read the auto-flow file

Read the appropriate `*AutoEventFlow.cs` to understand:
- The `ChainTable` array (returned through the `Chains` property)
- Existing entries and their comment structure
- Where the new entry logically belongs

### Step 4: Check for circular chains and validation gates

**CRITICAL**: Trace the full chain to ensure no infinite loops:
- If A -> B is being added, check: does B -> ... -> A exist anywhere?
- Check both auto-chains AND bridge mappings that could create a loop
- If a cycle is detected, STOP and warn the user

**Also check for a validation gate**: never chain a `*Requested` event that arrives raw from
input to its `*Starting` — the controller that validates (cooldown, airborne, lane boundary)
publishes `*Starting` itself, and an auto-chain would fire before the check runs. See the
comments atop `TempleRunAutoEventFlow.cs`.

### Step 5: Add the mapping

Add the new `(From, To)` entry to the `ChainTable` array. Place it:
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
(GameFlowEvents.PauseRequested, GameFlowEvents.Pausing),
(GameFlowEvents.Pausing, GameFlowEvents.Paused),
```

**Async progression** (auto-chain the request, controller publishes completion):
```csharp
(GameFlowEvents.GameScenesLoadRequested, GameFlowEvents.GameScenesLoading),
// GameScenesLoading -> GameScenesLoaded: Published by FireEventAfterSceneLoads
//             once every gameplay scene has loaded
```

**Orchestration** (cross-phase):
```csharp
(GameFlowEvents.GameplayReady, GameFlowEvents.MainMenuShowRequested),
(GameFlowEvents.GameScenesLoaded, GameFlowEvents.GameStartRequested),
```

**Fan-out** (one source, several consequences — fire in declaration order):
```csharp
(TempleRunEvents.PlayerFailingAtTurn, TempleRunEvents.PlayerFailing),
(TempleRunEvents.PlayerFailingAtObstacle, TempleRunEvents.PlayerFailing),
```
