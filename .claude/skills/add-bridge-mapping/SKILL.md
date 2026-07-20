---
name: add-bridge-mapping
description: Add a cross-domain event bridge mapping between two event domains (e.g., TempleRun to GameFlow). Use when a feature in one domain needs to trigger behavior in another domain.
allowed-tools: Read, Edit, Grep, Glob
argument-hint: <SourceEvent> -> <TargetEvent>
---

# Add Bridge Mapping

Add a cross-domain event mapping to one of the bridge classes. Bridges connect different event domains so they can communicate without direct coupling.

## Arguments

- `$ARGUMENTS` - Source event -> Target event (e.g., `TempleRunEvents.CoinCollected -> GameFlowEvents.ScoreUpdated`)

## Domain Isolation Rule

**Bridges are the ONLY place where cross-domain event references are allowed.** Domain code must never directly subscribe to or publish events from another domain. If TempleRun code needs to react to a GameFlow lifecycle event, the bridge must translate it into a TempleRun-domain event first.

After adding a bridge mapping, remind the user that any existing code that directly references the foreign-domain event should be refactored to subscribe to the new local-domain event instead.

## Available Bridges

| Bridge Class | File | Connects |
|-------------|------|----------|
| **TempleRunGameFlowBridge** | `Assets/GameFlow/Scripts/TempleRunSpecific/TempleRunGameFlowBridge.cs` | TempleRun <-> GameFlow (bidirectional) |

This is the only bridge in the template. If you add another integration domain
(analytics, backend services, etc.), create a new bridge class for it following
the same pattern rather than referencing that domain's events from gameplay code.

## CRITICAL: Always use dictionaries

**NEVER add individual `SubscribeToEvent` / `UnsubscribeToEvent` calls in bridge or auto-flow classes.** All event mappings MUST go into the appropriate dictionary. The `SubscribeToAllEnumEvents` handler will pick them up automatically. Individual subscriptions break the declarative pattern and create maintenance burden.

## Procedure

### Step 1: Identify source and target domains

From the user's request, determine:
- Which domain the source event belongs to
- Which domain the target event belongs to
- Which bridge class handles this direction

### Step 2: Verify events exist

Read both enum files to confirm the source and target events exist. If they don't, tell the user to run `/add-event` first.

### Step 3: Read the bridge file

Read the appropriate bridge class to understand:
- The existing dictionary mappings
- The direction dictionaries (e.g., `_autoTempleRun2GameFlowEvents` vs `_autoGameFlow2TempleRunEvents`)
- Comment style used

### Step 4: Add the mapping

Add the new entry to the correct direction dictionary. Include a comment explaining why this bridge exists.

**TempleRunGameFlowBridge has two dictionaries:**
- `_autoTempleRun2GameFlowEvents` — TempleRun fires, GameFlow receives
- `_autoGameFlow2TempleRunEvents` — GameFlow fires, TempleRun receives

### Step 5: Check for circular paths

Verify the new mapping doesn't create a circular event chain:
- A -> B (bridge) -> C (auto-chain) -> A would loop infinitely
- Trace the full path from source to any auto-chained targets

### Step 6: Summarize

```
Added bridge mapping:
  [SourceDomain].[SourceEvent] -> [TargetDomain].[TargetEvent]
  In: [BridgeClassName]
  Direction: [Source] -> [Target]

Event flow path:
  [trace the full chain including any auto-chains that will fire]
```

## Example

Adding `TempleRunEvents.CoinCollected -> GameFlowEvents.ScoreUpdateRequested`:

1. Verify both events exist in their enums
2. Open `TempleRunGameFlowBridge.cs`
3. Add to `_autoTempleRun2GameFlowEvents`:
   ```csharp
   // Coin collected in gameplay -> request score update in GameFlow
   { TempleRunEvents.CoinCollected, GameFlowEvents.ScoreUpdateRequested },
   ```
4. Trace: CoinCollected -> (bridge) -> ScoreUpdateRequested -> (auto-chain?) -> ...
