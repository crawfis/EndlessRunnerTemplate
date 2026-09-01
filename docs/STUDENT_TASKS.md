# Student Task Catalog: From Plain Runner to Polished Product

The template is deliberately a *plain* runner — capsule player, primitive obstacles, flat
track. That's the point: everything below is a well-scoped way to make it yours. **127
tasks**, grouped by sub-specialty so a team can divide work along interests (gameplay code,
tech art, audio, UI, systems design…). Effort tags are rough: **S** = a few days, **M** = a
week or two, **L** = a multi-week centerpiece. Tasks are referenced as section-letter +
number (e.g., A6, G3).

Play Subway Surfers and Temple Run before choosing — most of these tasks are "the thing that
makes those games feel finished." (And see section P: the best task might come from
dissecting a game *you* love.)

Tasks that need online services (networked multiplayer, cloud leaderboards, live tuning)
build on the sibling **[RunnerUGSTemplate](https://github.com/crawfis/RunnerUGSTemplate)**
(RUGS) — the same runner with Unity Gaming Services integrated behind a game/service
contract. See sections N and O — and RUGS's own
[Future Task Catalog](https://github.com/crawfis/RunnerUGSTemplate/blob/main/docs/FUTURE_TASKS.md),
which continues this catalog's lettering with sections Q–X: the full cloud-side entries,
each with an AI hand-off brief.

**Working through Timebox 1?** [TIMEBOX_1_REQUIREMENTS.md](TIMEBOX_1_REQUIREMENTS.md) is the
assignment: the five phases, the effort budget, git and AI setup, every deliverable matched
to an owner, sample team plans for five through nine-plus people, and the Greenlight
presentation running order. The tasks below are what those plans point at — two per person.

**The one rule still applies.** Whatever you build, systems communicate through events
(see [ARCHITECTURE.md](ARCHITECTURE.md)). Start every feature with the
[required skills workflow](../CLAUDE.md#required-skills-workflow): `/list-events` →
`/add-event` → implement → `/audit-events`. The walkthrough in
[ADDING_A_MECHANIC.md](ADDING_A_MECHANIC.md) is the recipe.

---

## A. Core Gameplay & Feel

1. **Real character controller with auto-run (M/L).** The player is not a physical body
   today: `MoveCharacterByDistance` *computes* the transform every frame as a pure function
   of `DistanceTracker.DistanceTravelled` — anchor + distance × direction, plus lane offset
   and jump/slide height — so nothing about the player can be blocked, pushed, or fall.
   Replace it with a real first- or third-person controller (Unity's Starter Assets, or your
   own `CharacterController`/`Rigidbody`) that applies the forward run *itself*, with input
   still doing lanes, jump, slide and dash. Cinemachine 3 is already a dependency, so the
   camera rig is authoring rather than code — but first person is a genuine design change:
   you lose the read on what's ahead, so obstacle spacing and the turn warning distance need
   retuning. The real work is the inversion — distance stops *driving* position and starts
   being *measured from* it. Keep `DistanceTracker` as the single source of truth (segment
   advance, spawning, difficulty, turn windows and teleport all read it) and feed it the
   forward component of the frame's actual movement instead of `speed × deltaTime`.
   **Prerequisite for A2 and A3** — physical failure only means something once the player is
   physically there — and the foundation of the explorer pivot (section M).
2. **Collision-driven turn failure (S/M).** Replace the distance-window turn-failure check
   with physical trigger volumes on the corner walls — running into the wall publishes
   `PlayerFailingAtTurn` instead of a distance comparison. Compare the two approaches: which
   is more tunable? More honest to the player?
3. **Trigger-based obstacle hits with a stumble state (M).** Distinct reactions: glancing
   hit → stumble (slow down, grace period, drop coins); head-on → fail. Subway Surfers'
   stumble is a big part of its fairness feel.
4. **Dodge roll (S).** The worked example in [ADDING_A_MECHANIC.md](ADDING_A_MECHANIC.md) —
   a quick ground roll under or past a hazard, sibling to Jump/Slide/Dash. A good first task
   for any team member.
5. **Double jump / air control (S).** Second jump mid-air; small lane-change authority while
   airborne. Requires reasoning about the jump controller's state machine.
6. **Wall-run or grind rails (L).** Rails or wall segments the player can commit to as an
   alternate path over obstacles. Touches track data, player state, and visuals.
7. **Climb & jump-up (M/L).** Tiles at different heights: low blocks you jump onto, tall
   ones you climb (with an animation pause). Pairs with the voxel tile art task (G3).
8. **Moving obstacles (M).** Swinging blades, rolling boulders, trains sliding between
   lanes. Spawned per-segment like static obstacles but with scripted motion — keep motion
   logic separate from spawn logic.
9. **Near-miss detection (S/M).** Detect shaving past an obstacle within ε and reward it
   (score bonus, sound, slow-mo flash). A small system with outsized feel impact.
10. **Combo / momentum multiplier (M).** Chained actions (jump→slide→near-miss) build a
    multiplier that decays when you play safe. Design the decay curve carefully.
11. **Checkpoint milestones (S).** Every 500 m: fanfare, brief invulnerability, +bonus.
12. **Revive / second chance (M).** On death, offer one revive per run (paid with a
    currency or a cooldown). Requires a clean re-entry path through the event flow —
    `PlayerReviveRequested/Reviving/Revived` already exist as hooks.
13. **Tune the feel (M).** A pure design task: sweep speeds, jump arcs, lane-change
    durations, camera FOV/follow-lag across difficulty levels; document before/after and
    playtest results.

14. **Cinemachine camera rig (M).** Cinemachine 3.1.7 ships as a dependency and is used
    *nowhere* — the camera is a plain child of the player. Build a real rig: separate cameras
    for running, jumping, dashing, turning and death, blended by state, with Impulse for
    landings and hits. Cinemachine 3.0 broke most of the 2.x API and most tutorials with it,
    so budget time for the samples. Pairs with the speed pass (F2) and the feel tuning (A13).

## B. Track Generation & World Structure

1. **Straight-tiles-only mode (M).** A subway-style level: no turns at all, lanes only —
   author a `TrackLevelSO` whose pool is exclusively `Straight` segments, then rebalance
   obstacles/coins so lane discipline carries the challenge.
2. **Wire up a non-default `ISegmentSelector` (M).** The selection policy is a pluggable
   strategy (`Assets/TempleRun/Scripts/Track/Selection/`) with four existing
   implementations (`WeightedDifficultySelector` — the default wired in `TrackManager` —
   plus `AuthoredSequenceSelector`, `DistanceRampSelector`, and `WaveSelector`). Expose the
   choice as data (e.g. per `TrackLevelSO`), wire `DistanceRampSelector` or `WaveSelector`
   into a level, and tune it until the ramp or the wave is actually felt in play.
3. **New `ISegmentSelector`: your own pacing policy (M).** Study `DistanceRampSelector` and
   `WaveSelector`, then design a selector neither covers — e.g. adaptive difficulty that
   reacts to recent failures, or authored set-pieces spliced into weighted selection.
4. **New `IPathSegmentBuilder` (L).** Geometry is also a strategy
   (`Assets/TempleRun/Scripts/Track/Geometry/`, `AxisAligned90Builder`, `ArcTurnBuilder`).
   Add S-curves, gentle slopes, or banked turns. The hardest and most rewarding code task
   in the track system.
5. **Vertical track variation (L).** Make `LaneHeights` real: raised/lowered lanes and
   segments at different elevations connected by ramps or the climb mechanic (A7).
6. **Branch-and-rejoin routes (L).** Beyond the `Either` T-junction: parallel routes that
   split and merge, one riskier but coin-rich.
7. **Biome / theme system (M).** Drive prefab sets from `VisualTheme` and switch themes
   every N segments — desert → temple → caves — including matched skybox and lighting.
8. **Authored set-piece segments (S/M).** Hand-craft `Preset` spawn-slot segments — a coin
   arc over a jump, an obstacle slalom — and weave them into levels via tags.
9. **Expose segment `Connections` (M).** The runtime supports a which-segment-may-follow
   graph; the authoring SOs don't expose it. Add it, plus editor validation for dead ends.
10. **Endless scaling (S/M).** Interpolate speed and spawn rates continuously with distance
    instead of stepping per level; find where it stops being fun.

## C. Characters & Animation

1. **Animated character (L).** Replace the capsule with a rigged character: run cycle,
   jump, slide, turn lean, death — all driven *only* by subscribing to gameplay events.
   The flagship task for a character specialist.
2. **Animation blending & sync (M).** Blend trees keyed to actual run speed; transition
   polish so slides and jumps read at 60 km/h. Follows C1.
3. **Character select & skins (M).** Multiple characters with distinct silhouettes;
   selection UI; persistence of the chosen skin. Pairs with the shop (E5).
4. **Ragdoll death (S/M).** Swap animator for physics on `PlayerDied`. Cheap drama.
5. **Chaser NPC (L).** Temple Run's monkeys / Subway's inspector: a pursuer who closes in
   when you stumble and catches you on the second mistake. This is really a *grace-period
   system with a face* — design it as state driven by fail events.
6. **Ambient NPCs (M).** Bystanders and critters along the track that react (dive away,
   cheer). Spawn/recycle like scenery; zero gameplay coupling.
7. **Companion pet (M).** Follows the player, scoops near-lane coins at intervals — a
   lighter cousin of the coin magnet power-up.

8. **AI rival racer (M/L).** `AIController` already exists as a deterministic, perfect
   turner — it publishes a turn request at a fixed distance from every wall. Make it a
   *competitor*: give it a reaction time, let it misjudge, rubber-band its speed to how the
   player is doing, and let it genuinely win or lose. A rival that never errs is a metronome,
   not an opponent, so the error model *is* the design work. Distinct from the chaser (C5),
   which pursues rather than races.
9. **World-space character UI (M).** Names, health or stamina bars, and status icons floating
   above characters — in UI Toolkit, which is the interesting part, because world-space panels
   are the awkward corner of the framework. You need this the moment there is a rival (C8) or
   a second player (N1). Read the PanelRenderer notes in [KNOWN_ISSUES.md](KNOWN_ISSUES.md)
   first.

## D. Power-Ups & Collectables

1. **New `IPowerUpEffect` strategies (S/M each).** Power-up behavior is a strategy
   interface (`Assets/TempleRun/Scripts/PowerUps/IPowerUpEffect.cs`). Add: **jetpack**
   (fly above the track collecting coin trails), **super sneakers** (jump height ×2 —
   watch the obstacle tuning), **score frenzy** (×2 stacking with combo), **hoverboard**
   (one free crash, then a cooldown).
2. **Power-up upgrade tracks (M).** Spend coins to lengthen each power-up's duration —
   the classic Subway Surfers meta-loop. Needs save persistence (L3).
3. **Collectable hunts (S/M).** Collect the letters W-O-R-D across runs for a weekly
   reward; drives return sessions.
4. **Mystery box (S).** Rare pickup granting a random reward on the summary screen.
5. **Coin choreography (S).** Author coin patterns with spawn slots: arcs over jumps,
   trails threading the safe lane — coins as a *guidance system*, not just score.
6. **Premium currency (S/M).** Rare gems alongside coins; separate wallet, separate sinks.
   Foundation for the economy tasks (E5).

7. **Inventory & consumables (M).** Power-ups apply the instant you touch them. Add a held
   slot instead: pick it up, carry it, spend it when *you* choose. One slot first, then N,
   then decide whether a second pickup swaps or stacks — that decision is the design. Builds
   on `IPowerUpEffect`, so the effects themselves need no changes.

## E. Progression, Scores & Economy

1. **Mission system (L).** Three concurrent objectives ("slide 20 times", "collect 500
   coins in one run"); completing a set raises a permanent score multiplier. The single
   strongest retention structure in the genre.
2. **Player XP & unlock ladder (M).** Account level fed by run score; levels gate skins,
   themes, power-ups.
3. **Local achievements (M).** Event-driven achievement checks with a toast UI. (The
   sibling RUGS template does this with Unity Gaming Services — compare approaches.)
4. **Daily challenge & streaks (M).** A seeded daily level (same track for everyone — the
   RNG is already injectable) plus streak rewards.
5. **Shop & monetization design (L).** Spend coins/gems on skins, upgrades, consumables;
   stub IAP behind an interface (no real store needed), and stub a rewarded-ad placement
   behind the same kind of interface. A serious design exercise in pricing a virtual economy —
   document your sink/faucet analysis, and argue where an ad belongs, or whether it does.
6. **Per-level stats & records (S).** Best distance, coins, longest combo per level;
   surface them in the level selector.
7. **Scoring rebalance (S/M).** Today score ≈ distance. Design a score model (distance +
   coins × style multiplier), expose the weights in a tuning SO, and justify the choices.
8. **Run summary screen (M).** Post-death breakdown with animated tallies — where score
   design (E7) becomes visible to the player.
9. **Locked levels & unlock criteria (M).** The level selector already persists unlocks
   and best scores (`LevelProgressManager`); make locking a real design surface: richer
   unlock criteria (score threshold on the previous level, missions completed, stars
   earned), and honest locked-state UI — padlock, progress toward unlocking, "reach
   2000 m on Level 2 to unlock." Add 1–3 star ratings per level and star-gated content.
10. **World-map level select (M/L).** Replace the level list with a map screen (in UI
    Toolkit — see section I): a winding path of level nodes showing stars and locks, with
    an animated reveal when a new level unlocks. The map *is* the progression display.

11. **Arcade initials & local high scores (S).** A three-letter entry screen and a top-ten
    table per level, persisted. Deliberately old-fashioned, genuinely satisfying, and a
    complete UI Toolkit exercise in an afternoon: focus handling, keyboard and gamepad
    navigation, and a list that animates a new entry into place.
12. **Game modes (M).** Time attack, one-life hardcore, a finite-distance level with an actual
    end, a daily seeded run. The point is *not* four forks of the game: define a mode as a
    rule-set — win condition, fail condition, starting state, which systems switch off — and
    make the existing loop read it. If adding a fifth mode means editing five files, the
    abstraction is wrong.

## F. VFX

1. **Pickup & power-up particles (S/M).** Coin sparkles, power-up auras on the player,
   collect bursts. All listeners to existing events — zero gameplay edits, which is the
   point of the architecture.
2. **Speed sensation pass (M).** FOV kick, subtle speed lines, camera shake on landings —
   perceived speed is mostly VFX, not velocity.
3. **Shader work (M/L).** Dissolve-out for despawning segments, a shimmering hologram
   marker at `Either` junctions, scrolling energy on dash. Shader Graph territory.
4. **Dash & slide trails (S).** Trail renderers toggled by `DashStarted`/`SlideStarted`.
5. **Screen-space feedback (S).** Damage vignette on stumble, gold flash on milestone,
   shield shimmer border while protected.
6. **Environmental VFX (M).** Falling leaves, dust storms, fog volumes — per biome (B7).

## G. Art & Environment

1. **Background & skybox art (M).** Layered parallax backdrops or authored skyboxes per
   theme; the cheapest way to make the game look "real."
2. **Themed obstacle & track art (M/L).** Replace primitives with modeled sets (temple
   stone, subway props). Respect the spawner prefab contract (origin, trigger colliders).
3. **Voxel tile sets with heights (L).** Voxel track tiles at multiple heights feeding the
   climb/jump-up mechanic (A7) — art and gameplay co-designed. (A voxel spawner already
   exists in `TrackVisuals` to build on.)
4. **Trackside dressing with recycling (M).** Props along the track (pillars, banners,
   wrecks) spawned per segment and pooled like everything else.
5. **Decals & wear (S).** Track-surface variety — cracks, arrows before turns, skid marks.

6. **Seeded run variation (S/M).** Randomize materials, skybox, lighting and prop sets per
   run from the injectable RNG, so two runs of the same level look different — and identical
   again when replayed from the same seed. That seed discipline is what makes the feature
   useful for bug reports, daily challenges (E4) and ghost racing (L4).

## H. Lighting & Rendering

1. **Time-of-day over a run (M).** Day → dusk → night as distance grows: gradient ambient,
   rotating sun, emissive props waking up at night.
2. **Lighting quality pass (M).** Light probes along the track, baked vs realtime
   trade-offs with a moving world, URP volume tweaks per theme.
3. **Post-processing profiles (S/M).** Bloom, color grading, vignette per biome; snappy
   transition when themes change.
4. **Rendering performance pass (M/L).** Profile, then fix: GPU instancing for tiles,
   batching, overdraw. Produce a before/after frame-time report — the report is the
   deliverable.

## I. UI / UX — UI Toolkit Only

All UI in this template runs on **UI Toolkit** (UXML/USS rendered by `PanelRenderer`), and
that is a hard constraint for every task below: **no uGUI, no Canvas, no TextMeshPro
overlays.** Learning to ship polished runtime UI in UI Toolkit — the way current Unity
expects — is part of the exercise. Read the PanelRenderer rules in
[KNOWN_ISSUES.md](KNOWN_ISSUES.md) first (show/hide via `style.display`, cache roots from
`UIReloaded`) or your panel will be mysteriously blank.

1. **HUD polish (M).** Animated score ticker, coins that fly to the counter on pickup,
   power-up duration rings.
2. **Settings menu (M).** Audio sliders, quality presets, input rebinding — persisted.
3. **First-run tutorial (M).** Contextual teach moments ("swipe up to jump" as the first
   obstacle nears) driven by game events; skippable.
4. **Game-over celebration (S/M).** New-best fanfare, progress bar to the next unlock —
   make losing feel like progress. Include an instant retry that restarts the run without
   reloading the scene set: the gap between dying and trying again is the single biggest
   lever on "one more go."
5. **Localization (M).** String tables for 2+ languages via Unity Localization — start
   from the [ConsumerUI_RxGames](https://github.com/crawfis/ConsumerUI_RxGames) sample,
   which also shows Google Sheets → translation tables — and audit every hard-coded
   string out of the UXML.
6. **Accessibility pass (M).** Colorblind-safe obstacle/pickup signaling, reduced-motion
   toggle (kills camera shake/FOV kick), scalable HUD text, full input remapping.
7. **USS design system (M).** One shared stylesheet of USS variables — color roles,
   spacing scale, type ramp, corner radii — and restyle every panel (menu, level select,
   HUD, game over) to use it. Deliverable: a one-page style guide plus zero per-panel
   hard-coded colors. This is the difference between "programmer UI" and a product.
8. **Runtime theme switching (S/M).** Swap USS theme stylesheets at runtime — light/dark,
   or a UI reskin per biome (B7) — by toggling theme classes or `ThemeStyleSheet`s on the
   root. Prove it works mid-run.
9. **Custom VisualElements (M).** Build reusable custom controls with UXML attributes: a
   radial cooldown ring for power-ups, a segmented progress bar, an odometer-style score
   counter. Package them so any panel can drop them in.
10. **UI motion pass (M).** USS transitions and scheduler-driven animation for panel
    slides, button press feedback, and staggered list reveals in the level selector.
    Every animation must respect the reduced-motion toggle (I6).
11. **HUD data binding (M/L).** The HUD currently updates from event handlers; explore UI
    Toolkit's runtime data-binding to bind labels/bars to a view-model that the event
    handlers update. Compare the two patterns and write up which you'd keep — that
    write-up is course gold.
12. **Credits & licenses screen (S).** A data-driven credits panel: one row per person,
    package, and third-party asset, read from a ScriptableObject or JSON so crediting a new
    asset is a data edit rather than a UXML edit. Point it at the same register you keep
    license URLs in, and update it the day you import something — every team writes this in
    a panic at the end of the semester; write it in week one instead.

13. **In-game feedback & bug report (S/M).** A key opens a small form, captures a screenshot
    and the last N events from `EventHistory`, and writes a report — or opens a pre-filled
    GitHub issue. There is already a `ScreenCaptureEditor` to build on. The team that ships
    this gets several times the playtest data, because "press F2 when something feels wrong"
    is a much lower bar than "remember it and tell us afterwards."

## J. Audio

1. **Adaptive music (M/L).** Layered stems that build with speed/combo intensity; duck on
   death; stinger on milestones. (The `GTMY.Audio` package is already a dependency.)
2. **Full SFX pass (M).** Footsteps by surface, whoosh on near-miss, distinct pickup
   sounds, UI clicks — every sound a subscriber to an existing event, none wired into
   gameplay code.
3. **Mixer architecture (S/M).** Music/SFX/UI buses, pause/death snapshots, ducking.
4. **Spatial & environmental audio (M).** Passing obstacles pan and doppler; reverb zones
   in tunnels or under overhangs (pairs with B7 theming).

## K. Input & Porting

1. **Mobile port (L).** Touch swipe tuning (the swipe detector exists), safe-area UI,
   a real performance budget on a mid-range phone. "It runs on my phone" is not the bar —
   "60 fps on a 3-year-old phone" is.
2. **Accelerometer lane steering (S/M).** `AccelerometerInputActions` exists — make tilt
   control feel good (dead zone, sensitivity curve, calibration button).
3. **Gamepad + haptics (S/M).** Full controller support with rumble on hits/landings.
4. **WebGL build & publish (M).** Ship to itch.io: build size diet, loading screen,
   browser input quirks. Publishing is its own skill; do it early, not last.

5. **VR / XR (L).** Bring the runner to a headset. Be warned that this is a comfort problem
   before it is a rendering one: constant forward motion the player does not control is the
   classic way to make someone sick, so the real work is vignetting, a grounded reference
   frame, snap turns, and rethinking a game built around a camera nobody steers. Start with a
   written spike.

## L. Architecture, Code & Tooling

1. **Consolidate the two spawner base classes (S/M).** The starter refactor, now that
   `AutoEventFlowBase` is done. `SpawnerBase` and `PrefabSpawnerAbstract` are parallel
   implementations of one algorithm: both subscribe to the same three events, both claim
   spawned objects into a dictionary keyed by `SegmentGeometryData.SequenceIndex`, both
   destroy a group on `ActiveTrackChanged` with a cursor starting at `-1`. `SpawnerBase`'s
   own docstring admits it — "Mirrors `PrefabSpawnerAbstract`". Extract the shared
   segment-lifetime bookkeeping into one base; the subclasses differ only in what they
   instantiate and whether deletion is immediate or delayed. Same shape as the
   `AutoEventFlowBase` consolidation, and it teaches the spawner lifecycle the way that one
   taught event dispatch.
2. **Generalized object pooling (M).** One pooling service for obstacles, coins, VFX, and
   tiles, replacing per-spawner recycling. Measure allocation before/after.
3. **Save system (M).** A versioned profile (coins, unlocks, settings, stats) saved via
   events (`SaveRequested`/`Saved` hooks already exist in `GameFlowEvents`), pluggable
   backend (PlayerPrefs vs file).
4. **Ghost replay (L).** Record the input/event stream of a run; replay it as a
   translucent ghost racing alongside. The event architecture makes the recording half
   almost free — the playback half is the project.
5. **Play-mode test suite (M).** Because everything is events, you can drive the game
   headlessly: publish inputs, assert state transitions, catch auto-chain regressions.
   Build the harness and 10 meaningful tests.
6. **In-game event console (S/M).** Debug overlay streaming the event log live (filter by
   domain), plus a "publish arbitrary event" panel. Every other team will thank you.
7. **Difficulty director (L).** Replace static difficulty with a director that watches
   player performance (deaths, near-misses, combo health) and adjusts spawn intensity —
   rubber-banding done honestly. Combines B3, B10, and E7 thinking.

8. **CI/CD (M/L).** GitHub Actions that builds the project on every pull request, publishes a
   WebGL artifact you can click, and deploys to itch.io on a tag. This is the build check
   every team wants and few get. Budget for the licensing step — Unity in CI needs an
   activation secret, and that is the part that eats the day, not the YAML.
9. **Editor authoring tools (M).** Make the track data pleasant to author: a custom inspector
   and scene-view handles for `TrackSegmentSO` spawn slots, a validation pass that flags dead
   ends and empty level pools before Play Mode does, and a window that generates segment or
   prefab variants in bulk. `TrackDataImporter` is the only editor tooling here today. Tools
   are how one designer keeps six programmers busy.
10. **Debug console & prototyping hotkeys (S/M).** A runtime overlay to skip to segment N,
    spawn any power-up, toggle invulnerability, force a turn, swap the level or the selection
    algorithm, and reload the scene set. Every hour of playtesting spent replaying the first
    200 m is an hour this gives back. Sits naturally beside the event console (L6).
11. **Subscription lifecycle audit (S/M).** Four confirmed violations of the `OnDestroy`
    rule this repo leads with, each a different failure mode:
    `LevelSelectorController` subscribes in `OnEnable` but unsubscribes only in `OnDestroy`,
    so every reopen stacks another duplicate handler; `FireEventAfterSceneLoads` attaches to
    the *static* `SceneManager.sceneLoaded` with no detach, so a destroyed component stays
    reachable; `TrackManager` subscribes inside `Initialize()` but unsubscribes once, going
    unbalanced on a second run; `QuitController` unsubscribes only from inside its own
    handler and has no `OnDestroy` at all. Fix all four, then write the `/audit-events`
    check that would have caught them. Both of the first two have a correctly-paired sibling
    beside them (`FireEventWhenSceneCloses`, `CloseSceneOnEvent`) to diff against.
12. **Reconnect the orphaned subscriber (S).** A handler waits on an event nothing
    publishes, so its behavior has never once run: `MainMenuPanelController` subscribes to
    `GameplayNotReady`, whose publisher left with the removed UGS bridge. Decide honestly:
    wire up the missing publisher, or delete the handler. The judgment call *is* the
    exercise. (A second orphan — `DistanceController`'s `PlayerFailing` speed reset — was
    fixed when `PlayerFailingAtTurn`/`PlayerFailingAtObstacle` were auto-chained into
    `PlayerFailing`; diff that fix as a worked example before choosing.)
13. **Prune the dead event surface (M).** An audit found ~61 enum members that are never
    published, subscribed, or mapped, plus several chain roots that are wired into the
    auto-flow but never fired — so the whole chain below them is unreachable. Some are
    deliberate reserved vocabulary (`*EndRequested` / `*Ending` rungs kept for symmetry);
    some are genuine cruft, like `TempleRunEvents.DifficultySettingsApplied` /
    `DifficultyChanging` / `DifficultyChanged` (320–322), which duplicate the
    `TempleRun*`-prefixed members the live code actually uses and survive only in a stale doc
    comment. Separate the reserved from the rotten, delete the latter, and document the rule
    you used. Careful: some events are Inspector-wired by enum value or `[EventName]` string
    in scene assets and will not appear in a code grep.
14. **Deduplicate the pause controllers (S).** `PauseController` and `PlayerPauseController`
    are near-identical: both subscribe to `PlayerPaused` / `PlayerResumed` and both publish
    `PlayerPauseRequested` / `PlayerResumeRequested`. Work out which is actually wired in the
    scenes, whether both run at once (and what that does to a toggle), and collapse them into
    one. A small, self-contained lesson in why duplicated event handlers are worse than
    duplicated methods.

## M. Genre Pivot: Runner → Explorer

The runner formula is constant forward speed on a narrow path. Loosen either constraint and
the game changes genre: wide spaces to roam, movement the player owns, and reasons to look
around instead of ahead. This is the biggest design swing in the catalog — treat M1–M3 as
the foundation and the rest as what you build on it. The track *generation* pipeline
survives the pivot; what changes is how the player inhabits it.

1. **Very wide paths (M/L).** Segments 10–20 lanes wide (or lane-free): grow `LaneCount` /
   `LaneWidth` and replace discrete lane-hopping with continuous lateral steering. Audit
   everything that assumes "a lane" — spawn slots, obstacle placement, coin lines.
2. **Player-controlled forward movement (M/L).** Throttle, brake, even stop — forward
   speed becomes an input, not a constant. `DistanceTracker` and everything derived from
   distance (spawning, difficulty, turn windows) must tolerate a player who lingers.
   Suddenly obstacles are things to *study*, not just dodge. Starts from A1's controller —
   the auto-run is already there; here the player takes the throttle.
3. **Explorer camera & controls (M/L).** A third-person controller within the generated
   world: camera orbit, look-around, over-the-shoulder framing. Combined with M1–M2 you
   have a walkable PCG environment.
4. **Points of interest & secrets (M).** Side alcoves, breakable walls, hidden collectable
   caches, risky detours — author them as `Preset` spawn-slot set pieces (B8) placed off
   the main line, discovered rather than survived.
5. **Objectives over distance (M/L).** Replace "run far" scoring with explore-to-find
   goals: locate three relics, light every beacon, map N% of the level. Requires a quest
   state system (event-driven, naturally) and a new win/lose definition.
6. **Minimap & compass (M).** A UI Toolkit minimap drawn from the generated segment graph,
   with fog-of-war for unvisited branches. Pairs with I9 (custom VisualElements).
7. **Backtracking (L).** Let the player turn around. This breaks the deepest assumption in
   the template — segments despawn behind you — so it's really a segment-lifecycle
   redesign: keep-alive windows, re-entry events, memory budgeting. A great systems
   deep-dive for a strong programmer.

## N. Multiplayer

Two tracks: couch multiplayer here, networked multiplayer in the RUGS sibling (whose other
cloud services are covered in section O). Fair warning about the architecture: `UserInitiatedEvents` already carry a `PlayerNumber`
payload, but `Blackboard` and most gameplay state are singletons that assume one player —
**making player state per-player instead of global is the real work**, and it's exactly the
kind of refactor the event architecture makes tractable.

1. **Split-screen foundation: two players (L).** Unity Input System device pairing (one
   gamepad each, or keyboard halves), per-player input streams, per-player state
   (position, lives, score), and two cameras rendering side-by-side viewports into the
   same generated track.
2. **2–4 player couch race (L).** Builds on N1: a shared seeded track (the RNG is
   injectable, so every player sees the identical level), 2×2 viewports for four players,
   race rules — last-alive wins, or furthest distance when time expires — and a proper
   winner screen.
3. **Saboteur party mode (M/L).** Asymmetric couch play: one player runs while another
   spends a resource budget to drop obstacles and trigger hazards ahead of them from a
   top-down view. Swap seats each round. Cheap to build once N1 exists, and reliably the
   most-laughed-at demo.
4. **Networked multiplayer via RUGS (L).** In the RunnerUGSTemplate sibling: UGS Lobby +
   Relay + Netcode for GameObjects. Keep it honest for a semester: same seeded track on
   every client, replicate only each player's inputs/position/state, render remote players
   as runners in your world. Start with two players before dreaming bigger. The full
   entry is RUGS task V3, with V1 (the spike) and V2 (the lobby) leading in.
5. **Async ghost racing (M/L).** The networked feel without netcode: upload a recorded run
   (L4) to UGS Cloud Save keyed by the leaderboard entry, download a friend's ghost, and
   race it live. Often *more* fun than real-time for a runner — and it ships. The full
   entry is RUGS task U4.

## O. Live Services with UGS (in the RUGS sibling)

These tasks live in the **[RunnerUGSTemplate](https://github.com/crawfis/RunnerUGSTemplate)**
— the same runner with **Unity Gaming Services** integrated behind a game/service contract
(the UGS domain arrives as packages and meets the game at `GameServiceEvents`, so cloud
services never touch gameplay code directly). RUGS already ships working Authentication,
Leaderboards, Achievements, Remote Config, Economy, and Cloud Code; these tasks extend
them into real live-ops features. Fair warning: UGS work has a setup tax (project linking,
environments, deployments) — budget O1 before anything else.

> Every sketch below has grown into a full entry in RUGS's
> [Future Task Catalog](https://github.com/crawfis/RunnerUGSTemplate/blob/main/docs/FUTURE_TASKS.md)
> (the id mapping — O1 → Q1 and so on — is at the top of that file), alongside sections this
> catalog never sketched: Friends, Lobby, Matchmaker, Relay + Netcode, Vivox, push
> notifications, and more. Treat this section as the menu and that file as the recipe.

1. **Stand it up (S/M).** Clone RUGS, link your own UGS project, create environments,
   deploy the config, and get the full loop running: sign in → run → score on the
   leaderboard → achievement toast. Sounds trivial; teaches the entire cloud workflow and
   is the prerequisite for everything below.
2. **Leaderboard variants (M).** Beyond the daily-distance board: weekly and all-time
   boards, per-level boards keyed to the level number, and a friends/bucket view. Design
   which boards *mean* something — a board nobody can climb is worse than none.
3. **Achievements that teach the game (M).** Replace the placeholder achievements with a
   real set tied to catalog mechanics — near-misses (A9), combos (A10), missions (E1) — as
   instant and progressive tiers. Good achievements are a curriculum for playing well.
4. **Live tuning with Remote Config (M).** Move difficulty and economy knobs
   (`DifficultyConfig` values, coin values, power-up durations) behind Remote Config so
   you can retune the live game without a rebuild. Then run a real **A/B test**: two
   scoring models (E7) served to different cohorts, compared on the leaderboard.
5. **Seasonal event (M/L).** A limited-time challenge switched on by a Remote Config
   feature flag: themed level, event currency, its own leaderboard, countdown UI. The
   full live-ops loop in miniature — ship it, run it for two weeks, retire it.
6. **Cloud Save profiles (M).** Sync the save system (L3) — coins, unlocks, stars,
   settings — to UGS Cloud Save so a player's progress follows their sign-in across
   devices. Handle the classic conflict: local progress vs. cloud progress, who wins?
7. **Server-authoritative scores with Cloud Code (L).** Don't trust the client: submit
   the run's stat block (distance, duration, coins, event counts) to a Cloud Code
   endpoint that sanity-checks it — impossible speed, coins > spawned, duration mismatch
   — before writing the leaderboard. A genuine introduction to anti-cheat thinking.
8. **Economy service integration (M/L).** Back the shop (E5) with UGS Economy: server-side
   currency balances, virtual purchases, an inventory of owned skins/upgrades. Pairs with
   O7 — a server-trusted wallet is what makes the shop cheat-resistant.
9. **Analytics-driven tuning (M).** Instrument the funnel with Unity Analytics custom
   events — where players die (which segment ids), which power-ups get used, session
   length — then present a tuning change justified by the data. The dashboard and the
   argument are the deliverable.

## P. Dissect a Game You Love

The catalog above is not a menu you're limited to — it's a pattern to imitate. The most
valuable thing you can learn from this template is how to look at *any* game mechanic and
see the events, states, and data underneath it.

1. **The dissection exercise (S design + build varies).** Pick one element from a game you
   play or a game you want to make — Subway Surfers' hoverboard save, Crossy Road's
   unlock-toy machine, Vampire Survivors' level-up choices, Mario Kart's rubber-band
   items, Alto's one-button flow, Fall Guys' round structure. Write a 1–2 page teardown:
   What states does it have? What triggers transitions? What data drives it? Why does it
   feel good? Then map it onto this template — which domain owns it, what events it
   publishes/subscribes, what ScriptableObject data it needs, which existing interface
   (`ISegmentSelector`, `IPowerUpEffect`, `IPathSegmentBuilder`) it plugs into — and build
   the smallest version that actually plays. The teardown document is graded work, not
   throwaway.
2. **The pitch ritual (ongoing).** Make P1 a team habit: each milestone, every member
   pitches one dissected element (teardown + event map + effort estimate); the team votes
   one into the sprint. The winning pitch doc *is* the spec — and by semester's end you'll
   have a backlog that looks like a real studio's.

---

## Choosing well

- **Pick a vertical slice, not a layer.** "Chaser NPC + stumble + its audio/VFX" beats
  "all the VFX in the game." Slices force the event-driven integration this template
  exists to teach.
- **Check the seams first.** If your task touches segment selection, geometry, or
  power-ups, there is probably already an interface for it (`ISegmentSelector`,
  `IPathSegmentBuilder`, `IPowerUpEffect`) — implement a strategy, don't fork the caller.
- **Art and audio tasks need no engine surgery.** Visual/audio scenes subscribe to events;
  that's why they're listed as independent tasks. If your art task seems to require editing
  a controller, re-read [ARCHITECTURE.md](ARCHITECTURE.md#the-big-idea).
- **Big swings need a spine.** The explorer pivot (M) and multiplayer (N) are
  team-defining choices, not side tasks — if you pick one, schedule its foundation tasks
  first and let everyone else's work build on it.
- **Run `/audit-events` before every merge.**
