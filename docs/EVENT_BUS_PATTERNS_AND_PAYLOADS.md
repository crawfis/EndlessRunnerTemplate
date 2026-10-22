# Event Bus Patterns and Payloads

How to subscribe, publish, declare payloads, choose a delivery policy and wire auto-chains.
The rules these snippets serve are in [AGENTS.md](../AGENTS.md); the full event list is in
[EVENTS.md](EVENTS.md).

## Bus aliases

**One static facade per domain.** `EventsFor<T>` is static and lazily initialized, so there
is no singleton, no scene GameObject and no execution-order attribute to get right. Alias it
per file:

```csharp
using GameFlowBus   = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.GameFlow.Events.GameFlowEvents>;
using TempleRunBus  = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;
using CountdownBus  = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.Countdown.Events.CountdownEvents>;
using UserInputBus  = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.Events.UserInitiatedEvents>;
```

## Subscribing to events

```csharp
private void Awake()
{
    GameFlowBus.Subscribe(GameFlowEvents.GameStarting, OnGameStarting);
}

private void OnDestroy()
{
    GameFlowBus.Unsubscribe(GameFlowEvents.GameStarting, OnGameStarting);
}

private void OnGameStarting(string eventName, object sender, object data)
{
    // Handle event - cast data if needed: var score = (float)data;
}
```

**CRITICAL: Always unsubscribe in OnDestroy()** - failure causes null reference errors after scene unload.

## Publishing events

```csharp
// Without data
GameFlowBus.Publish(GameFlowEvents.MainMenuShown, this, null);

// With data payload
float score = Blackboard.Instance.DistanceTracker.DistanceTravelled;
TempleRunBus.Publish(TempleRunEvents.PlayerDied, this, score);
```

## Typed payloads

An event that carries data declares its type on the enum member with `[EventPayload]` —
that declaration is the contract (and what StrictMode validates). Call sites do not mint
`EventId<T>` fields. Subscribe and publish through the bus alias directly, and cast the
payload on the first line of the handler:

```csharp
[EventPayload(typeof(TrackSegmentInfo))]
ActiveTrackChanging = 261,

private void Awake()     => TempleRunBus.Subscribe(TempleRunEvents.ActiveTrackChanging, OnTrackChanging);
private void OnDestroy() => TempleRunBus.Unsubscribe(TempleRunEvents.ActiveTrackChanging, OnTrackChanging);

private void OnTrackChanging(string eventName, object sender, object data)
{
    var segment = (TrackSegmentInfo)data;
}
```

The cast is deliberately bare — no `is` guard, no null check (see the no-defensive-guards
rule): a wrong payload should throw at the cast, and the `[EventPayload]` declaration on
the enum is what tells every call site (and StrictMode) which cast is right. Events with no
payload, or a genuinely variable one, stay undeclared - no declaration means no checking,
which is the intended default. Do not reintroduce `EventId<T>` fields in new code.

**A basic-typed payload gets an inline comment saying what the value *is*.**
`typeof(TrackSegmentInfo)` names its own meaning; `typeof(int)` does not, and a reader of
one member should not have to find the publisher to learn whether the number is a player,
a level or a score:

```csharp
[EventPayload(typeof(int))]  // Player id
JumpRequested = 80,

[EventPayload(typeof(float))]  // Distance travelled, run-absolute
DistanceUpdated = 330,
```

This applies to `int`, `float`, `string`, `bool`, `long` and `double`. Domain types name
themselves and need no comment.

## Delivery policy: edge or level

Default is `Transient`: a subscriber that arrives after the publish hears nothing. Mark an
event `[EventDelivery(EventDelivery.Sticky)]` **only** if it is a *level*.

- **Edge** - a transition (`MainMenuHiding`, `DifficultyChangeRequested`). Only meaningful in
  sequence. Replaying it to a late subscriber is actively wrong. Leave it `Transient`.
- **Level** - a state (`TrackLevelApplied`, `TempleRunDifficultySettingsApplied`).
  Self-describing on its own, so replay is safe. An event that already carries a payload is usually a level.

Where a level is currently expressed as **two opposing edges** (`Paused`/`Resumed`), Sticky
alone is unsafe - a late subscriber gets whichever half fired last. Model the level directly
as one event carrying the value instead, and keep the edges `Transient` for animation and SFX.

Use **Window > Events > Upgrade Audit** after a play session to see which events actually had
a late subscriber. That measurement, not the name, is the evidence for making one Sticky.

Read a retained value without subscribing with `EventsFor<T>.TryGetLast(eventEnum, out
sender, out data)` (cast `data`), which is how `TrackManager` gets the selected level at init.

## Auto-event flow pattern

Events auto-chain through flat `(From, To)` pair tables in `GameFlowAutoEventFlow.cs` and `TempleRunAutoEventFlow.cs`:

```csharp
// In GameFlowAutoEventFlow.cs - GameFlow domain auto-chains
// (three entries excerpted from a much larger table)
private static readonly (GameFlowEvents From, GameFlowEvents To)[] ChainTable =
{
    (GameFlowEvents.GameStartRequested, GameFlowEvents.GameStarting),
    (GameFlowEvents.GameScenesLoaded, GameFlowEvents.GameStartRequested),
    (GameFlowEvents.GameEnding, GameFlowEvents.GameScenesUnloadRequested),
    // ... more entries
};

protected override IReadOnlyList<(GameFlowEvents From, GameFlowEvents To)> Chains => ChainTable;
```

When `GameScenesLoaded` fires, it automatically triggers `GameStartRequested` -> `GameStarting`.

> **Chains are declared as a flat list of pairs, not a dictionary.** Every dispatch
> class (the auto-flows and the bridges) shares one implementation in
> `Assets/_Common/Events/AutoEventFlowBase.cs`:
> `EventChainDispatcher<TSource, TDest>` does subscribe-to-all, lookup and publish;
> `AutoEventFlowBase<TSource, TDest>` is the MonoBehaviour wrapper for a single direction.
> A bidirectional bridge cannot inherit twice, so `TempleRunGameFlowBridge` holds two
> dispatchers instead.
>
> The pair list exists so **one event may declare several consequences** — a dictionary
> allowed exactly one successor each. That ceiling never produced bugs directly; it produced
> workarounds. Finding a source event's slot already taken, developers published the second
> consequence by hand inside a controller, which is how failure logic came to publish pause
> events. Targets fire in declaration order, synchronously, so each target's own chain
> completes before the next is published — keep multi-target groups together and say why the
> order matters.
