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
and don't collide. Read the *publisher* column too, not just the names: those three groups
each declare a rung nothing publishes (see §5). (See also [EVENTS.md](EVENTS.md).)

## 1. Add the events

Use the skill so numbering/naming stay consistent:
```
/add-event Roll TempleRun
```
It adds a lifecycle group to `TempleRunEvents.cs`, e.g. in a free range:
```csharp
// ---------- Player movement: roll ----------
RollRequested = 90,
RollStarting  = 91,
RollStarted   = 92,
RollEnding    = 93,
RollEnded     = 94,
```
And a raw-input event to `UserInitiatedEvents.cs`:
```csharp
UserRollRequested,
```

## 2. Do NOT auto-chain the request → starting

`RollRequested` is the bridge's *raw* translation of input — it fires whether or not a roll
is currently legal. The controller is the validation gate: it publishes `RollStarting` itself
once its checks pass (see §5). Auto-chaining `RollRequested → RollStarting` would fire before
any validation runs and silently defeat the gate — that exact mapping was once live for Dash
and defeated the dash cooldown outright (see the comments in `TempleRunAutoEventFlow.cs`).

Use `/add-auto-chain` only for progressions that really are unconditional (e.g.
`PlayerDied → TempleRunEndRequested`). A chain is one `(From, To)` entry in the flow class's
`ChainTable` array:
```csharp
(TempleRunEvents.PlayerDied, TempleRunEvents.TempleRunEndRequested),
```
One source may declare several targets; they fire synchronously in declaration order.
Every other rung — `*Starting`, `*Started`, `*Ending`, `*Ended` — is published by the
controller when the animation/coroutine actually reaches that point, never auto-chained.
Chaining them would announce a stage the mechanic hasn't reached yet.

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

Subscribe to the gameplay event, validate, mutate state, and **announce** the result — one
publish per rung you declared in §1. Model the structure on `DashController` /
`SlideController`, and the end-of-action rungs on `CountdownController`, which is the one
controller in the codebase that publishes a complete `*Ending` → `*Ended` pair:
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
        StartCoroutine(RollRoutine());
    }

    private System.Collections.IEnumerator RollRoutine()
    {
        TempleRunBus.Publish(TempleRunEvents.RollStarted, this, null);
        // ...animate / adjust Blackboard offsets over time...
        yield return new WaitForSeconds(rollDuration);

        TempleRunBus.Publish(TempleRunEvents.RollEnding, this, null);
        _isRolling = false;                 // state clears between the two rungs
        TempleRunBus.Publish(TempleRunEvents.RollEnded, this, null);
    }
}
```
Keep visuals/audio out of here — other components subscribe to `RollStarted` / `RollEnded` to
play effects. That separation is the whole point.

### Publish every rung you declared

Five events instead of two is not ceremony; each rung answers a different question, and each
is the only honest place for some other system to hook in:

| Rung | Means | Who hooks in |
|------|-------|--------------|
| `RollRequested` | Someone asked. May well be illegal. | The controller, as its gate |
| `RollStarting` | The gate said yes; the roll is about to begin. | SFX, animation trigger, analytics |
| `RollStarted` | The roll is genuinely underway. | Anything that must not fire on a rejected request |
| `RollEnding` | About to finish — the last moment the player is still rolling. | Animation blend-out, restoring a collider or hitbox |
| `RollEnded` | Over; state is back to normal. | Anything that must wait for the mechanic to be done |

Note the order in `RollRoutine`: `RollEnding` fires **before** `_isRolling` clears, `RollEnded`
after. A subscriber to `RollEnding` can therefore still see the rolling state it is reacting
to. `CountdownController` does exactly this with `CountdownEnding` / `CountdownEnded`.

**A rung you declare and never publish is a dead event.** It reads as wired — it is right
there in the enum, next to four rungs that do fire — and it silently does nothing; the
[event-review retrospective](event-review/the-half-wired-chain.html) is about a whole chain
that failed this way. Three of these are live in the codebase right now: `DashEnding`,
`SlideEnding` and `JumpEnding` are declared, and nothing publishes any of them (the comment
beside Dash in `TempleRunAutoEventFlow.cs` claims otherwise and is stale). That is why the
sample above models its end rungs on `CountdownController` and not on `DashSpeedController`
— and it is
task L13 in [STUDENT_TASKS.md](STUDENT_TASKS.md), if you would rather fix it than route
around it.

So: publish every rung you declared, or don't declare it. Two more rungs are optional, and
the rule is the same — take them only if something needs them:

- `RollEndRequested` — something else asks to cut the roll short (landing, a collision, a
  power-up). `SlideEndRequested` and `JumpEndRequested` are declared for this; `Dash` has no
  such rung, because nothing interrupts a dash.
- `RollFailed` — the gate said no and something needs to react (a "can't do that" sound, a
  tutorial hint). Without it, a rejected request is silent, which is usually fine and
  occasionally the bug.

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
- [ ] Request consumed by the controller, which publishes `*Starting` only after validation
      — never auto-chain `*Requested → *Starting`
- [ ] **Every rung declared in §1 is actually published** — `*Starting`, `*Started`,
      `*Ending`, `*Ended` all come from the controller. A declared rung nothing publishes is
      a dead event; drop it from the enum instead
- [ ] Input publishes only `UserInitiatedEvents`, and unsubscribes its handlers
- [ ] Bridge maps the intent into a gameplay request (the bridge is the only `UserInitiatedEvents` subscriber)
- [ ] Controller subscribes in `Awake`, **unsubscribes in `OnDestroy`**, publishes results
- [ ] Visuals/audio live in separate subscribers, not the controller
- [ ] `/audit-events` is clean
