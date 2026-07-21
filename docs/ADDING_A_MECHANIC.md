# Adding a Mechanic: End-to-End Walkthrough

This is the recipe for adding a gameplay mechanic the way the template intends — events first,
logic second. We'll add a **Roll** (a quick ground roll, sibling to Jump/Slide/Dash) to show
every layer. The existing Dash mechanic is the closest template to copy.

The golden rule: **no system calls another system directly.** Input publishes an intent,
gameplay reacts, and state changes are announced as events.

## 0. Review what exists

```
/list-events TempleRun
```
Look at the Jump/Slide/Dash groups and their value ranges so your new events are consistent
and don't collide. (See also [EVENTS.md](EVENTS.md).)

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

## 2. Auto-chain the request → starting

Requests that kick off immediately (no async gate) auto-progress. Dash does this; mirror it:
```
/add-auto-chain TempleRunEvents.RollRequested -> TempleRunEvents.RollStarting
```
This adds one line to `TempleRunAutoEventFlow.cs`:
```csharp
{ TempleRunEvents.RollRequested, TempleRunEvents.RollStarting },
```
> Only auto-chain what should fire automatically. The `*Started` / `*Ended` events are
> published by the controller when the animation/coroutine actually finishes — never
> auto-chained.

## 3. Turn input into an intent

Input scripts publish **only** `UserInitiatedEvents`, never TempleRun events (keeps the input
layer domain-clean). Add a binding in `MovementInputActions.cs` (or a small dedicated input
class like `DashInputActions.cs`):
```csharp
_playerControls.Player.Roll.performed += OnRoll;      // in OnEnable
_playerControls.Player.Roll.performed -= OnRoll;      // in OnDisable/OnDestroy — always!

private void OnRoll(InputAction.CallbackContext ctx)
{
    EventsPublisherUserInitiated.Instance.PublishEvent(
        UserInitiatedEvents.UserRollRequested, this, PlayerNumber);
}
```
(Add the `Roll` action to the `LeftRightJumpSlide` input asset and regenerate its C# wrapper.)

## 4. Bridge input → gameplay

Translate the raw intent into a gameplay request. Dash and Slide do this in
`Input2TempleRunAutoEventBridge.cs`:
```csharp
{ UserInitiatedEvents.UserRollRequested, TempleRunEvents.RollRequested },
```
(Some inputs — turns, lane changes, jump — are instead consumed directly by their controllers.
Either is fine; the bridge is the tidy default for a new mechanic.)

## 5. Write the controller

Subscribe to the gameplay event, validate, mutate state, and **announce** the result. Model it
on `DashController` / `SlideController`:
```csharp
internal class RollController : MonoBehaviour
{
    private bool _isRolling;

    private void Awake()
    {
        EventsPublisherTempleRun.Instance.SubscribeToEvent(
            TempleRunEvents.RollStarting, OnRollStarting);
    }

    private void OnDestroy()   // MANDATORY — matching unsubscribe
    {
        EventsPublisherTempleRun.Instance.UnsubscribeToEvent(
            TempleRunEvents.RollStarting, OnRollStarting);
    }

    private void OnRollStarting(string e, object sender, object data)
    {
        if (_isRolling) return;             // guard
        _isRolling = true;
        StartCoroutine(RollRoutine());
    }

    private System.Collections.IEnumerator RollRoutine()
    {
        EventsPublisherTempleRun.Instance.PublishEvent(TempleRunEvents.RollStarted, this, null);
        // ...animate / adjust Blackboard offsets over time...
        yield return new WaitForSeconds(rollDuration);
        _isRolling = false;
        EventsPublisherTempleRun.Instance.PublishEvent(TempleRunEvents.RollEnded, this, null);
    }
}
```
Keep visuals/audio out of here — other components subscribe to `RollStarted` / `RollEnded` to
play effects. That separation is the whole point.

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
- [ ] Request → Starting auto-chained; Started/Ended published by the controller
- [ ] Input publishes only `UserInitiatedEvents`, and unsubscribes its handlers
- [ ] Bridge maps the intent into a gameplay request (or a controller consumes it directly)
- [ ] Controller subscribes in `Awake`, **unsubscribes in `OnDestroy`**, publishes results
- [ ] Visuals/audio live in separate subscribers, not the controller
- [ ] `/audit-events` is clean
