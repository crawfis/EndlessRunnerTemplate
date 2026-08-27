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

Search for classes that call `Subscribe` or `SubscribeToAll` but do NOT have a corresponding `Unsubscribe` or `UnsubscribeFromAll` in `OnDestroy()`. This includes typed subscriptions made through an `EventId<T>` field (`SomeEvent.Subscribe(...)`), which need the matching `SomeEvent.Unsubscribe(...)`.

**Pattern to find:**
```
Grep for `.Subscribe(` and `.SubscribeToAll(` in *.cs files
For each file found, verify it also contains the matching `.Unsubscribe(` / `.UnsubscribeFromAll(` in an OnDestroy method
```

**Report format:**
```
MISSING UNSUBSCRIPTION:
  [File:Line] subscribes to [EventName] but never unsubscribes
  Fix: Add the matching Unsubscribe in OnDestroy()
```

### Check 2: Direct Coupling (bypassing events)

Search for direct method calls or references that should go through the event system:
- `GetComponent<>()` calls that reach across scene boundaries
- `FindObjectOfType<>()` for cross-system communication
- Direct singleton references like `GameController.Instance.StartGame()` (should be an event)
- `SendMessage()` or `BroadcastMessage()` calls

**Exclude from this check:**
- `EventsFor<T>` bus references and their per-file aliases (`GameFlowBus`, `TempleRunBus`, `UserInputBus`) — these ARE the event system
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
- Published anywhere (`Publish([EnumName].[EventName]`, or `<Field>.Publish(` for a typed `EventId<T>`)
- Subscribed to anywhere (`Subscribe([EnumName].[EventName]`, or `<Field>.Subscribe(` for a typed `EventId<T>`)
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

3. **Additional domains** (added via `/add-event-domain`): run the same check for each —
   the domain's enum name may appear outside its own `Assets/<Domain>/` folder ONLY in
   bridge files. The authoritative domain list is every enum marked `[EventEnum]` under
   `Assets/`.

**Report format:**
```
DOMAIN ISOLATION VIOLATION:
  [File:Line] references [ForeignDomain]Events from [CurrentDomain] code
  Fix: Add bridge mapping in [BridgeFile] and subscribe to a local domain event instead
```

### Check 7: Domain Registry Drift

The curated domain list lives in two mirrored tables: `CLAUDE.md` (Architecture Overview,
"Domain Registry") and the top of `docs/EVENTS.md`.

1. Grep `\[EventEnum\]` across `Assets/**/*.cs`. Every hit must be in a file matching the
   placement convention `Assets/*/Scripts/Events/*Events.cs` — an event enum anywhere else
   is a violation.
2. Compare the enums found against both registry tables. Flag any enum missing from a
   table, any table row with no matching enum, and any disagreement between the two tables.

**Report format:**
```
REGISTRY DRIFT:
  [EnumName] at [File] is not listed in [CLAUDE.md | docs/EVENTS.md] registry
  (or) [EnumName] declared outside Assets/*/Scripts/Events/ at [File]
  (or) Registry row [Domain] has no matching [EventEnum] enum
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
  Registry drift: [count]

  Total issues: [count]
  Severity: [CLEAN / WARNINGS / CRITICAL]
```

## Files to Scan

- Event enums: `Assets/GameFlow/Scripts/Events/GameFlowEvents.cs`, `Assets/TempleRun/Scripts/Events/TempleRunEvents.cs`, `Assets/TempleRun/Scripts/Events/UserInitiatedEvents.cs`
- Auto-flows: `Assets/GameFlow/Scripts/Events/GameFlowAutoEventFlow.cs`, `Assets/TempleRun/Scripts/Events/TempleRunAutoEventFlow.cs`
- Bridge: `Assets/GameFlow/Scripts/TempleRunSpecific/TempleRunGameFlowBridge.cs`
- Any additional `[EventEnum]` enums, `*AutoEventFlow` classes, and `*Bridge` classes from domains added later
- All C# scripts: `Assets/**/*.cs`
