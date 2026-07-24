# Student Task Catalog: From Plain Runner to Polished Product

The template is deliberately a *plain* runner — capsule player, primitive obstacles, flat
track. That's the point: everything below is a well-scoped way to make it yours. **109
tasks**, grouped by sub-specialty so a team can divide work along interests (gameplay code,
tech art, audio, UI, systems design…). Effort tags are rough: **S** = a few days, **M** = a
week or two, **L** = a multi-week centerpiece. Tasks are referenced as section-letter +
number (e.g., A6, G3).

Play Subway Surfers and Temple Run before choosing — most of these tasks are "the thing that
makes those games feel finished." (And see section P: the best task might come from
dissecting a game *you* love.)

Tasks that need online services (networked multiplayer, cloud leaderboards, live tuning)
build on the sibling **[RunnerUGSTemplate](https://github.com/crawfis/RunnerUGSTemplate)**
(RUGS) — the same runner with Unity Gaming Services integrated as a fourth event domain.
See sections N and O.

**The one rule still applies.** Whatever you build, systems communicate through events
(see [ARCHITECTURE.md](ARCHITECTURE.md)). Start every feature with the
[required skills workflow](../CLAUDE.md#required-skills-workflow): `/list-events` →
`/add-event` → implement → `/audit-events`. The walkthrough in
[ADDING_A_MECHANIC.md](ADDING_A_MECHANIC.md) is the recipe.

---

## A. Core Gameplay & Feel

1. **Collision-driven turn failure (S/M).** Replace the distance-window turn-failure check
   with physical trigger volumes on the corner walls — running into the wall publishes
   `PlayerFailingAtTurn` instead of a distance comparison. Compare the two approaches: which
   is more tunable? More honest to the player?
2. **Trigger-based obstacle hits with a stumble state (M).** Distinct reactions: glancing
   hit → stumble (slow down, grace period, drop coins); head-on → fail. Subway Surfers'
   stumble is a big part of its fairness feel.
3. **Roll mechanic (S).** The worked example in [ADDING_A_MECHANIC.md](ADDING_A_MECHANIC.md)
   — a quick ground roll sibling to Jump/Slide/Dash. A good first task for any team member.
4. **Double jump / air control (S).** Second jump mid-air; small lane-change authority while
   airborne. Requires reasoning about the jump controller's state machine.
5. **Wall-run or grind rails (L).** Rails or wall segments the player can commit to as an
   alternate path over obstacles. Touches track data, player state, and visuals.
6. **Climb & jump-up (M/L).** Tiles at different heights: low blocks you jump onto, tall
   ones you climb (with an animation pause). Pairs with the voxel tile art task (G3).
7. **Moving obstacles (M).** Swinging blades, rolling boulders, trains sliding between
   lanes. Spawned per-segment like static obstacles but with scripted motion — keep motion
   logic separate from spawn logic.
8. **Near-miss detection (S/M).** Detect shaving past an obstacle within ε and reward it
   (score bonus, sound, slow-mo flash). A small system with outsized feel impact.
9. **Combo / momentum multiplier (M).** Chained actions (jump→slide→near-miss) build a
   multiplier that decays when you play safe. Design the decay curve carefully.
10. **Checkpoint milestones (S).** Every 500 m: fanfare, brief invulnerability, +bonus.
11. **Revive / second chance (M).** On death, offer one revive per run (paid with a
    currency or a cooldown). Requires a clean re-entry path through the event flow —
    `PlayerReviveRequested/Reviving/Revived` already exist as hooks.
12. **Tune the feel (M).** A pure design task: sweep speeds, jump arcs, lane-change
    durations, camera FOV/follow-lag across difficulty levels; document before/after and
    playtest results.

## B. Track Generation & World Structure

1. **Straight-tiles-only mode (M).** A subway-style level: no turns at all, lanes only —
   author a `TrackLevelSO` whose pool is exclusively `Straight` segments, then rebalance
   obstacles/coins so lane discipline carries the challenge.
2. **New `ISegmentSelector`: distance-based difficulty ramp (M).** The selection policy is
   a pluggable strategy (`Assets/TempleRun/Scripts/Track/Selection/`) with two existing
   implementations (`WeightedDifficultySelector`, `AuthoredSequenceSelector`). Add one that
   ramps target difficulty with `DistanceTravelled`.
3. **New `ISegmentSelector`: wave pacing (M).** Encounter design — rest, build tension,
   spike, rest. The selector alternates easy and hard pools on a distance rhythm.
4. **New `IPathSegmentBuilder` (L).** Geometry is also a strategy
   (`Assets/TempleRun/Scripts/Track/Geometry/`, `AxisAligned90Builder`, `ArcTurnBuilder`).
   Add S-curves, gentle slopes, or banked turns. The hardest and most rewarding code task
   in the track system.
5. **Vertical track variation (L).** Make `LaneHeights` real: raised/lowered lanes and
   segments at different elevations connected by ramps or the climb mechanic (A6).
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
   stub IAP behind an interface (no real store needed). A serious design exercise in
   pricing a virtual economy — document your sink/faucet analysis.
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
   climb/jump-up mechanic (A6) — art and gameplay co-designed. (A voxel spawner already
   exists in `TrackVisuals` to build on.)
4. **Trackside dressing with recycling (M).** Props along the track (pillars, banners,
   wrecks) spawned per segment and pooled like everything else.
5. **Decals & wear (S).** Track-surface variety — cracks, arrows before turns, skid marks.

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
   make losing feel like progress.
5. **Localization (M).** String tables for 2+ languages via Unity Localization; audit
   every hard-coded string out of the UXML.
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

## L. Architecture & Code (major changes)

1. **Consolidate `AutoEventFlowBase` (S/M).** The documented starter refactor: four
   dispatch classes re-implement the same `Enum.Parse` logic; unify them in the shared
   base class. Teaches the event plumbing end to end.
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
   Suddenly obstacles are things to *study*, not just dodge.
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
   as runners in your world. Start with two players before dreaming bigger.
5. **Async ghost racing (M/L).** The networked feel without netcode: upload a recorded run
   (L4) to UGS Cloud Save keyed by the leaderboard entry, download a friend's ghost, and
   race it live. Often *more* fun than real-time for a runner — and it ships.

## O. Live Services with UGS (in the RUGS sibling)

These tasks live in the **[RunnerUGSTemplate](https://github.com/crawfis/RunnerUGSTemplate)**
— the same runner with **Unity Gaming Services** integrated as a fourth event domain
(`UGS_EventsEnum`, bridged to GameFlow by `UGSGameFlowBridge`, so cloud services never
touch gameplay code directly). RUGS already ships working Authentication, Leaderboards,
Achievements, and Remote Config; these tasks extend them into real live-ops features. Fair
warning: UGS work has a setup tax (project linking, environments, deployments) — budget
O1 before anything else.

1. **Stand it up (S/M).** Clone RUGS, link your own UGS project, create environments,
   deploy the config, and get the full loop running: sign in → run → score on the
   leaderboard → achievement toast. Sounds trivial; teaches the entire cloud workflow and
   is the prerequisite for everything below.
2. **Leaderboard variants (M).** Beyond the daily-distance board: weekly and all-time
   boards, per-level boards keyed to the level number, and a friends/bucket view. Design
   which boards *mean* something — a board nobody can climb is worse than none.
3. **Achievements that teach the game (M).** Replace the placeholder achievements with a
   real set tied to catalog mechanics — near-misses (A8), combos (A9), missions (E1) — as
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
