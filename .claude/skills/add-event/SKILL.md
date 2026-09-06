---
name: add-event
description: Add new events to the endless runner event system. Use when adding any new feature, behavior, or action that needs to communicate across systems. Ensures events are added to the correct domain enum with proper naming conventions, numbering, and optional auto-chain/bridge wiring.
allowed-tools: Read, Edit, Grep, Glob
argument-hint: <feature-name> [domain]
---

# Add Event

Add new events to the endless runner event-driven architecture. Every action in this codebase MUST go through the event system.

## Arguments

- `$ARGUMENTS` - Feature name and optionally the domain (GameFlow, TempleRun, Countdown, UserInitiated)

## Procedure

### Step 1: Determine the correct domain

Ask the user if not obvious from context:

| Domain | Enum File | When to use |
|--------|-----------|-------------|
| **GameFlow** | `Assets/GameFlow/Scripts/Events/GameFlowEvents.cs` | App lifecycle: loading, menus, pause, quit, scenes, config |
| **TempleRun** | `Assets/TempleRun/Scripts/Events/TempleRunEvents.cs` | Gameplay: player actions, turns, track, collisions, coins, power-ups |
| **Countdown** | `Assets/Countdown/Scripts/Events/CountdownEvents.cs` | Session ceremony: the pre-run 3…2…1 (start, ticks, end) |
| **UserInitiated** | `Assets/TempleRun/Scripts/Events/UserInitiatedEvents.cs` | Raw input: new player-triggered actions |

> This table lists the domains that exist today. The authoritative list is every enum
> marked `[EventEnum]` under `Assets/` — grep for `\[EventEnum\]` if you suspect the table
> is stale, and update it when a domain is added. If the feature genuinely needs a domain
> that doesn't exist yet (rare), run `/add-event-domain` first — its decision gate tells
> you whether you really do.

### Step 2: Read the target enum file

Read the enum file to find:
- The highest existing enum value in the relevant category
- Existing category comment blocks (e.g., `// ---------- Category ----------`)
- The naming pattern used

### Step 3: Generate the event group

Follow the naming convention. Most features need a lifecycle group:

```
*Requested    - User or system initiated a request
*Starting/*ing - Action is beginning (async in progress)
*Started/*ed  - Action completed successfully
*Failed       - Action failed
*Cancelled    - Action was cancelled (optional)
```

Choose the appropriate subset. Not all features need all states. For example:
- Simple toggle: `FeatureRequested`, `FeatureActivated`, `FeatureDeactivated`
- Async operation: `FeatureRequested`, `FeatureStarting`, `FeatureStarted`, `FeatureFailed`
- UI panel: `FeatureShowRequested`, `FeatureShowing`, `FeatureShown`, `FeatureHideRequested`, `FeatureHiding`, `FeatureHidden`

### Step 4: Assign enum values

- Find the next available value range (gaps of 10 between categories)
- Values within a group are sequential
- Add a category comment header: `// ---------- Feature Name ----------`
- Exception — `UserInitiatedEvents`: implicit sequential values with a `User` prefix and no
  categories; just append at the end, with `[EventPayload(typeof(int))]  // Player id` on the
  member — every member carries the player id and nothing else

### Step 5: Edit the enum file

Add the new events to the enum. Place them in a logical location (near related features or at the end).

- If a member carries data, declare it on the member: `[EventPayload(typeof(T))]`. For `int`,
  `float`, `string`, `bool`, `long` or `double` add an inline comment saying what the value
  *is* (`[EventPayload(typeof(int))]  // Player id`). A member with no payload, or a genuinely
  variable one, stays undeclared. Do not mint `EventId<T>` fields at call sites.
- Add `[EventDelivery(EventDelivery.Sticky)]` only if the member is a *level* (a state that is
  self-describing on its own); a transition stays `Transient`. See CLAUDE.md "Typed Payloads"
  and "Delivery Policy".

### Step 6: Ask about auto-chaining and bridges

After adding events, ask:
- "Should any of these auto-chain? (e.g., *Requested -> *Starting)" — if yes, suggest running `/add-auto-chain`
- "Do any of these need to bridge to another domain?" — if yes, suggest running `/add-bridge-mapping`

### Step 7: Summarize

Output a summary:
```
Added to [Domain]Events:
  [FeatureName]Requested = [value]
  [FeatureName]Starting  = [value]
  [FeatureName]Started   = [value]
  [FeatureName]Failed    = [value]

Next steps:
  /add-auto-chain  — if these should auto-chain
  /add-bridge-mapping — if these cross domains
```

## Example

For "adding obstacle detection":
- Domain: TempleRun (gameplay mechanic)
- Events: `ObstacleDetected`, `ObstacleAvoidRequested`, `ObstacleAvoiding`, `ObstacleAvoided`, `ObstacleAvoidFailed`
- Values: next available in TempleRun Hazards/Collisions category (120+)
