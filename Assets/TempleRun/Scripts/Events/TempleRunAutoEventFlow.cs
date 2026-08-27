using System.Collections.Generic;

using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

namespace CrawfisSoftware.TempleRun.Events
{
    /// <summary>
    /// Auto-chain TempleRun-specific events. Keep this focused on TempleRun internal lifecycles;
    /// cross-system bridges live in TempleRunGameFlowBridge.
    /// </summary>
    internal class TempleRunAutoEventFlow : MonoBehaviour
    {
        // VALIDATION GATES: no player-movement *Requested event is auto-chained to its *Starting.
        // Those *Requested events are Input2TempleRunAutoEventBridge's raw translations of user
        // input, so they fire whether or not the action is currently legal. An auto-chain here
        // would run before any controller validated, silently defeating cooldowns, airborne
        // checks, and lane boundaries. Each controller publishes its own *Starting once its
        // checks pass. The lifecycle chains below (pause, countdown, start, end) are different:
        // nothing gates them, so chaining is safe.
        private readonly Dictionary<TempleRunEvents, TempleRunEvents> _autoTempleRun2TempleRunEvents = new Dictionary<TempleRunEvents, TempleRunEvents>()
        {
            // ================================================================================
            // FAILURE LIFECYCLE
            // ================================================================================
            // Every specific failure funnels into one generic PlayerFailing. Consumers that
            // mean "the player failed somehow" subscribe to PlayerFailing; only consumers that
            // genuinely care WHICH failure (PlayerFailureAutoTurnController) take a specific.
            // Two keys may share one value. PlayerFailing is released by PlayerFailed, which
            // PlayerFailedController publishes when the hitch is over.
            { TempleRunEvents.PlayerFailingAtTurn, TempleRunEvents.PlayerFailing },
            { TempleRunEvents.PlayerFailingAtObstacle, TempleRunEvents.PlayerFailing },

            // ================================================================================
            // PAUSE / RESUME BRIDGES (mirror GameFlowAutoEventFlow)
            // ================================================================================
            { TempleRunEvents.PlayerPauseRequested, TempleRunEvents.PlayerPausing },
            { TempleRunEvents.PlayerPausing, TempleRunEvents.PlayerPaused },
            { TempleRunEvents.PlayerResumeRequested, TempleRunEvents.PlayerResuming },
            { TempleRunEvents.PlayerResuming, TempleRunEvents.PlayerResumed },

            // ================================================================================
            // COUNTDOWN BRIDGE (mirror GameFlowAutoEventFlow)
            // ================================================================================
            { TempleRunEvents.CountdownStartRequested, TempleRunEvents.CountdownStarting },
            // CountdownStarting -> CountdownTick(s) -> CountdownEnding -> CountdownEnded: published elsewhere

            // ================================================================================
            // GAME START BRIDGE
            // ================================================================================
            { TempleRunEvents.TempleRunStartRequested, TempleRunEvents.TempleRunStarting },
            { TempleRunEvents.TempleRunStarting, TempleRunEvents.TempleRunStarted },

            // ================================================================================
            // GAME END BRIDGE
            // ================================================================================
            { TempleRunEvents.PlayerDied, TempleRunEvents.TempleRunEndRequested },
            { TempleRunEvents.TempleRunEndRequested, TempleRunEvents.TempleRunEnding },
            { TempleRunEvents.TempleRunEnding, TempleRunEvents.TempleRunEnded },

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
            // SlideStarted -> SlideEnded: Published by SlideArcController (when animation completes)

            // ================================================================================
            // DASH AUTO-CHAINS
            // ================================================================================
            // DashRequested -> DashStarting is NOT auto-chained. This mapping was previously live
            // and defeated the dash cooldown outright: DashRequested is the bridge's raw
            // translation of UserDashRequested, so DashStarting fired even when DashController had
            // rejected the request. DashController publishes DashStarting once its checks pass.
            // DashStarting -> DashStarted: Published by DashSpeedController (at animation start)
            // DashEnding -> DashEnded: Published by DashSpeedController (when dash completes)

            // ================================================================================
            // JUMP AUTO-CHAINS
            // ================================================================================
            // JumpRequested -> JumpStarting is NOT auto-chained. See the validation-gate note at
            // the top of this dictionary: chaining it would launch a second jump while one is
            // already in the air. JumpController publishes JumpStarting once its checks pass.
            // JumpStarting -> JumpStarted: Published by JumpArcController (at arc apex)
            // JumpStarted -> JumpLanded: Published by JumpArcController (when arc completes)

            // ================================================================================
            // OBSTACLE AUTO-CHAINS
            // ================================================================================
            // Gated by PowerUpBuffController for Shield support. See PowerUpBuffController.cs.
            // PowerUpBuffController subscribes to ObstacleHit and decides:
            //   Shield active  -> publishes ObstacleRecovered
            //   Shield inactive -> publishes PlayerFailingAtObstacle
            //{ TempleRunEvents.ObstacleHit, TempleRunEvents.PlayerFailingAtObstacle },

            // ================================================================================
            // COIN COLLECTION AUTO-CHAINS
            // ================================================================================
            { TempleRunEvents.CoinCollectRequested, TempleRunEvents.CoinCollecting },
            // CoinCollecting -> CoinCollected: Published by CoinCollectionController

            // ================================================================================
            // POWER-UP COLLECTION AUTO-CHAINS
            // ================================================================================
            { TempleRunEvents.PowerUpCollectRequested, TempleRunEvents.PowerUpCollecting },
            // PowerUpCollecting -> PowerUpCollected: Published by PowerUpBuffController (destroys GO, confirms pickup)
            { TempleRunEvents.PowerUpCollected, TempleRunEvents.PowerUpActivateRequested },
            { TempleRunEvents.PowerUpActivateRequested, TempleRunEvents.PowerUpActivating },
            // PowerUpActivating -> PowerUpActivated: Published by PowerUpBuffController (after buff applied)
            // PowerUpDeactivateRequested: Published by PowerUpBuffController (after timer expires)
            { TempleRunEvents.PowerUpDeactivateRequested, TempleRunEvents.PowerUpDeactivating },
            // PowerUpDeactivating -> PowerUpDeactivated: Published by PowerUpBuffController (after buff removed)
        };

        protected virtual void Awake()
        {
            TempleRunBus.SubscribeToAll(AutoFireTempleRunEventFromTempleRunEvent);
        }

        protected virtual void OnDestroy()
        {
            TempleRunBus.UnsubscribeFromAll(AutoFireTempleRunEventFromTempleRunEvent);
        }

        private void AutoFireTempleRunEventFromTempleRunEvent(string eventName, object sender, object data)
        {
            if (!TempleRunBus.TryGetEnum(eventName, out TempleRunEvents templeRunEvent)) return;
            if (_autoTempleRun2TempleRunEvents.TryGetValue(templeRunEvent, out TempleRunEvents autoEvent))
            {
                TempleRunBus.Publish(autoEvent, sender, data);
            }
        }
    }
}