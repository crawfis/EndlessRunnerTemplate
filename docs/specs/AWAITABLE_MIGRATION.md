# Plan: replace coroutines with Unity's `Awaitable`

**Status:** implemented on branch `awaitable-migration` (2026-09-06) — phases 0–3 each built
clean and phase 4 landed; **the play session in [Verification](#verification) is still owed
before merge.** [What changed against the plan](#what-changed-against-the-plan) is at the end.
**Baseline tag:** `pre-awaitable-migration` — the coroutine implementation as it shipped.
Diff any file below against that tag to see the before/after as a teaching example.
**Why:** the project is on **Unity 6.6** (`6000.6`), where `Awaitable` is the native
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

**20 files, 23 coroutine methods, 31 `StartCoroutine` call sites**, in four shapes. (An
earlier draft said 26 sites: it counted the input classes by helper method and the rest by
call site. The per-file table below was always right; only the headline was off.) (The
`IEnumerator` hits in `Input/GameControls.cs` and `Input/LeftRightJumpSlide.cs` are generated
`IEnumerable` implementations, not coroutines — leave them alone.)

| Shape | Files |
|-------|-------|
| **A. Delay, then do one thing** | `QuitController`, `LoadSceneAfterGameControlEvent`, `TimedEvent`, `GameFlowUIPanelController` (×2), `TeleportController`, `PlayerFailedController`, `PlayerFailureAutoTurnController`, `PrefabSpawnerAbstract`, `MovementInputActions` (3 cooldown helpers, 7 call sites), `SwipeDetectorActions` (1 helper, 4 call sites) |
| **B. Per-frame loop that ends** | `JumpArcController`, `SlideArcController`, `DashSpeedController`, `LaneOffsetController` (1 helper, 2 call sites), `CharacterTeleporter`, `CountdownController` |
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
| `while (!op.isDone) yield return null` | `await Awaitable.FromAsyncOperation(op, token);` — Unity 6 can also `await op;` directly, but a bare `await op` carries no token, and Rule 1 says every await does |
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
    TempleRunBus.Publish(TempleRunEvents.JumpEnding, this, null);   // JumpEnded follows by chain
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
| `Player/DistanceController.cs` | `WaitForEndOfFrame` → `EndOfFrameAsync`. `while (true)` loop; CTS started in `OnPlayerActivated` (cancelling any earlier one first), cancelled in `OnGameOver` and `OnDestroy` — replaces `DeleteCoroutine()`. |
| `Player/PowerUpBuffController.cs` | `Dictionary<PowerUpType, CancellationTokenSource>`. Cancel + replace on re-collect; `OnPowerUpDeactivating` cancels the entry as it removes it, so "entry present ⇔ timer running" holds; cancel all in `OnTempleRunEnded` **and** `OnDestroy` (the latter is what the L17 audit will look for). |
| `Countdown/Scripts/CountdownController.cs` | CTS; the loop body is otherwise unchanged. |
| `Player/CharacterTeleporter.cs` | Per-frame loop on `GameTime.Instance.deltaTime` → `NextFrameAsync(destroyCancellationToken)`. |
| `Player/PlayerFailedController.cs` | `_hitchCoroutine != null` was doubling as "a hitch is already running" — keep that as a plain `bool _hitching`. Realtime wait. `StopAllCoroutines()` in `OnDestroy` goes away. |
| `Player/PlayerFailureAutoTurnController.cs` | Realtime wait; `StopAllCoroutines()` → `destroyCancellationToken`. |
| `Audio/Metronome.cs` | CTS; the `StopCoroutine(null)` guard in `StopMetronome` becomes `_cts?.Cancel()` and the comment explaining the guard can go — `?.` covers it. |
| `Input/MovementInputActions.cs`, `Input/SwipeDetectorActions.cs` | Three (resp. one) cooldown helpers; scaled waits, as today. `destroyCancellationToken` is what stops a cooldown from touching a disposed `InputAction`. |
| `TrackVisuals/PrefabSpawnerAbstract.cs` | Per-object delayed destroy. Cancellation leaves the object alive, exactly as `StopCoroutine` does today — scene unload destroys it either way. |
| `GameControl/UnloadNonActiveScenes.cs` | The biggest win: the nested `while (!op.isDone)` collapses to `foreach (var op in unloadOperations) await Awaitable.FromAsyncOperation(op, destroyCancellationToken);` — see the API table for why not a bare `await op`. |
| `SceneManagement/LoadSceneAfterGameControlEvent.cs` | Once the method is `async`, the compiler flags the un-awaited `SceneManager.LoadSceneAsync(...)` (CS4014, because `AsyncOperation` is now awaitable). It was fire-and-forget before too, so it becomes `_ = SceneManager.LoadSceneAsync(...)`. |
| `UI/GameFlowUIPanelController.cs` | Two realtime waits. `ShowGameOver()` stays sync and fires the async part with `_ =`. |
| `_Common/Utility/TimedEvent.cs` | Keeps the `_useRealtime` branch — one arm `Wait.ForSecondsRealtime`, the other `Awaitable.WaitForSecondsAsync`. |

## Phasing

Each phase ends with `dotnet build Assembly-CSharp.csproj` and a play session from
`Assets/GameFlow/Scenes/Boot/0_BootStrap_Game_Only`.

0. **Pilot.** `Wait.cs` + `JumpArcController`. Confirm the jump arc and the
   `JumpStarted` / `JumpEnded` timing are unchanged, and that entering and leaving Play Mode
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

Three changes, all in the worked dodge-roll example — this is the file every student copies
from, so it is the highest-leverage edit in the migration.

> **Baseline (as found on 2026-09-06):** the full-event-ladder sweep had landed, and the
> sample had moved on from what an earlier draft of this plan assumed. The `RollController`
> in §5 now publishes only `RollStarting` and `RollEnding`; `RollStarted` and `RollEnded`
> arrive by chain, and the teardown (`_isRolling = false`) comes *before* `RollEnding`, as
> the "leave every link chained" rule requires. The edit below is written against that
> file, not the older one — only the wait changes.

- **§5, the `RollController` sample.** Replace the last two methods; the rest of the class
  (the `Awake`/`OnDestroy` subscribe pair, the `_isRolling` gate) is unchanged, and so are
  the two publish rungs and their ordering.

  ```csharp
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
  ```

  Then a short paragraph under the block, because this sample is where students meet both
  rules for the first time: an async method — unlike a coroutine — does **not** die with
  its MonoBehaviour, which is what `destroyCancellationToken` is for; the method returns
  `Awaitable` rather than `void` so a cancellation is absorbed instead of logged as an
  unhandled exception; and `WaitForSecondsAsync` is scaled, with `Wait.ForSecondsRealtime`
  as the unscaled wait.
- **§2, "Nobody has to find, understand or re-time your coroutine"**: reword `coroutine` →
  `async method`.
- **§5 lead-in.** It says only "Model it on `DashController` / `SlideController`" — the
  two *gate* controllers, which own no coroutine and are untouched by this migration. (An
  earlier draft expected it to name `CountdownController` for the end rungs; it does not.)
  The sample itself resembles the arc controllers, so it still lands in phase 4, after
  `DashSpeedController` / `SlideArcController` are converted — otherwise the walkthrough
  teaches a pattern the code it resembles doesn't use yet.

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
  - **L16. Await an event (S/M).** Add an `Awaitable`-returning `WaitAsync(eventEnum, token)`
    for `EventsFor<T>`, so a controller can write
    `await TempleRunBus.WaitAsync(TempleRunEvents.JumpEnded, token)` instead of subscribing,
    setting a flag, and unsubscribing. (An earlier draft hung this off `EventId<T>`; that
    type was removed and is banned — CLAUDE.md, *Typed Payloads* — so the task is
    `EventsFor<T>`-only.) `EventsFor<T>` is a static class in the EventsPublisher package, so
    the default home is a static helper in `Assets/_Common/Events` that infers the bus from
    the enum — `await EventAwaiter.WaitAsync(TempleRunEvents.JumpEnded, token)` — built on a
    subscribe-once handler that completes an `AwaitableCompletionSource` and unsubscribes,
    plus a token registration that unsubscribes on cancel. Putting it on `EventsFor<T>`
    itself, so the bus-alias form reads, is a package change: ask the owner first. Convert two
    call sites, then argue the trade-off honestly: it reads far better, and it hides the
    subscription from `/audit-events` and from **List Current Subscribers**. Decide where the
    line is and document the rule you used.
  - **L17. Async lifetime audit (S).** The `async` sibling of L11. Write the `/audit-events`
    companion check that flags an `await` with no cancellation token, an `async void` on a
    MonoBehaviour, and a CTS that is never cancelled in `OnDestroy` — the three ways an async
    method outlives its object. Seed it by reintroducing each defect on a branch and
    confirming the check catches it.
- **The task count moves 130 → 132.** (It was 128 when this plan was written; D8 and L15
  landed first — see [TRACK_PLAYER_DECOUPLING.md](TRACK_PLAYER_DECOUPLING.md) §8. That is also
  why the two new tasks above are numbered L16/L17 rather than L15/L16.)
  **The "eight places" list below was short — the real count is fifteen.** Prose occurrences,
  all of which change together:
  `docs/STUDENT_TASKS.md` (the intro), `docs/TALK_OUTLINE.md` (**four**: two stat lines and
  two prose mentions), `docs/TIMEBOX_1_REQUIREMENTS.md`, `docs/TUTORIAL_SERIES.md`,
  `docs/ai/timebox-1.md`, `CLAUDE.md`, `README.md` — **and the two talk decks that the
  original list missed**, `docs/talk/its-just-an-endless-runner.html` (four: a stat tile, a
  speaker note, a source caption, the closing slide) and
  `docs/talk/its-just-an-endless-runner-v2.html` (four, same shape). The decks had never
  been moved to 130 — they still said 128 — so they went 128 → 132 in one step. (The
  `130-series` mentions in `docs/EVENTS.md` and task E-series are event numbers, not the
  task count; leave them.)
  **Generated — regenerate, never hand-edit:** `docs/TIMEBOX_1_REQUIREMENTS.html` and
  `docs/canvas/timebox1/*.html` come from `docs/TIMEBOX_1_REQUIREMENTS.md` via
  `python docs/canvas/build_timebox1.py`, which **requires `pandoc` on PATH**;
  `docs/TIMEBOX_2_REQUIREMENTS.html` likewise from its `.md` sibling (untouched here: the
  Timebox 2 text carries no task count). `docs/ai/timebox-1.md` is a Canvas pull snapshot
  (`pull_from_canvas.py`), so its count is hand-edited like any other prose.

### Not affected

`docs/ARCHITECTURE.md`, `docs/EVENTS.md`, `docs/TRACKS.md`, the `EXERCISE_*` and `TIMEBOX_*`
requirement docs, and every `.claude/skills/*/SKILL.md` — none of them mention coroutines.
The `docs/event-review/*.html` retrospectives quote code from the `pre-event-seam-audit` tag
and are deliberately frozen; leave them.

## What changed against the plan

Executed 2026-09-06 on branch `awaitable-migration`, phases 0–4 in order, `dotnet build`
after each (clean every time; the one warning, `PlayerLifeController._playerID` unused, is
pre-existing). Everything above was updated in place; this is the list of where the landed
code or docs differ from the plan as first written, so the diff against
`pre-awaitable-migration` reads without surprises.

- **Counts.** 31 `StartCoroutine` call sites in 23 coroutine methods, not 26 sites; the
  per-file table was already right. The two talk decks were still at 128 tasks, not 130.
- **`UnloadNonActiveScenes`** awaits `Awaitable.FromAsyncOperation(op, destroyCancellationToken)`
  rather than a bare `await op`, so Rule 1 holds without exception.
- **`LoadSceneAfterGameControlEvent`** discards the `LoadSceneAsync` result (`_ =`) — the only
  compiler warning the migration produced (CS4014), and the call was fire-and-forget before.
- **`PowerUpBuffController`** also cancels a buff's CTS when `OnPowerUpDeactivating` removes
  its entry, and cancels every remaining one in `OnDestroy`. Today only the timer itself
  reaches `OnPowerUpDeactivating`, so the first is a no-op on every live path; it just keeps
  "entry present ⇔ timer running" true if a cleanse mechanic ever publishes the request.
- **`Metronome` / `DistanceController`** cancel any earlier CTS before starting a new loop,
  the same restart shape as `LaneOffsetController`; the coroutine versions would have run
  two loops if `PlayerActivated` ever fired twice.
- **`LaneOffsetController`'s** restart cancel never fires in practice: `LaneChangeController`
  gates on `_isChanging` until the completion event, so no lerp is in flight when the next
  request arrives. It is kept because `TempleRunStarting` resets that gate.
- **L16** is `EventsFor<T>`-only (no `EventId<T>`), with the placement rule spelled out:
  the default is a helper in `Assets/_Common/Events`; adding to the package needs the
  owner's OK.
- **ADDING_A_MECHANIC** §5 sample: written against the file as it is (two publish rungs, the
  rest chained, teardown before `RollEnding`), not the older four-rung sample the plan
  quoted; the lead-in names no converted file.
- **Build mechanics.** The Editor only regenerates `Assembly-CSharp.csproj` on focus, so
  until it did, the phase 0 and 1 builds ran against a throwaway copy of the csproj with the
  `Wait.cs` `Compile` item added (deleted after each build). `Wait.cs.meta` was written by
  hand with a fresh GUID, as `/generate-segments` does for its assets. From phase 2 on the
  Editor had regenerated the real csproj and the builds used it.
- **No `catch (OperationCanceledException)` was needed** at build time; whether one is
  needed at all is what the exit-Play-Mode-mid-run check in Verification decides.
