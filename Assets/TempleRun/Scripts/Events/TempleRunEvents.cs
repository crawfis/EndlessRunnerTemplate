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

        // ---------- Player pause / resume ----------
        PlayerPauseRequested = 20,
        PlayerPausing = 21,
        PlayerPaused = 22,
        PlayerResumeRequested = 23,
        PlayerResuming = 24,
        PlayerResumed = 25,
        // Bridged from UserInitiatedEvents.UserPauseToggle. PauseController resolves the toggle
        // against its own state into PlayerPauseRequested or PlayerResumeRequested.
        PlayerPauseToggleRequested = 26,
        //PlayerPause = PlayerPaused, // Legacy naming
        //PlayerResume = PlayerResumed, // Legacy naming

        // ---------- Countdown ----------
        CountdownStartRequested = 30,
        CountdownStarting = 31,
        CountdownStarted = 32,
        CountdownTick = 33,
        CountdownEnding = 34,
        CountdownEnded = 35,
        CountdownCancelled = 36,

        // ---------- Game lifecycle (TempleRun domain) ----------
        TempleRunStartRequested = 38,
        TempleRunStarting = 39,
        TempleRunStarted = 40,
        TempleRunEndRequested = 41,
        TempleRunEnding = 42,
        TempleRunEnded = 43,

        // ---------- Player movement: turning ----------
        // Both directions carry the full ladder, published by two classes: TurnController is the
        // gate and publishes Starting. TurnCommitController commits an Either junction and
        // publishes Started; the teleport onto the new spline is the turn's duration, and
        // TeleportController publishes Ending when it lands. Only Ending -> Ended is chained.
        // Renumbered from the old 50-56 layout, which had no *Started rungs and left the
        // terminal rungs stranded at 58/59. Safe because no TempleRunEvents member is
        // serialized in a scene or prefab - unlike GameFlowEvents, which is.
        // (52-55 previously held TurnLeftEnding/TurnRightRequested/Starting/Ending; 56 held
        // SegmentRequested, now 340 with the rest of the segment vocabulary; 57 was a removed
        // StraightSegmentCompleted.)
        TurnLeftRequested = 50,
        TurnLeftStarting = 51,
        TurnLeftStarted = 52,
        TurnLeftEnding = 53,
        TurnLeftEnded = 54,
        TurnRightRequested = 55,
        TurnRightStarting = 56,
        TurnRightStarted = 57,
        TurnRightEnding = 58,
        TurnRightEnded = 59,

        // ---------- Player movement: slide ----------
        SlideRequested = 60,
        SlideStarting = 61,
        SlideStarted = 62,
        SlideEndRequested = 63,
        SlideEnding = 64,
        SlideEnded = 65,

        // ---------- Player movement: dash ----------
        DashRequested = 70,
        DashStarting = 71,
        DashStarted = 72,
        DashEnding = 73,
        DashEnded = 74,

        // ---------- Player movement: jump ----------
        JumpRequested = 80,
        JumpStarting = 81,
        JumpStarted = 82,
        JumpEndRequested = 83,
        JumpEnding = 84,
        JumpEnded = 85,

        // ---------- Player movement: lane change ----------
        LaneChangeLeftRequested = 100,
        LaneChangingLeft = 101,
        LaneChangedLeft = 102,
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
        CurrentSplineChanging = 221,
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
        TeleportStarting = 281,
        TeleportStarted = 282,
        TeleportEndRequested = 283,
        TeleportEnding = 284,
        TeleportEnded = 285,

        // ---------- Bridged from GameFlow ----------
        // Transient, and currently unpublished: the level no longer applies one resolved config, it
        // publishes its whole difficulty table via DifficultySettingsApplied and the difficulty
        // system picks from it. Kept because the name is baked into assets and the bridge mapping
        // still stands, so a project extending this template can publish it - and the declaration
        // below is what tells it, and StrictMode, what the payload must be.
        [EventPayload(typeof(DifficultyConfig))]
        TempleRunConfigApplied = 300,
        TempleRunScenesReady = 302,
        // A level: the selected level number is state, self-describing, and published once - before
        // the gameplay scene (and TrackManager) exists. Retained so TrackManager can read it at init
        // with TryGetLast instead of it being parked in a Blackboard field.
        [EventPayload(typeof(int))]
        [EventDelivery(EventDelivery.Sticky)]
        TempleRunLevelApplied = 304,          // data: int (selected level number, bridged from GameFlow)

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