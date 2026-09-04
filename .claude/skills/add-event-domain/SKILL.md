---
name: add-event-domain
description: Create a new event domain — a new [EventEnum] enum with its own EventsFor<T> bus, optional auto-flow, and a bridge — for a genuinely separate bounded context (analytics, backend/UGS, netcode). Rare; includes the decision gate, scene hosting, and the registration checklist for docs and skills.
allowed-tools: Read, Write, Edit, Grep, Glob
argument-hint: <DomainName> [purpose]
---

# Add Event Domain

Stand up a new event domain alongside `GameFlowEvents`, `TempleRunEvents`,
`UserInitiatedEvents`, and `CountdownEvents`. This is rare and structural — most features
are a new *category* inside an existing enum, not a new domain. The Countdown domain
(2026-09, [docs/specs/COUNTDOWN_DOMAIN.md](../../../docs/specs/COUNTDOWN_DOMAIN.md)) is
this skill's in-repo worked example: a session-ceremony bounded context whose bridges
translate rather than relay (`GameStarting → CountdownStartRequested` in,
`CountdownEnded → PlayerActivateRequested` out).

## Arguments

- `$ARGUMENTS` — the domain name (PascalCase, e.g. `Analytics`) and optionally its purpose.

## Step 0: Decision gate — do you actually need a domain?

A domain is a bounded context with its own enum, its own bus, and an isolation boundary
other code may cross only through a bridge. Create one ONLY if all three hold:

1. **Separate concern with its own lifecycle** — not app flow (GameFlow), not gameplay
   (TempleRun), not raw input (UserInitiated). Good candidates: backend services
   (auth, leaderboards, cloud save), analytics/telemetry, networking.
2. **The rest of the game must stay decoupled from it** — you want to add, remove, or swap
   it without touching gameplay or app-flow code. The bridge is what buys that. The
   operational test: could a trivial **stub** — same events consumed and published, same
   payloads and Sticky behavior, nothing real behind them — sit in its place and keep the
   game running? (`AIController` is the in-repo proof at the input seam: a deterministic
   autopilot publishing the same `UserInitiatedEvents` the human's input actions do —
   nothing downstream knows the difference.)
3. **It will grow a family of events** — several lifecycle groups, not one or two events.

If any of these fail → STOP and use `/add-event` instead: a new mechanic, panel, or system
is a category in an existing enum.

A strong **capture-point purpose** reinforces criterion 2: a stream worth logging and
replaying as a unit is itself a reason the rest of the game must stay decoupled from it.
The UserInitiated seam is the template's example — timestamp those events and keep the
run's random seed, and you have a complete, replayable playthrough (ghost runners, demos,
bug reproductions).

Precedent: the UGS sibling template's domain (`PlayerAuthenticated`, `ScoreUpdating`,
`LeaderboardOpening` — see the timeline comments in `GameFlowAutoEventFlow.cs`), which
crossed into GameFlow only through a `UGSGameFlowBridge`.

## Procedure

### Step 1: Choose names and placement

| Piece | Convention | Example (`Analytics`) |
|-------|------------|----------------------|
| Enum | `<Name>Events` | `AnalyticsEvents` |
| File | `Assets/<Name>/Scripts/Events/<Name>Events.cs` | `Assets/Analytics/Scripts/Events/AnalyticsEvents.cs` |
| Namespace | `CrawfisSoftware.<Name>.Events` (mirror GameFlow, the cleanest of the three) | `CrawfisSoftware.Analytics.Events` |
| Bus alias | `using <Name>Bus = CrawfisSoftware.Events.EventsFor<...>;` per consuming file | `using AnalyticsBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.Analytics.Events.AnalyticsEvents>;` |
| Auto-flow (optional) | `<Name>AutoEventFlow`, next to the enum | `AnalyticsAutoEventFlow` |
| Bridge | `<Name>GameFlowBridge` (or whichever pair applies), hosted under the more application-level domain, mirroring `TempleRunSpecific/` | `Assets/GameFlow/Scripts/AnalyticsSpecific/AnalyticsGameFlowBridge.cs` |

### Step 2: Create the enum

```csharp
using CrawfisSoftware.Events;

namespace CrawfisSoftware.Analytics.Events
{
    [EventEnum]
    public enum AnalyticsEvents
    {
        // ---------- Session ----------
        SessionStartRequested = 0,
        SessionStarting = 1,
        SessionStarted = 2,
        SessionEnded = 3,

        // next category starts at 10+
    }
}
```

- **`[EventEnum]` is required** — it registers the family with the events registry before
  the first scene loads (string -> enum lookup, `EventRef` inspector dropdowns, the
  `Window > Events > Upgrade Audit`).
- Explicit values; categories separated by `// ---------- Name ----------` comments with
  gaps of ~10 between categories.
- Naming: `*Requested` / `*Starting`/`*ing` / `*Started`/`*ed` / `*Failed` / `*Cancelled`.
- Declare payloads with `[EventPayload(typeof(T))]`. Add
  `[EventDelivery(EventDelivery.Sticky)]` only for a *level* (see CLAUDE.md
  "Delivery Policy - edge or level").

### Step 3: The bus — nothing to build

`EventsFor<AnalyticsEvents>` already works: static, lazily initialized, no publisher file,
no scene object, no execution-order concern. Subscribe and publish exactly like the
existing domains.

### Step 4 (optional): Auto-flow class

Derive from `AutoEventFlowBase<AnalyticsEvents, AnalyticsEvents>` and supply a
`static readonly (AnalyticsEvents From, AnalyticsEvents To)[]` of chains — subscribe,
dispatch and unsubscribe are handled for you. Copy the shape from
`GameFlowAutoEventFlow.cs`. Skip this until the domain actually has same-domain
progressions.

One event may declare several targets: the chains are a flat list of pairs, not a
dictionary. Targets fire in declaration order, synchronously. Do **not** chain a
`*Requested` that arrives raw from input to its `*Starting` — chaining runs before any
controller validates, which silently defeats the gate.

### Step 5: Bridge class — required as soon as the domain talks to another

Copy `TempleRunGameFlowBridge.cs`: two `static readonly` pair arrays, one per direction, each
driving an `EventChainDispatcher<TSource, TDest>` attached in `Awake()` and detached in
`OnDestroy()`. (A bidirectional bridge cannot inherit `AutoEventFlowBase` twice, which is why
it composes two dispatchers instead.) **Never reference the new domain's enum from another domain's code** — the bridge is
the only crossing point; that is the whole point of the domain. Then add mappings with
`/add-bridge-mapping`, and add the new bridge class to that skill's "Available Bridges"
table.

### Step 6: Host the components in a scene whose lifetime matches the domain

Verified current placements — follow the pattern:

| Component | Host scene | Lifetime |
|-----------|-----------|----------|
| `GameFlowAutoEventFlow` | `0_BootStrap_Game_Only` | whole app |
| `TempleRunGameFlowBridge` | `Game_Boot_2_Play` | menus + game session |
| `CountdownAutoEventFlow`, `CountdownGameFlowBridge`, `Countdown2TempleRunBridge` | `Game_Boot_2_Play` (one `CountdownDomain` object) | menus + game session |
| `TempleRunAutoEventFlow`, `Input2TempleRunAutoEventBridge` | `TempleRunGameplay` | one run |

An app-lifetime domain (e.g. analytics) hosts its flow/bridge in `0_BootStrap_Game_Only`;
session-scoped in `Game_Boot_2_Play`; gameplay-scoped in the gameplay scene. `OnDestroy`
unsubscription is what makes the shorter lifetimes safe.

### Step 7: Register the domain everywhere the current three are listed

This is the step people forget. Update every place that enumerates domains:

- `CLAUDE.md` — the Domain Registry table (Architecture Overview), Domain Isolation table,
  Namespaces block, Key Files table, folder tree
- Skills — `list-events` (Step 1/2 tables), `add-event` (Step 1 table), `add-auto-chain`
  (auto-flow files table), `add-bridge-mapping` (Available Bridges), `audit-events`
  (Check 6 + Files to Scan)
- Pointer files — `GEMINI.md` and `.github/copilot-instructions.md` (the domain-isolation
  bullet; keep the two mirrored)
- Docs — `docs/ARCHITECTURE.md`; the Domain Registry mirror at the top of `docs/EVENTS.md`
  (then regenerate the catalog via `/list-events`); the event-domain table in `README.md`

### Step 8: Verify

Run `/audit-events` — its isolation check generalizes to the new domain: the new enum's
name may appear outside its own `Assets/<Name>/` folder ONLY in bridge files. Then play a
session with `CrawfisSoftware > Events > Log Events` enabled and inspect the trace.

### Step 9: Summarize

```
Created domain: [Name]
  Enum:    Assets/[Name]/Scripts/Events/[Name]Events.cs  ([N] events)
  Bus:     EventsFor<[Name]Events>  (alias [Name]Bus)
  Flow:    [path, or "none yet — no same-domain progressions"]
  Bridge:  [path]  ([n] mappings [Name]->X, [m] mappings X->[Name])
  Host:    [scene]  (lifetime: [app | session | run])
  Registered in: CLAUDE.md, 5 skills, 2 pointer files, ARCHITECTURE.md, EVENTS.md, README.md
```
