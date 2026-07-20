---
name: audit-events
description: Audit the codebase for event system anti-patterns. Scans for missing OnDestroy unsubscriptions, direct coupling instead of events, unused events, potential circular auto-chains, and other violations of the event-driven architecture. Run this after adding new features.
allowed-tools: Read, Grep, Glob
argument-hint: [scope]
---

# Audit Events

Scan the codebase for violations of the event-driven architecture. This skill checks for anti-patterns and reports issues.

## Arguments

- `$ARGUMENTS` - Optional scope: `all` (default), `subscriptions`, `coupling`, `unused`, `circular`, or a specific file/folder path

## Audit Checks

### Check 1: Missing OnDestroy Unsubscriptions

Search for classes that call `SubscribeToEvent` or `SubscribeToAllEnumEvents` but do NOT have a corresponding `UnsubscribeToEvent` or `UnsubscribeToAllEnumEvents` in `OnDestroy()`.

**Pattern to find:**
```
Grep for SubscribeToEvent in *.cs files
For each file found, verify it also contains UnsubscribeToEvent in an OnDestroy method
```

**Report format:**
```
MISSING UNSUBSCRIPTION:
  [File:Line] subscribes to [EventName] but never unsubscribes
  Fix: Add UnsubscribeToEvent in OnDestroy()
```

### Check 2: Direct Coupling (bypassing events)

Search for direct method calls or references that should go through the event system:
- `GetComponent<>()` calls that reach across scene boundaries
- `FindObjectOfType<>()` for cross-system communication
- Direct singleton references like `GameController.Instance.StartGame()` (should be an event)
- `SendMessage()` or `BroadcastMessage()` calls

**Exclude from this check:**
- `EventsPublisher*.Instance` references (these ARE the event system)
- `Blackboard.Instance` (legitimate shared state)
- References within the same class or same scene

**Report format:**
```
DIRECT COUPLING:
  [File:Line] directly calls [TargetClass.Method] instead of publishing an event
  Suggestion: Publish [DomainEvents.SuggestedEvent] instead
```

### Check 3: Unused Events

For each event in all three enums, search if it is:
- Published anywhere (`PublishEvent([EnumName].[EventName]`)
- Subscribed to anywhere (`SubscribeToEvent([EnumName].[EventName]`)
- Referenced in an auto-chain or bridge mapping

**Report format:**
```
UNUSED EVENT:
  [EnumName].[EventName] = [value]
  - Published: [yes/no, file locations]
  - Subscribed: [yes/no, file locations]
  - Auto-chained: [yes/no, source/target]
  - Bridge mapped: [yes/no, source/target]
```

### Check 4: Circular Auto-Chain Detection

Trace all auto-chain and bridge mappings to detect cycles:
1. Build a directed graph of all mappings from:
   - `GameFlowAutoEventFlow.cs`
   - `TempleRunAutoEventFlow.cs`
   - `TempleRunGameFlowBridge.cs`
2. Run cycle detection on the graph
3. Report any cycles found

**Report format:**
```
CIRCULAR CHAIN DETECTED:
  [Event1] -> [Event2] -> [Event3] -> [Event1]
  Files involved: [list of auto-flow/bridge files]
```

### Check 5: Subscription/Publish Mismatch

Check for events that are published but never subscribed to (dead events) or subscribed to but never published (waiting forever).

**Report format:**
```
NEVER SUBSCRIBED (published but no listener):
  [EnumName].[EventName] published in [File] but no subscribers found

NEVER PUBLISHED (subscribed but never fires):
  [EnumName].[EventName] subscribed in [File] but never published
```

### Check 6: Domain Isolation Violations (Cross-Domain Event References)

Each domain's code may ONLY reference events from its own domain. Cross-domain event references are ONLY permitted inside the bridge file (`TempleRunGameFlowBridge.cs`).

**Scan for these violations:**

1. **TempleRun code referencing GameFlowEvents:**
   - Grep for `GameFlowEvents\.` in `Assets/TempleRun/**/*.cs`
   - Any match is a violation (TempleRun should only use `TempleRunEvents` and `UserInitiatedEvents`)

2. **GameFlow code referencing TempleRunEvents (outside bridges):**
   - Grep for `TempleRunEvents\.` in `Assets/GameFlow/**/*.cs`
   - Exclude `TempleRunGameFlowBridge.cs` — that file is allowed
   - Any other match is a violation

**Report format:**
```
DOMAIN ISOLATION VIOLATION:
  [File:Line] references [ForeignDomain]Events from [CurrentDomain] code
  Fix: Add bridge mapping in [BridgeFile] and subscribe to a local domain event instead
```

## Output Summary

At the end, provide a summary:
```
Event System Audit Results:
  Missing unsubscriptions: [count]
  Direct coupling violations: [count]
  Unused events: [count]
  Circular chains: [count]
  Publish/subscribe mismatches: [count]
  Domain isolation violations: [count]

  Total issues: [count]
  Severity: [CLEAN / WARNINGS / CRITICAL]
```

## Files to Scan

- Event enums: `Assets/GameFlow/Scripts/Events/GameFlowEvents.cs`, `Assets/TempleRun/Scripts/Events/TempleRunEvents.cs`, `Assets/TempleRun/Scripts/Events/UserInitiatedEvents.cs`
- Auto-flows: `Assets/GameFlow/Scripts/Events/GameFlowAutoEventFlow.cs`, `Assets/TempleRun/Scripts/Events/TempleRunAutoEventFlow.cs`
- Bridge: `Assets/GameFlow/Scripts/TempleRunSpecific/TempleRunGameFlowBridge.cs`
- All C# scripts: `Assets/**/*.cs`
