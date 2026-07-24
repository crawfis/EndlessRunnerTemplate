# Student Task Catalog: From Plain Runner to Polished Product

The template is deliberately a *plain* runner — capsule player, primitive obstacles, flat
track. That's the point: everything below is a well-scoped way to make it yours. Tasks are
grouped by sub-specialty so a team can divide work along interests (gameplay code, tech art,
audio, UI, systems design…). Effort tags are rough: **S** = a few days, **M** = a week or
two, **L** = a multi-week centerpiece.

Play Subway Surfers and Temple Run before choosing — most of these tasks are "the thing that
makes those games feel finished."

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
   ones you climb (with an animation pause). Pairs with the voxel art task (G3).
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

13. **Straight-tiles-only mode (M).** A subway-style level: no turns at all, lanes only —
    author a `TrackLevelSO` whose pool is exclusively `Straight` segments, then rebalance
    obstacles/coins so lane discipline carries the challenge.
14. **New `ISegmentSelector`: distance-based difficulty ramp (M).** The selection policy is
    a pluggable strategy (`Assets/TempleRun/Scripts/Track/Selection/`) with two existing
    implementations (`WeightedDifficultySelector`, `AuthoredSequenceSelector`). Add one that
    ramps target difficulty with `DistanceTravelled`.
15. **New `ISegmentSelector`: wave pacing (M).** Encounter design — rest, build tension,
    spike, rest. The selector alternates easy and hard pools on a distance rhythm.
16. **New `IPathSegmentBuilder` (L).** Geometry is also a strategy
    (`Assets/TempleRun/Scripts/Track/Geometry/`, `AxisAligned90Builder`, `ArcTurnBuilder`).
    Add S-curves, gentle slopes, or banked turns. The hardest and most rewarding code task
    in the track system.
17. **Vertical track variation (L).** Make `LaneHeights` real: raised/lowered lanes and
    segments at different elevations connected by ramps or the climb mechanic (A6).
18. **Branch-and-rejoin routes (L).** Beyond the `Either` T-junction: parallel routes that
    split and merge, one riskier but coin-rich.
19. **Biome / theme system (M).** Drive prefab sets from `VisualTheme` and switch themes
    every N segments — desert → temple → caves — including matched skybox and lighting.
20. **Authored set-piece segments (S/M).** Hand-craft `Preset` spawn-slot segments — a coin
    arc over a jump, an obstacle slalom — and weave them into levels via tags.
21. **Expose segment `Connections` (M).** The runtime supports a which-segment-may-follow
    graph; the authoring SOs don't expose it. Add it, plus editor validation for dead ends.
22. **Endless scaling (S/M).** Interpolate speed and spawn rates continuously with distance
    instead of stepping per level; find where it stops being fun.

## C. Characters & Animation

23. **Animated character (L).** Replace the capsule with a rigged character: run cycle,
    jump, slide, turn lean, death — all driven *only* by subscribing to gameplay events.
    The flagship task for a character specialist.
24. **Animation blending & sync (M).** Blend trees keyed to actual run speed; transition
    polish so slides and jumps read at 60 km/h. Follows C23.
25. **Character select & skins (M).** Multiple characters with distinct silhouettes;
    selection UI; persistence of the chosen skin. Pairs with the shop (E5).
26. **Ragdoll death (S/M).** Swap animator for physics on `PlayerDied`. Cheap drama.
27. **Chaser NPC (L).** Temple Run's monkeys / Subway's inspector: a pursuer who closes in
    when you stumble and catches you on the second mistake. This is really a *grace-period
    system with a face* — design it as state driven by fail events.
28. **Ambient NPCs (M).** Bystanders and critters along the track that react (dive away,
    cheer). Spawn/recycle like scenery; zero gameplay coupling.
29. **Companion pet (M).** Follows the player, scoops near-lane coins at intervals — a
    lighter cousin of the coin magnet power-up.

## D. Power-Ups & Collectables

30. **New `IPowerUpEffect` strategies (S/M each).** Power-up behavior is a strategy
    interface (`Assets/TempleRun/Scripts/PowerUps/IPowerUpEffect.cs`). Add: **jetpack**
    (fly above the track collecting coin trails), **super sneakers** (jump height ×2 —
    watch the obstacle tuning), **score frenzy** (×2 stacking with combo), **hoverboard**
    (one free crash, then a cooldown).
31. **Power-up upgrade tracks (M).** Spend coins to lengthen each power-up's duration —
    the classic Subway Surfers meta-loop. Needs save persistence (L4).
32. **Collectable hunts (S/M).** Collect the letters W-O-R-D across runs for a weekly
    reward; drives return sessions.
33. **Mystery box (S).** Rare pickup granting a random reward on the summary screen.
34. **Coin choreography (S).** Author coin patterns with spawn slots: arcs over jumps,
    trails threading the safe lane — coins as a *guidance system*, not just score.
35. **Premium currency (S/M).** Rare gems alongside coins; separate wallet, separate sinks.
    Foundation for the economy tasks (E5, E8).

## E. Progression, Scores & Economy

36. **Mission system (L).** Three concurrent objectives ("slide 20 times", "collect 500
    coins in one run"); completing a set raises a permanent score multiplier. The single
    strongest retention structure in the genre.
37. **Player XP & unlock ladder (M).** Account level fed by run score; levels gate skins,
    themes, power-ups.
38. **Local achievements (M).** Event-driven achievement checks with a toast UI. (The
    sibling RUGS template does this with Unity Gaming Services — compare approaches.)
39. **Daily challenge & streaks (M).** A seeded daily level (same track for everyone — the
    RNG is already injectable) plus streak rewards.
40. **Shop & monetization design (L).** Spend coins/gems on skins, upgrades, consumables;
    stub IAP behind an interface (no real store needed). A serious design exercise in
    pricing a virtual economy — document your sink/faucet analysis.
41. **Per-level stats & records (S).** Best distance, coins, longest combo per level;
    surface them in the level selector.
42. **Scoring rebalance (S/M).** Today score ≈ distance. Design a score model (distance +
    coins × style multiplier), expose the weights in a tuning SO, and justify the choices.
43. **Run summary screen (M).** Post-death breakdown with animated tallies — where score
    design (E7) becomes visible to the player.

## F. VFX

44. **Pickup & power-up particles (S/M).** Coin sparkles, power-up auras on the player,
    collect bursts. All listeners to existing events — zero gameplay edits, which is the
    point of the architecture.
45. **Speed sensation pass (M).** FOV kick, subtle speed lines, camera shake on landings —
    perceived speed is mostly VFX, not velocity.
46. **Shader work (M/L).** Dissolve-out for despawning segments, a shimmering hologram
    marker at `Either` junctions, scrolling energy on dash. Shader Graph territory.
47. **Dash & slide trails (S).** Trail renderers toggled by `DashStarted`/`SlideStarted`.
48. **Screen-space feedback (S).** Damage vignette on stumble, gold flash on milestone,
    shield shimmer border while protected.
49. **Environmental VFX (M).** Falling leaves, dust storms, fog volumes — per biome (B7).

## G. Art & Environment

50. **Background & skybox art (M).** Layered parallax backdrops or authored skyboxes per
    theme; the cheapest way to make the game look "real."
51. **Themed obstacle & track art (M/L).** Replace primitives with modeled sets (temple
    stone, subway props). Respect the spawner prefab contract (origin, trigger colliders).
52. **Voxel tile sets with heights (L).** Voxel track tiles at multiple heights feeding the
    climb/jump-up mechanic (A6) — art and gameplay co-designed. (A voxel spawner already
    exists in `TrackVisuals` to build on.)
53. **Trackside dressing with recycling (M).** Props along the track (pillars, banners,
    wrecks) spawned per segment and pooled like everything else.
54. **Decals & wear (S).** Track-surface variety — cracks, arrows before turns, skid marks.

## H. Lighting & Rendering

55. **Time-of-day over a run (M).** Day → dusk → night as distance grows: gradient ambient,
    rotating sun, emissive props waking up at night.
56. **Lighting quality pass (M).** Light probes along the track, baked vs realtime
    trade-offs with a moving world, URP volume tweaks per theme.
57. **Post-processing profiles (S/M).** Bloom, color grading, vignette per biome; snappy
    transition when themes change.
58. **Rendering performance pass (M/L).** Profile, then fix: GPU instancing for tiles,
    batching, overdraw. Produce a before/after frame-time report — the report is the
    deliverable.

## I. UI / UX

59. **HUD polish (M).** Animated score ticker, coins that fly to the counter on pickup,
    power-up duration rings.
60. **Settings menu (M).** Audio sliders, quality presets, input rebinding — persisted.
61. **First-run tutorial (M).** Contextual teach moments ("swipe up to jump" as the first
    obstacle nears) driven by game events; skippable.
62. **Game-over celebration (S/M).** New-best fanfare, progress bar to the next unlock —
    make losing feel like progress.
63. **Localization (M).** String tables for 2+ languages via Unity Localization; audit
    every hard-coded string out of the UI.
64. **Accessibility pass (M).** Colorblind-safe obstacle/pickup signaling, reduced-motion
    toggle (kills camera shake/FOV kick), scalable HUD text, full input remapping.

## J. Audio

65. **Adaptive music (M/L).** Layered stems that build with speed/combo intensity; duck on
    death; stinger on milestones. (The `GTMY.Audio` package is already a dependency.)
66. **Full SFX pass (M).** Footsteps by surface, whoosh on near-miss, distinct pickup
    sounds, UI clicks — every sound a subscriber to an existing event, none wired into
    gameplay code.
67. **Mixer architecture (S/M).** Music/SFX/UI buses, pause/death snapshots, ducking.
68. **Spatial & environmental audio (M).** Passing obstacles pan and doppler; reverb zones
    in tunnels (B9 pairs well).

## K. Input & Porting

69. **Mobile port (L).** Touch swipe tuning (the swipe detector exists), safe-area UI,
    a real performance budget on a mid-range phone. "It runs on my phone" is not the bar —
    "60 fps on a 3-year-old phone" is.
70. **Accelerometer lane steering (S/M).** `AccelerometerInputActions` exists — make tilt
    control feel good (dead zone, sensitivity curve, calibration button).
71. **Gamepad + haptics (S/M).** Full controller support with rumble on hits/landings.
72. **WebGL build & publish (M).** Ship to itch.io: build size diet, loading screen,
    browser input quirks. Publishing is its own skill; do it early, not last.

## L. Architecture & Code (major changes)

73. **Consolidate `AutoEventFlowBase` (S/M).** The documented starter refactor: four
    dispatch classes re-implement the same `Enum.Parse` logic; unify them in the shared
    base class. Teaches the event plumbing end to end.
74. **Generalized object pooling (M).** One pooling service for obstacles, coins, VFX, and
    tiles, replacing per-spawner recycling. Measure allocation before/after.
75. **Save system (M).** A versioned profile (coins, unlocks, settings, stats) saved via
    events (`SaveRequested`/`Saved` hooks already exist in `GameFlowEvents`), pluggable
    backend (PlayerPrefs vs file).
76. **Ghost replay (L).** Record the input/event stream of a run; replay it as a
    translucent ghost racing alongside. The event architecture makes the recording half
    almost free — the playback half is the project.
77. **Play-mode test suite (M).** Because everything is events, you can drive the game
    headlessly: publish inputs, assert state transitions, catch auto-chain regressions.
    Build the harness and 10 meaningful tests.
78. **In-game event console (S/M).** Debug overlay streaming the event log live (filter by
    domain), plus a "publish arbitrary event" panel. Every other team will thank you.
79. **Difficulty director (L).** Replace static difficulty with a director that watches
    player performance (deaths, near-misses, combo health) and adjusts spawn intensity —
    rubber-banding done honestly. Combines B2, B10, and E7 thinking.

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
- **Run `/audit-events` before every merge.**
