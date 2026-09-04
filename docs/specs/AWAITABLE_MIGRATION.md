# Plan: replace coroutines with Unity's `Awaitable`

**Status:** proposal / ready to execute
**Baseline tag:** `pre-awaitable-migration` — the coroutine implementation as it shipped.
Diff any file below against that tag to see the before/after as a teaching example.
**Why:** the project is on **Unity 6.5** (`6000.5`), where `Awaitable` is the native
frame-aware async type. Coroutines are the last piece of the template that teaches a
Unity-only idiom: `IEnumerator` + `yield return` transfers to nothing outside the engine,
while `async`/`await` is the same C# students will write against every API for the rest of
their careers. The conversion is also a net *reduction* in code — most of the
`private Coroutine _x` fields and their `StopCoroutine` bookkeeping disappear.

> **How to use:** this doc is written to double as a session prompt — paste it whole into a
> fresh Claude Code session, or point one at the file. Read [CLAUDE.md](../../CLAUDE.md)
> first. No events, auto-chains or bridges change, so the event rules are unaffected — but
> the `OnDestroy` unsubscribe rule still applies to every file you touch.

## Scope

**20 files, 26 `StartCoroutine` sites**, in four shapes. (The `IEnumerator` hits in
`Input/GameControls.cs` and `Input/LeftRightJumpSlide.cs` are generated `IEnumerable`
implementations, not coroutines — leave them alone.)

| Shape | Files |
|-------|-------|
| **A. Delay, then do one thing** | `QuitController`, `LoadSceneAfterGameControlEvent`, `TimedEvent`, `GameFlowUIPanelController` (×2), `TeleportController`, `PlayerFailedController`, `PlayerFailureAutoTurnController`, `PrefabSpawnerAbstract`, `MovementInputActions` (3 cooldown helpers), `SwipeDetectorActions` |
| **B. Per-frame loop that ends** | `JumpArcController`, `SlideArcController`, `DashSpeedController`, `LaneOffsetController`, `CharacterTeleporter`, `CountdownController` |
| **C. Long-lived loop with an explicit stop** | `Metronome`, `DistanceController`, `PowerUpBuffController` (one timer per active buff) |
| **D. Wait on an `AsyncOperation`** | `UnloadNonActiveScenes` |

## API mapping

| Coroutine | Awaitable |
|-----------|-----------|
| `IEnumerator Foo()` | `async Awaitable Foo()` |
| `StartCoroutine(Foo())` | `_ = Foo();` |
| `yield return null` | `await Awaitable.NextFrameAsync(token)` |
| `yield return new WaitForEndOfFrame()` | `await Awaitable.EndOfFrameAsync(token)` |
| `yield return new WaitForSeconds(s)` | `await Awaitable.WaitForSecondsAsync(s, token)` |
| `yield return new WaitForSecondsRealtime(s)` | **no built-in** — `await Wait.ForSecondsRealtime(s, token)`, see below |
| `while (!op.isDone) yield return null` | `await op;` — Unity 6 awaits `AsyncOperation` directly |
| `yield break` | `return` |

`PauseController` / `PlayerPauseController` set `Time.timeScale = 0`, so the scaled vs.
realtime distinction is load-bearing. `WaitForSecondsAsync` is scaled (the exact equivalent
of `WaitForSeconds`, and it likewise never completes while paused). Every site that used
`WaitForSecondsRealtime` today — the quit delay, the failure hitch, the auto-turn delay, the
teleport, and both UI overlays — must stay realtime. `NextFrameAsync` is frame-based and so
is unaffected by `timeScale`, which preserves the current behaviour of the per-frame loops
exactly: they keep ticking, but their `Time.deltaTime` reads 0, so the arcs freeze.

## Two rules — that is the whole design

### Rule 1: every `await` takes a token

A coroutine dies with its MonoBehaviour. An async method does not: it keeps running against a
destroyed object until it hits an exception. Unity already provides
`MonoBehaviour.destroyCancellationToken`, so **every** await in this codebase passes it. That
single token is the entire replacement for automatic coroutine cleanup — no new fields, no
`OnDestroy` bookkeeping.

It also closes a live hazard in `MovementInputActions`: a cooldown that outlived the object
would call `Enable()` on an `InputAction` that `OnDestroy` had already disposed.

### Rule 2: one `CancellationTokenSource`, only where a restart is real

Only four places actually stop a running coroutine: `LaneOffsetController` (rapid lane
changes), `CountdownController` (restart), `Metronome` / `DistanceController` (the run ends),
and `PowerUpBuffController` (re-collecting a power-up resets its timer). Those get a plain
CTS that covers restart *and* destroy — no linked tokens, no `destroyCancellationToken`:

```csharp
private CancellationTokenSource _cts;

private void OnDestroy() => _cts?.Cancel();

private void OnLaneChangingLeft(string eventName, object sender, object data)
{
    _cts?.Cancel();
    _cts = new CancellationTokenSource();
    _ = LerpToOffset(targetOffset, TempleRunEvents.LaneChangedLeft, data, _cts.Token);
}
```

`PowerUpBuffController` just swaps `Dictionary<PowerUpType, Coroutine>` for
`Dictionary<PowerUpType, CancellationTokenSource>`; every call site keeps its shape.

Everywhere else — `Jump`, `Slide`, `Dash` and all of shape A — keeps **no field at all**.
Their "if somehow a jump is already running" guards go away: the validation gate in
`JumpController` / `SlideController` / `DashController` is what makes `*Starting`
non-reentrant, and validation is the architect's job, not the animation's.

### The corollary: never `async void`

Always `async Awaitable`, even fire-and-forget. `async void` routes a cancellation into
Unity's unhandled-exception path and prints an error every time a scene unloads;
`Awaitable`-returning methods absorb it.

## One new file

`Assets/_Common/Utility/Wait.cs` (namespace `CrawfisSoftware.Utility`) — Unity ships no
unscaled `WaitForSecondsAsync`, and eight sites need one:

```csharp
public static class Wait
{
    /// <summary>Awaitable equivalent of WaitForSecondsRealtime — ignores Time.timeScale.</summary>
    public static async Awaitable ForSecondsRealtime(float seconds, CancellationToken token)
    {
        float remaining = seconds;
        while (remaining > 0f)
        {
            await Awaitable.NextFrameAsync(token);
            remaining -= Time.unscaledDeltaTime;
        }
    }
}
```

## Worked example

`JumpArcController` is the conversion in miniature — the pilot for phase 0:

```csharp
private void OnJumpStarting(string eventName, object sender, object data)
{
    _ = RunJumpArc();
}

private async Awaitable RunJumpArc()
{
    // ...config reads unchanged...
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        // ...unchanged...
        await Awaitable.NextFrameAsync(destroyCancellationToken);
    }

    Blackboard.Instance.JumpHeightOffset = 0f;
    TempleRunBus.Publish(TempleRunEvents.JumpLanded, this, null);
}
```

The `_jumpCoroutine` field, both `StopCoroutine` calls and the reentrancy guard all
disappear. Net: fewer lines, and the method still reads top to bottom.

## Per-file notes

Everything not listed here is a mechanical application of the table above: swap the
signature, swap the `yield`, drop `using System.Collections;`.

| File | Note |
|------|------|
| `Player/DashSpeedController.cs` | Keep the deliberate one-frame defer (`yield return null; …; continue;` that dodges an event cycle) verbatim as an `await …NextFrameAsync(…); … continue;`. It looks like a wart; it isn't. |
| `Player/DistanceController.cs` | `WaitForEndOfFrame` → `EndOfFrameAsync`. `while (true)` loop; CTS started in `OnGameStarted`, cancelled in `OnGameOver` and `OnDestroy` — replaces `DeleteCoroutine()`. |
| `Player/PowerUpBuffController.cs` | `Dictionary<PowerUpType, CancellationTokenSource>`. Cancel + replace on re-collect; cancel all in `OnTempleRunEnded`. |
| `Player/CountdownController.cs` | CTS; the loop body is otherwise unchanged. |
| `Player/CharacterTeleporter.cs` | Per-frame loop on `GameTime.Instance.deltaTime` → `NextFrameAsync(destroyCancellationToken)`. |
| `Player/PlayerFailedController.cs` | `_hitchCoroutine != null` was doubling as "a hitch is already running" — keep that as a plain `bool _hitching`. Realtime wait. `StopAllCoroutines()` in `OnDestroy` goes away. |
| `Player/PlayerFailureAutoTurnController.cs` | Realtime wait; `StopAllCoroutines()` → `destroyCancellationToken`. |
| `Audio/Metronome.cs` | CTS; the `StopCoroutine(null)` guard in `StopMetronome` becomes `_cts?.Cancel()` and the comment explaining the guard can go — `?.` covers it. |
| `Input/MovementInputActions.cs`, `Input/SwipeDetectorActions.cs` | Three (resp. one) cooldown helpers; scaled waits, as today. `destroyCancellationToken` is what stops a cooldown from touching a disposed `InputAction`. |
| `TrackVisuals/PrefabSpawnerAbstract.cs` | Per-object delayed destroy. Cancellation leaves the object alive, exactly as `StopCoroutine` does today — scene unload destroys it either way. |
| `GameControl/UnloadNonActiveScenes.cs` | The biggest win: the nested `while (!op.isDone)` collapses to `foreach (var op in unloadOperations) await op;`. |
| `UI/GameFlowUIPanelController.cs` | Two realtime waits. `ShowGameOver()` stays sync and fires the async part with `_ =`. |
| `_Common/Utility/TimedEvent.cs` | Keeps the `_useRealtime` branch — one arm `Wait.ForSecondsRealtime`, the other `Awaitable.WaitForSecondsAsync`. |

## Phasing

Each phase ends with `dotnet build Assembly-CSharp.csproj` and a play session from
`Assets/GameFlow/Scenes/Boot/0_BootStrap_Game_Only`.

0. **Pilot.** `Wait.cs` + `JumpArcController`. Confirm the jump arc and the
   `JumpStarted` / `JumpLanded` timing are unchanged, and that entering and leaving Play Mode
   leaves a clean console.
1. **Shape B** — the five remaining per-frame loops.
2. **Shape A** — the ten delay-then-act sites. Mostly mechanical; the two input classes are
   the bulk of it.
3. **Shapes C and D** — the three long-lived loops and `UnloadNonActiveScenes`.
4. **Docs** — below.

## Verification

- `dotnet build Assembly-CSharp.csproj` after every phase.
- Full play session: countdown → run → lane changes → jump → slide → dash → power-up (let one
  expire, and re-collect one to reset its timer) → hit an obstacle (the hitch) → miss a turn
  (auto-turn) → die → game-over overlay → back to menu → quit. Every one of those paths owns
  a converted coroutine.
- Pause mid-jump and mid-slide: the arc must freeze and resume, not snap to its end.
- Exit Play Mode mid-run and watch for console errors — that is the destroy-token check.
- `/audit-events` still passes (nothing about events changes, but the subscribe/unsubscribe
  pairs get touched in most of these files).

**One thing to watch:** if a cancelled await ever surfaces in the console on scene unload, the
fix is a one-line `catch (OperationCanceledException) { }` — added only where it actually
appears, never pre-emptively.

## Documentation changes

### `CLAUDE.md`

- **Utilities** row of the Key Files table: add `Wait.cs` to the `Assets/_Common/Utility/…`
  list.
- **Folder Structure**, the `_Common/Utility/` line: same addition.
- **Gotchas and Warnings**: add a short subsection, *Async and coroutines* — this project
  uses `Awaitable`, not coroutines; every `await` takes `destroyCancellationToken` (or a
  controller's CTS); never `async void`; `Wait.ForSecondsRealtime` is the unscaled wait.

### `docs/ADDING_A_MECHANIC.md`

Two changes, both in the worked dodge-roll example — this is the file every student copies
from, so it is the highest-leverage edit in the migration:

- **§5, the `RollController` sample.** Replace the last two methods verbatim — the rest of
  the class (the `Awake`/`OnDestroy` subscribe pair, the `_isRolling` gate) is unchanged:

  ```csharp
      private void OnRollRequested(string e, object sender, object data)
      {
          if (_isRolling) return;             // the validation gate
          _isRolling = true;
          TempleRunBus.Publish(TempleRunEvents.RollStarting, this, null);
          _ = RollRoutine();                  // fire and forget - see the note below
      }

      private async Awaitable RollRoutine()
      {
          TempleRunBus.Publish(TempleRunEvents.RollStarted, this, null);
          // ...animate / adjust Blackboard offsets over time...
          await Awaitable.WaitForSecondsAsync(rollDuration, destroyCancellationToken);
          _isRolling = false;
          TempleRunBus.Publish(TempleRunEvents.RollEnded, this, null);
      }
  ```

  Then add two sentences of prose under the block, because this sample is where students
  meet both rules for the first time: an async method — unlike a coroutine — does **not**
  die with its MonoBehaviour, which is what `destroyCancellationToken` is for; and the
  method returns `Awaitable` rather than `void` so a cancellation is absorbed instead of
  logged as an unhandled exception.
- **§2, "The `*Started` / `*Ended` events are published by the controller when the
  animation/coroutine actually finishes"**: reword `coroutine` → `async method`.
- **§5 lead-in, "Model it on `DashController` / `SlideController`"**: still correct, but only
  after phase 3 — those two are converted in phases 1 and 2. Land this doc edit in phase 4,
  not earlier, so the walkthrough never teaches a pattern the siblings it points at don't
  use yet.

### `docs/STUDENT_TASKS.md`

- **L1 (consolidate the two spawner base classes)** — "the subclasses differ only in what
  they instantiate and whether deletion is immediate or delayed" stays true, but name the
  mechanism correctly: `SpawnerBase` destroys inline, `PrefabSpawnerAbstract` awaits
  `_debugDestroyDelayTime` first. One clause, so the task still points at real code.
- **L5 (play-mode test suite)** — add a sentence: now that gameplay is `Awaitable`-based, the
  harness can `await` a mechanic to completion instead of yielding a fixed number of frames,
  and Unity's test framework supports async tests directly. It makes the task easier and the
  tests less flaky, and students should know that before they scope it.
- **Two new tasks in section L**, both of which only become possible after this migration:
  - **L15. Await an event (S/M).** Add an `Awaitable`-returning `WaitAsync(token)` to
    `EventId<T>` / `EventsFor<T>`, so a controller can write
    `await JumpLanded.WaitAsync(token)` instead of subscribing, setting a flag, and
    unsubscribing. Convert two call sites, then argue the trade-off honestly: it reads far
    better, and it hides the subscription from `/audit-events` and from **List Current
    Subscribers**. Decide where the line is and document the rule you used.
  - **L16. Async lifetime audit (S).** The `async` sibling of L11. Write the `/audit-events`
    companion check that flags an `await` with no cancellation token, an `async void` on a
    MonoBehaviour, and a CTS that is never cancelled in `OnDestroy` — the three ways an async
    method outlives its object. Seed it by reintroducing each defect on a branch and
    confirming the check catches it.
- **The task count moves 128 → 130.** It is quoted in eight places, all of which change
  together: `docs/STUDENT_TASKS.md` (the intro), `docs/TALK_OUTLINE.md` (four),
  `docs/TIMEBOX_1_REQUIREMENTS.md`, `docs/TUTORIAL_SERIES.md`, `docs/ai/timebox-1.md`,
  `CLAUDE.md`, and `README.md`. `docs/TIMEBOX_1_REQUIREMENTS.html` and
  `docs/TIMEBOX_2_REQUIREMENTS.html` are generated from their `.md` siblings — regenerate
  rather than hand-edit.

### Not affected

`docs/ARCHITECTURE.md`, `docs/EVENTS.md`, `docs/TRACKS.md`, the `EXERCISE_*` and `TIMEBOX_*`
requirement docs, and every `.claude/skills/*/SKILL.md` — none of them mention coroutines.
The `docs/event-review/*.html` retrospectives quote code from the `pre-event-seam-audit` tag
and are deliberately frozen; leave them.
