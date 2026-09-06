# Adding a Mechanic: End-to-End Walkthrough

This is the recipe for adding a gameplay mechanic the way the template intends — events first,
logic second. We'll add a **Roll** — a dodge roll: a quick ground roll under or past a hazard, sibling
to Jump/Slide/Dash — to show
every layer. The existing Dash mechanic is the closest template to copy.

The golden rule: **no system calls another system directly.** Input publishes an intent,
gameplay reacts, and state changes are announced as events.

## 0. Review what exists

```
/list-events TempleRun
```
Look at the Jump/Slide/Dash groups and their value ranges so your new events are consistent
and don't collide. Read how each rung is *reached* too, not just its name — some are
published by a controller, some arrive by auto-chain, and §2 is about choosing which.
(See also [EVENTS.md](EVENTS.md).)

## 1. Add the events

Use the skill so numbering/naming stay consistent:
```
/add-event Roll TempleRun
```
It adds a lifecycle group to `TempleRunEvents.cs`, e.g. in a free range:
```csharp
// ---------- Player movement: roll ----------
[EventPayload(typeof(int))]  // Player id
RollRequested = 90,
RollStarting  = 91,
RollStarted   = 92,
RollEnding    = 93,
RollEnded     = 94,
```
And a raw-input event to `UserInitiatedEvents.cs`:
```csharp
[EventPayload(typeof(int))]  // Player id
UserRollRequested,
```

## 2. Chain the whole ladder, then break the links you need

**Start with every link auto-chained.** A chain is one `(From, To)` entry in the flow class's
`ChainTable` array, so a five-rung ladder is four lines:
```csharp
(TempleRunEvents.RollRequested, TempleRunEvents.RollStarting),
(TempleRunEvents.RollStarting,  TempleRunEvents.RollStarted),
(TempleRunEvents.RollStarted,   TempleRunEvents.RollEnding),
(TempleRunEvents.RollEnding,    TempleRunEvents.RollEnded),
```
One source may declare several targets; they fire synchronously in declaration order.

At this point you have a working mechanic and **no controller at all**. Press the key, and
the ladder runs end to end: the animation, the SFX and the HUD can all be written and tested
against real events before any gameplay logic exists. If your roll doesn't care whether one
is already in progress, you can honestly stop here, playtest it, and move on.

Then each link you **break** out of the `ChainTable` is a place to insert code. That is what
the links are *for*:

| Link | What goes here when you break it | How often |
|------|----------------------------------|-----------|
| `Requested → Starting` | **The gate.** Should this request be allowed at all? Cooldown, already-airborne, lane boundary, not enough currency. Break it, and the controller publishes `RollStarting` only once its checks pass. | Whenever the action can be refused |
| `Starting → Started` | **Warm-up.** Anything that must complete before the action truly begins — a wind-up animation, reserving a resource, loading an asset. Break it, and the controller publishes `RollStarted` when the warm-up finishes. | Often nothing; leave it chained |
| `Started → Ending` | **The action itself** — its duration. This is the animation or timer, so the controller publishes `RollEnding` when it actually completes. | Almost always broken |
| `Ending → Ended` | **The recovery window.** A landing recovery, a stand-up animation, a "GO!" flash, a beat before control returns. | Usually nothing yet; **leave it chained** |

### Leave every link you have no code for chained

This is the point of the whole design, so it is worth being blunt about: **a link you leave
in the `ChainTable` is a seam somebody else can open.** A teammate who wants a beat between
`RollEnding` and `RollEnded` — a recovery window, a hook, a delay — adds their entry by
breaking that one link. Your controller does not change. The `RollEnded` subscribers do not
change. Nobody has to find, understand or re-time your async method.

Publishing both rungs yourself, back to back, takes that away:

```csharp
// DON'T: the link between these two is now unreachable
TempleRunBus.Publish(TempleRunEvents.RollEnding, this, null);
TempleRunBus.Publish(TempleRunEvents.RollEnded, this, null);
```

```csharp
// DO: publish the rung you actually reached, and let the chain carry it
TempleRunBus.Publish(TempleRunEvents.RollEnding, this, null);
// (TempleRunEvents.RollEnding, TempleRunEvents.RollEnded) lives in the ChainTable
```

Both versions fire the same two events in the same order this afternoon. Only the second one
can absorb a change next week without an edit to `RollController`. Two adjacent
`Publish` calls for consecutive rungs of the same ladder are always the anti-pattern — if you
find yourself writing them, the second one belongs in the `ChainTable`.

One consequence to keep straight: a chained event fires **synchronously inside** the publish
of the link's source. So do your teardown *before* publishing `RollEnding`, not between the
two rungs — by the time `RollEnded` reaches a subscriber, the state should already be clean.

### The one rule: a gate and a chain cannot share a link

The moment a controller validates a request, that link **must** leave the `ChainTable` — in
the same edit. Otherwise `RollStarting` fires from the chain regardless of what the controller
decided, and the gate is silently dead code: it runs, it returns early, and the mechanic
happens anyway.

This is not hypothetical. `DashRequested → DashStarting` sat in the `ChainTable` while
`DashController` was checking the cooldown, and the cooldown did nothing at all — see the
comments in `TempleRunAutoEventFlow.cs`, which record each link that is deliberately absent
and why. A link that is broken but undocumented reads exactly like one somebody forgot.

So: chain everything, then break a link and add its code together, never one without the
other. `/add-auto-chain` checks this for you.

## 3. Turn input into an intent

Input scripts publish **only** `UserInitiatedEvents`, never TempleRun events (keeps the input
layer domain-clean). Add a binding in `MovementInputActions.cs` (or a small dedicated input
class like `DashInputActions.cs`):
```csharp
_inputActions.Player.Roll.performed += OnRoll;      // in OnEnable
_inputActions.Player.Roll.performed -= OnRoll;      // in OnDisable/OnDestroy — always!

private void OnRoll(InputAction.CallbackContext ctx)
{
    UserInputBus.Publish(
        UserInitiatedEvents.UserRollRequested, this, PlayerNumber);
}
```
(Add the `Roll` action to the `LeftRightJumpSlide` input asset and regenerate its C# wrapper.)

## 4. Bridge input → gameplay

Translate the raw intent into a gameplay request. Every input intent does this in
`Input2TempleRunAutoEventBridge.cs`:
```csharp
(UserInitiatedEvents.UserRollRequested, TempleRunEvents.RollRequested),
```
(The bridge is the **only** place in the codebase allowed to subscribe to
`UserInitiatedEvents`; gameplay controllers subscribe to the TempleRun event it produces.)

## 5. Write the controller

You only need one once you break a link. The controller below breaks exactly two:
`Requested → Starting`, because a roll can be refused while one is already running, and
`Started → Ending`, because the roll takes time. Those two entries must be **absent** from
the `ChainTable` — the gate is dead code otherwise (§2).

The other two links stay chained, and notice what that does to the code: the controller
never publishes `RollStarted` or `RollEnded` at all. It has no warm-up and no recovery
window, so it says nothing about them — and both links stay open for whoever needs one.

Subscribe to the gameplay event, validate, mutate state, and **announce** each rung you
reach — and only those. Model it on `DashController` / `SlideController`:
```csharp
internal class RollController : MonoBehaviour
{
    private bool _isRolling;

    private void Awake()
    {
        TempleRunBus.Subscribe(
            TempleRunEvents.RollRequested, OnRollRequested);
    }

    private void OnDestroy()   // MANDATORY — matching unsubscribe
    {
        TempleRunBus.Unsubscribe(
            TempleRunEvents.RollRequested, OnRollRequested);
    }

    private void OnRollRequested(string e, object sender, object data)
    {
        if (_isRolling) return;             // the validation gate
        _isRolling = true;
        TempleRunBus.Publish(TempleRunEvents.RollStarting, this, null);
        _ = RollRoutine();                  // fire and forget; RollStarted arrives by chain
    }

    private async Awaitable RollRoutine()
    {
        // ...animate / adjust Blackboard offsets over time...
        await Awaitable.WaitForSecondsAsync(rollDuration, destroyCancellationToken);

        _isRolling = false;                 // teardown first - see below
        TempleRunBus.Publish(TempleRunEvents.RollEnding, this, null);
        // RollEnded is not published here. (RollEnding, RollEnded) is in the ChainTable,
        // so the link stays open for a recovery window someone adds later.
    }
}
```
Keep visuals/audio out of here — other components subscribe to `RollStarted` / `RollEnded` to
play effects. That separation is the whole point.

Two things about the wait, because this is where you meet both rules for the first time.
An async method — unlike a coroutine — does **not** die with its MonoBehaviour, which is
what `destroyCancellationToken` is for: every `await` in this project passes it (or the
controller's own `CancellationTokenSource`, where a restart is real). And the method returns
`Awaitable` rather than `void`, so a cancellation on scene unload is absorbed instead of
logged as an unhandled exception — never `async void`. `WaitForSecondsAsync` is scaled and so
freezes under pause; the unscaled wait is `Wait.ForSecondsRealtime` (see CLAUDE.md, *Async
and coroutines*).

### Publish every rung you declared

Five events instead of two is not ceremony; each rung answers a different question, and each
is the only honest place for some other system to hook in:

| Rung | Means | Who hooks in |
|------|-------|--------------|
| `RollRequested` | Someone asked. May well be illegal. | The controller, as its gate |
| `RollStarting` | The gate said yes; the roll is about to begin. | SFX, animation trigger, analytics |
| `RollStarted` | The roll is genuinely underway. | Anything that must not fire on a rejected request |
| `RollEnding` | The roll's own work is done and the controller is handing off. | A recovery window, a stand-up animation — inserted by breaking the link below |
| `RollEnded` | Over, including anything hooked into the link above. | Anything that must wait for the mechanic to be completely done |

Note what `RollRoutine` does *not* do: it never publishes `RollEnded`. It clears its state,
publishes the rung it actually reached, and the chain carries the ladder the rest of the way
— which is what keeps that last link available to everyone else.

**A rung that nothing reaches — not published, not chained — is a dead event.** It reads as
wired, sitting in the enum beside four rungs that do fire, and it silently does nothing; the
[event-review retrospective](event-review/the-half-wired-chain.html) is about a whole chain
that failed this way. `DashEnding`, `SlideEnding` and `JumpEnding` were exactly this until
recently: declared, and published by nobody. Reaching a rung *via the ChainTable* counts —
that is the whole point of the previous section. Never publishing it and never chaining it
does not.

If a rung has no use at all, delete it rather than leaving it as scenery; sorting the
reserved from the rotten across the rest of the enum is task L13 in
[STUDENT_TASKS.md](STUDENT_TASKS.md).

So: publish every rung you declared, or don't declare it. Two more rungs are optional, and
the rule is the same — take them only if something needs them:

- `RollEndRequested` — something else asks to cut the roll short (landing, a collision, a
  power-up). `SlideEndRequested` and `JumpEndRequested` are declared for this; `Dash` has no
  such rung, because nothing interrupts a dash.
- `RollFailed` — the gate said no and something needs to react (a "can't do that" sound, a
  tutorial hint). Without it, a rejected request is silent, which is usually fine and
  occasionally the bug.

### Shared derived data goes on the payload; shared decisions get one owner

Sooner or later two components need the same number. There are two right answers and one
tempting wrong one.

- **If it is *data* several components need — put it on the event they already receive.**
  Whoever publishes the event is usually the only one who can compute it correctly, and
  every subscriber then gets it for free.
- **If it is a *decision* — give it exactly one owner** and let that owner announce the
  outcome as an event. `TurnController` decides whether a turn is legal; nobody else votes.
- **The wrong answer is for each consumer to derive it privately** from a message that
  almost carried it.

That last one does not look like a bug, which is why it spreads. The track measures
distances from each segment's own entrance; every consumer measures from the start of the
run. Converting between them needs one number — the distance at which the segment began —
and because the message did not carry it, **five components each kept their own running
sum**: two turn components, the segment-advance trigger, the transition controller, and the
HUD. They agreed only because someone kept them agreeing by hand, and the comment that used
to sit in `TurnController.OnTrackChanging` recorded what it cost when one of them drifted.

The fix was not a new event. `TrackSegmentInfo` gained one field —
`StartDistance`, stamped once by `TrackManager`, the only component that knows the queue
order and every segment's length — plus the accessors that add it. Five accumulators became
zero, `AIController` stopped holding a `[SerializeField] TurnController` to read two numbers
off it, and no enum member was added or removed. Before you add an event to move a value
around, check whether a message that already reaches everyone could simply carry it.

The full worked example is
[TRACK_PLAYER_DECOUPLING §1](specs/TRACK_PLAYER_DECOUPLING.md).

## 6. Wire it into a scene

Add the `RollController` to the gameplay controllers object (see `TempleRunGameplay` /
`Game_Boot_2_Play`). If it needs tuning data, make a small `RollConfig` ScriptableObject like
`DashConfig`/`JumpConfig` and reference it.

## 7. Audit

```
/audit-events
```
Confirms: every subscription has a matching `OnDestroy` unsubscribe, no cross-domain leak (your
input code only touches `UserInitiatedEvents`, your controller only `TempleRunEvents`), no
unused events, no accidental cycle.

## Checklist

- [ ] Events added to the right enum(s) with consistent numbering
- [ ] The ladder runs end to end — start fully auto-chained, then break only the links that
      need code
- [ ] **No link is both chained and gated.** Every link a controller publishes by hand is
      absent from the `ChainTable`, with a comment there saying why
- [ ] **Every rung declared in §1 is actually published** — `*Starting`, `*Started`,
      `*Ending`, `*Ended` all come from the controller. A declared rung nothing publishes is
      a dead event; drop it from the enum instead
- [ ] Input publishes only `UserInitiatedEvents`, and unsubscribes its handlers
- [ ] Bridge maps the intent into a gameplay request (the bridge is the only `UserInitiatedEvents` subscriber)
- [ ] Controller subscribes in `Awake`, **unsubscribes in `OnDestroy`**, publishes results
- [ ] Visuals/audio live in separate subscribers, not the controller
- [ ] `/audit-events` is clean
