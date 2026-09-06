using System.Collections.Generic;

using CrawfisSoftware.Config;
using CrawfisSoftware.Events;

namespace CrawfisSoftware.TempleRun
{
    [EventEnum]
    public enum TempleRunEvents
    {
        // ---------- Player lifecycle ----------
        PlayerFailRequested = 0,
        PlayerFailing = 1,
        PlayerFailed = 2,
        PlayerDeathRequested = 3,
        PlayerDying = 4,
        PlayerDied = 5,
        PlayerReviveRequested = 6,
        PlayerReviving = 7,
        PlayerRevived = 8,
        PlayerFailingAtTurn = 12,
        PlayerFailingAtObstacle = 13,
        // Bridged from the Countdown domain: the ceremony's end, translated into player terms.
        // Both links below are chained in TempleRunAutoEventFlow; a spawn-in animation or grace
        // period later breaks one, with no controller edit.
        PlayerActivateRequested = 14,
        PlayerActivating = 15,
        PlayerActivated = 16,

        // ---------- Player pause / resume ----------
        PlayerPauseRequested = 20,
        PlayerPausing = 21,
        PlayerPaused = 22,
        PlayerResumeRequested = 23,
        PlayerResuming = 24,
        PlayerResumed = 25,
        // Bridged from UserInitiatedEvents.UserPauseToggle. PauseController resolves the toggle
        // against its own state into PlayerPauseRequested or PlayerResumeRequested - neither of
        // which carries the id onward, because nothing downstream is per-player yet.
        [EventPayload(typeof(int))]
        PlayerPauseToggleRequested = 26,
        //PlayerPause = PlayerPaused, // Legacy naming
        //PlayerResume = PlayerResumed, // Legacy naming

        // (30-36 previously held the countdown ladder. The countdown is a ceremony the session
        // runs, not a gameplay mechanic, so it moved to its own domain - CrawfisSoftware.Countdown
        // - and reaches gameplay only as PlayerActivateRequested via Countdown2TempleRunBridge.)

        // ---------- Game lifecycle (TempleRun domain) ----------
        TempleRunStartRequested = 38,
        TempleRunStarting = 39,
        TempleRunStarted = 40,
        // Deliberately undeclared: two sources with different payloads. The bridge forwards the
        // player id from UserQuitRequested; the ChainTable also reaches it from PlayerDied, which
        // carries the score. A genuinely variable payload stays undeclared - see CLAUDE.md.
        TempleRunEndRequested = 41,
        TempleRunEnding = 42,
        TempleRunEnded = 43,

        // ---------- Player movement: turning ----------
        // Both directions carry the full ladder, published by two classes: TurnController is the
        // gate and publishes Starting. TurnCommitController commits an Either junction and
        // publishes Started; the teleport onto the new spline is the turn's duration, and
        // TeleportController publishes Ending when it lands. Only Ending -> Ended is chained.
        // Started carries the run-absolute distance the turn was committed at; the terminal rungs
        // carry nothing. Ending used to forward the exit spline, which no subscriber read and
        // which parked track vocabulary on a player event.
        // Renumbered from the old 50-56 layout, which had no *Started rungs and left the
        // terminal rungs stranded at 58/59. Safe because no TempleRunEvents member is
        // serialized in a scene or prefab - unlike GameFlowEvents, which is.
        // (52-55 previously held TurnLeftEnding/TurnRightRequested/Starting/Ending; 56 held
        // SegmentRequested, now 340 with the rest of the segment vocabulary; 57 was a removed
        // StraightSegmentCompleted.)
        // The eight rungs below marked (int) are the bridge's translations of an input request,
        // and the bridge forwards its payload unchanged - so the player id the input source
        // published arrives here. They have no other publisher.
        [EventPayload(typeof(int))]
        TurnLeftRequested = 50,
        TurnLeftStarting = 51,
        TurnLeftStarted = 52,
        TurnLeftEnding = 53,
        TurnLeftEnded = 54,
        [EventPayload(typeof(int))]
        TurnRightRequested = 55,
        TurnRightStarting = 56,
        TurnRightStarted = 57,
        TurnRightEnding = 58,
        TurnRightEnded = 59,

        // ---------- Player movement: slide ----------
        [EventPayload(typeof(int))]
        SlideRequested = 60,
        SlideStarting = 61,
        SlideStarted = 62,
        SlideEndRequested = 63,
        SlideEnding = 64,
        SlideEnded = 65,

        // ---------- Player movement: dash ----------
        [EventPayload(typeof(int))]
        DashRequested = 70,
        DashStarting = 71,
        DashStarted = 72,
        DashEnding = 73,
        DashEnded = 74,

        // ---------- Player movement: jump ----------
        [EventPayload(typeof(int))]
        JumpRequested = 80,
        JumpStarting = 81,
        JumpStarted = 82,
        JumpEndRequested = 83,
        JumpEnding = 84,
        JumpEnded = 85,

        // ---------- Player movement: lane change ----------
        [EventPayload(typeof(int))]
        LaneChangeLeftRequested = 100,
        LaneChangingLeft = 101,
        LaneChangedLeft = 102,
        [EventPayload(typeof(int))]
        LaneChangeRightRequested = 103,
        LaneChangingRight = 104,
        LaneChangedRight = 105,
        LaneChangeLeftFailed = 106,
        LaneChangeRightFailed = 107,

        // ---------- Player hazards / collisions ----------
        ObstacleHit = 120,
        ObstacleRecoveryRequested = 121,
        ObstacleRecovering = 122,
        ObstacleRecovered = 123,

        // ---------- Player interaction: coins / power-ups ----------
        CoinCollectRequested = 140,
        CoinCollecting = 141,
        CoinCollected = 142,

        PowerUpCollectRequested = 160,
        PowerUpCollecting = 161,
        PowerUpCollected = 162,

        PowerUpActivateRequested = 180,
        PowerUpActivating = 181,
        PowerUpActivated = 182,
        PowerUpDeactivateRequested = 183,
        PowerUpDeactivating = 184,
        PowerUpDeactivated = 185,

        // ---------- Abstract track generation (splines) ----------
        SplineSegmentCreateRequested = 200,
        SplineSegmentCreating = 201,
        // Published by PathProvider, one per consecutive point pair of every span - several for a
        // turn, one for a straight. Drives the spawners and the visual prefab spawner.
        [EventPayload(typeof(SplineSegmentData))]
        SplineSegmentCreated = 202,
        SplineSegmentReleaseRequested = 203,
        SplineSegmentReleasing = 204,
        SplineSegmentReleased = 205,

        CurrentSplineChangeRequested = 220,
        // The path the player is on and - via SplineSection.TeleportOwnsTransform - who writes
        // their transform while it is current. That rule used to be a Direction comparison each
        // subscriber made for itself against an unnamed four-slot tuple.
        [EventPayload(typeof(SplineSection))]
        CurrentSplineChanging = 221,
        // Published at segment exit. No subscriber today; the declaration is what tells the next
        // one, and StrictMode, what it will receive.
        [EventPayload(typeof(SplineSection))]
        CurrentSplineChanged = 222,

        // ---------- Track generation (segments/tiles) ----------
        TrackSegmentCreateRequested = 240,
        TrackSegmentCreating = 241,
        [EventPayload(typeof(TrackSegmentInfo))]
        TrackSegmentCreated = 242,
        TrackSegmentRecycleRequested = 243,
        TrackSegmentRecycling = 244,
        TrackSegmentRecycled = 245,

        ActiveTrackChangeRequested = 260,
        [EventPayload(typeof(TrackSegmentInfo))]
        ActiveTrackChanging = 261,
        [EventPayload(typeof(TrackSegmentInfo))]
        ActiveTrackChanged = 262,

        // ---------- Teleportation ----------
        TeleportRequested = 280,
        // Duration plus destination. The terminal rungs carry the destination alone: by the time
        // the teleport ends its duration is spent and nothing reads it.
        [EventPayload(typeof(TeleportInfo))]
        TeleportStarting = 281,
        [EventPayload(typeof(TeleportInfo))]
        TeleportStarted = 282,
        TeleportEndRequested = 283,
        [EventPayload(typeof(SplineSection))]
        TeleportEnding = 284,
        [EventPayload(typeof(SplineSection))]
        TeleportEnded = 285,

        // ---------- Bridged from GameFlow ----------
        // Transient, and currently unpublished: the level no longer applies one resolved config, it
        // publishes its whole difficulty table via DifficultySettingsApplied and the difficulty
        // system picks from it. Kept because the name is baked into assets and the bridge mapping
        // still stands, so a project extending this template can publish it - and the declaration
        // below is what tells it, and StrictMode, what the payload must be.
        [EventPayload(typeof(DifficultyConfig))]
        TempleRunConfigApplied = 300,
        // Begin the run's initialization: the gameplay scenes exist, so the track can be built.
        // Named for what the domain does with it, not for the GameFlow event that triggers it.
        RunInitializeRequested = 302,
        // A level: the selected track level is state, self-describing, and published once - before
        // the gameplay scene (and TrackManager) exists. Retained so TrackManager can read it at init
        // with TryGetLast instead of it being parked in a Blackboard field.
        [EventPayload(typeof(int))]
        [EventDelivery(EventDelivery.Sticky)]
        TrackLevelApplied = 304,              // data: int (the selected track level number, bridged from GameFlow)

        // ---------- Difficulty (bridged to/from GameFlow) ----------
        // A level: this IS the difficulty table, not a transition into one. GameDifficultyManager
        // is its only subscriber and has no other way to populate itself, so missing this left
        // SetDifficulty warning and no-opping against an empty table. Retained so the manager is
        // populated whenever it subscribes. PopulateDifficulties clears first, so a replay
        // followed by a live publish is idempotent.
        [EventPayload(typeof(IList<DifficultyConfig>))]
        [EventDelivery(EventDelivery.Sticky)]
        TempleRunDifficultySettingsApplied = 310,
        [EventPayload(typeof(DifficultyConfig))]
        TempleRunDifficultyChanging = 312,
        [EventPayload(typeof(DifficultyConfig))]
        TempleRunDifficultyChanged = 314,
        TempleRunDifficultyChangeFailed = 316,
        // The requested difficulty's name, resolved against the selected level's variants.
        [EventPayload(typeof(string))]
        TempleRunDifficultyChangeRequested = 318,

        // ---------- New difficulty events (direct, non-legacy) ----------
        DifficultySettingsApplied = 320,
        DifficultyChanging = 321,
        DifficultyChanged = 322,
        // The config still in effect after the change was refused; null when none is current.
        [EventPayload(typeof(DifficultyConfig))]
        DifficultyChangeFailed = 323,

        // ---------- Distance tracking (for achievements/UGS) ----------
        [EventPayload(typeof(float))]
        DistanceUpdated = 330,

        // ---------- Segment lifecycle ----------
        // Moved here from 56: this is segment vocabulary, not a rung of the turn ladder. It is
        // published by TurnCommitController between Turn*Started and Turn*Ending, and that
        // position is load-bearing - see the comment there.
        [EventPayload(typeof(Direction))]
        SegmentRequested = 340,           // Data: Direction (Left or Right). Player commits a direction at an Either junction.
        // TrackSegmentInfo is a struct, so these declarations also make a null payload an error
        // rather than a default-valued segment silently reaching a handler.
        [EventPayload(typeof(TrackSegmentInfo))]
        SegmentEntering = 342,            // Data: TrackSegmentInfo. Player approaching segment entrance.
        [EventPayload(typeof(TrackSegmentInfo))]
        SegmentEntered = 343,             // Data: TrackSegmentInfo. Player entered segment.
        [EventPayload(typeof(TrackSegmentInfo))]
        SegmentExiting = 344,             // Data: TrackSegmentInfo. Player approaching segment exit.
        [EventPayload(typeof(TrackSegmentInfo))]
        SegmentExited = 345,              // Data: TrackSegmentInfo. Player exited segment.

        // ---------- Segment geometry ----------
        [EventPayload(typeof(SegmentGeometryData))]
        SegmentGeometryReady = 350,       // Data: SegmentGeometryData. Full geometry built for a segment.
    }
}