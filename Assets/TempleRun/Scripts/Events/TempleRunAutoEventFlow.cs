using CrawfisSoftware.Events;

using System.Collections.Generic;


namespace CrawfisSoftware.TempleRun.Events
{
    /// <summary>
    /// Auto-chain TempleRun-specific events. Keep this focused on TempleRun internal lifecycles;
    /// cross-system bridges live in TempleRunGameFlowBridge.
    /// </summary>
    internal class TempleRunAutoEventFlow : AutoEventFlowBase<TempleRunEvents, TempleRunEvents>
    {
        // VALIDATION GATES: no player-movement *Requested event is auto-chained to its *Starting.
        // Those *Requested events are Input2TempleRunAutoEventBridge's raw translations of user
        // input, so they fire whether or not the action is currently legal. An auto-chain here
        // would run before any controller validated, silently defeating cooldowns, airborne
        // checks, and lane boundaries. Each controller publishes its own *Starting once its
        // checks pass. The lifecycle chains below (pause, activation, start, end) are different:
        // nothing gates them, so chaining is safe.
        private static readonly (TempleRunEvents From, TempleRunEvents To)[] ChainTable =
        {
            // ================================================================================
            // FAILURE LIFECYCLE
            // ================================================================================
            // Every specific failure funnels into one generic PlayerFailing. Consumers that
            // mean "the player failed somehow" subscribe to PlayerFailing; only consumers that
            // genuinely care WHICH failure (PlayerFailureAutoTurnController) take a specific.
            // Two keys may share one value. PlayerFailing is released by PlayerFailed, which
            // PlayerFailedController publishes when the hitch is over.
            (TempleRunEvents.PlayerFailingAtTurn, TempleRunEvents.PlayerFailing),
            (TempleRunEvents.PlayerFailingAtObstacle, TempleRunEvents.PlayerFailing),

            // ================================================================================
            // PAUSE / RESUME BRIDGES (mirror GameFlowAutoEventFlow)
            // ================================================================================
            (TempleRunEvents.PlayerPauseRequested, TempleRunEvents.PlayerPausing),
            (TempleRunEvents.PlayerPausing, TempleRunEvents.PlayerPaused),
            (TempleRunEvents.PlayerResumeRequested, TempleRunEvents.PlayerResuming),
            (TempleRunEvents.PlayerResuming, TempleRunEvents.PlayerResumed),

            // ================================================================================
            // PLAYER ACTIVATION (the release, bridged from the Countdown domain)
            // ================================================================================
            // PlayerActivateRequested arrives from Countdown2TempleRunBridge - gameplay's
            // translation of "the ceremony is over". Nothing gates it, so both links are chained:
            // they are seams, not stubs. A spawn-in animation goes in Requested -> Activating, a
            // grace period before hazards arm goes in Activating -> Activated, and either is added
            // by breaking that one link - no controller and no subscriber changes.
            (TempleRunEvents.PlayerActivateRequested, TempleRunEvents.PlayerActivating),
            (TempleRunEvents.PlayerActivating, TempleRunEvents.PlayerActivated),

            // ================================================================================
            // GAME START BRIDGE
            // ================================================================================
            (TempleRunEvents.TempleRunStartRequested, TempleRunEvents.TempleRunStarting),
            (TempleRunEvents.TempleRunStarting, TempleRunEvents.TempleRunStarted),

            // ================================================================================
            // GAME END BRIDGE
            // ================================================================================
            (TempleRunEvents.PlayerDied, TempleRunEvents.TempleRunEndRequested),
            (TempleRunEvents.TempleRunEndRequested, TempleRunEvents.TempleRunEnding),
            (TempleRunEvents.TempleRunEnding, TempleRunEvents.TempleRunEnded),

            // ================================================================================
            // TURN AUTO-CHAINS
            // ================================================================================
            // Turn*Requested -> Turn*Starting is NOT auto-chained: TurnController is the gate, and
            // only publishes Starting if the player is inside the turn window and the segment
            // actually bends that way.
            // Turn*Starting -> Turn*Started is NOT auto-chained either. TurnCommitController
            // subscribes to Starting, and a chain target and a subscriber of the same event have
            // no defined order between them - Started could land before the Either junction had
            // been committed. It commits first, then publishes Started itself.
            //
            // Started -> Ending is the turn's DURATION, and it is filled: Started publishes the
            // exit spline, the player teleports onto it, and TeleportController publishes
            // Turn*Ending when that motion finishes. The teleport used to hang off Ending instead,
            // which declared the turn over before the player had moved.
            // Ending -> Ended stays chained - a turn settle goes there.
            (TempleRunEvents.TurnLeftEnding, TempleRunEvents.TurnLeftEnded),
            (TempleRunEvents.TurnRightEnding, TempleRunEvents.TurnRightEnded),

            // ================================================================================
            // LANE CHANGE AUTO-CHAINS
            // ================================================================================
            // LaneChange*Requested -> LaneChanging* is NOT auto-chained. See the validation-gate
            // note at the top of this dictionary: chaining it would walk the player past a lane
            // boundary, or interrupt a change already in flight. LaneChangeController publishes
            // LaneChangingLeft/Right once its checks pass.
            // LaneChangingLeft -> LaneChangedLeft: Published by LaneOffsetController (after lerp completes)
            // LaneChangingRight -> LaneChangedRight: Published by LaneOffsetController (after lerp completes)

            // ================================================================================
            // SLIDE AUTO-CHAINS
            // ================================================================================
            // SlideRequested -> SlideStarting is NOT auto-chained. See the validation-gate note at
            // the top of this dictionary: chaining it would fire SlideStarting even when
            // SlideController rejects the request (already sliding, or still on cooldown).
            // SlideController publishes SlideStarting once its checks pass.
            // SlideStarting -> SlideStarted: Published by SlideArcController (at animation start)
            // SlideStarting -> SlideStarted -> SlideEnding: published by SlideArcController as the
            // animation reaches each rung. The last link is chained and left open: a stand-up
            // animation or recovery window goes there, with no controller edit.
            (TempleRunEvents.SlideEnding, TempleRunEvents.SlideEnded),

            // ================================================================================
            // DASH AUTO-CHAINS
            // ================================================================================
            // DashRequested -> DashStarting is NOT auto-chained. This mapping was previously live
            // and defeated the dash cooldown outright: DashRequested is the bridge's raw
            // translation of UserDashRequested, so DashStarting fired even when DashController had
            // rejected the request. DashController publishes DashStarting once its checks pass.
            // DashStarting -> DashStarted: Published by DashSpeedController (at animation start)
            // DashStarting -> DashStarted -> DashEnding: published by DashSpeedController as the
            // animation reaches each rung. The last link is chained and left open: a trail fade or
            // camera FOV ease-out goes there, with no controller edit.
            (TempleRunEvents.DashEnding, TempleRunEvents.DashEnded),

            // ================================================================================
            // JUMP AUTO-CHAINS
            // ================================================================================
            // JumpRequested -> JumpStarting is NOT auto-chained. See the validation-gate note at
            // the top of this dictionary: chaining it would launch a second jump while one is
            // already in the air. JumpController publishes JumpStarting once its checks pass.
            // JumpStarting -> JumpStarted: Published by JumpArcController (at arc apex)
            // JumpStarting -> JumpStarted -> JumpEnding: published by JumpArcController as the arc
            // reaches each rung. The last link is chained and left open: a landing recovery - a
            // hook, or a delay before control returns - goes there, with no controller edit.
            (TempleRunEvents.JumpEnding, TempleRunEvents.JumpEnded),

            // ================================================================================
            // TELEPORT AUTO-CHAINS
            // ================================================================================
            // TeleportController publishes only the *ing rungs; both links below are chained
            // because the teleport has no warm-up or wind-down of its own today. They exist so
            // one can be added later - a VFX wind-up before the move, an arrival sting after -
            // by breaking the link, with no change to TeleportController or its subscribers.
            (TempleRunEvents.TeleportStarting, TempleRunEvents.TeleportStarted),
            (TempleRunEvents.TeleportEnding, TempleRunEvents.TeleportEnded),

            // ================================================================================
            // OBSTACLE AUTO-CHAINS
            // ================================================================================
            // Gated by PowerUpBuffController for Shield support. See PowerUpBuffController.cs.
            // PowerUpBuffController subscribes to ObstacleHit and decides:
            //   Shield active  -> publishes ObstacleRecovered
            //   Shield inactive -> publishes PlayerFailingAtObstacle
            //(TempleRunEvents.ObstacleHit, TempleRunEvents.PlayerFailingAtObstacle),

            // ================================================================================
            // COIN COLLECTION AUTO-CHAINS
            // ================================================================================
            (TempleRunEvents.CoinCollectRequested, TempleRunEvents.CoinCollecting),
            // CoinCollecting -> CoinCollected: Published by CoinCollectionController

            // ================================================================================
            // POWER-UP COLLECTION AUTO-CHAINS
            // ================================================================================
            (TempleRunEvents.PowerUpCollectRequested, TempleRunEvents.PowerUpCollecting),
            // PowerUpCollecting -> PowerUpCollected: Published by PowerUpBuffController (destroys GO, confirms pickup)
            (TempleRunEvents.PowerUpCollected, TempleRunEvents.PowerUpActivateRequested),
            (TempleRunEvents.PowerUpActivateRequested, TempleRunEvents.PowerUpActivating),
            // PowerUpActivating -> PowerUpActivated: Published by PowerUpBuffController (after buff applied)
            // PowerUpDeactivateRequested: Published by PowerUpBuffController (after timer expires)
            (TempleRunEvents.PowerUpDeactivateRequested, TempleRunEvents.PowerUpDeactivating),
            // PowerUpDeactivating -> PowerUpDeactivated: Published by PowerUpBuffController (after buff removed)
        };

        protected override IReadOnlyList<(TempleRunEvents From, TempleRunEvents To)> Chains => ChainTable;
    }
}

