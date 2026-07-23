using System;
using System.Collections.Generic;

using UnityEngine;

namespace CrawfisSoftware.TempleRun.Events
{
    /// <summary>
    /// Auto-chain TempleRun-specific events. Keep this focused on TempleRun internal lifecycles;
    /// cross-system bridges live in TempleRunGameFlowBridge.
    /// </summary>
    internal class TempleRunAutoEventFlow : MonoBehaviour
    {
        private readonly Dictionary<TempleRunEvents, TempleRunEvents> _autoTempleRun2TempleRunEvents = new Dictionary<TempleRunEvents, TempleRunEvents>()
        {
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
            { TempleRunEvents.LaneChangeLeftRequested, TempleRunEvents.LaneChangingLeft },
            { TempleRunEvents.LaneChangeRightRequested, TempleRunEvents.LaneChangingRight },
            // LaneChangingLeft -> LaneChangedLeft: Published by LaneOffsetController (after lerp completes)
            // LaneChangingRight -> LaneChangedRight: Published by LaneOffsetController (after lerp completes)

            // ================================================================================
            // SLIDE AUTO-CHAINS
            // ================================================================================
            //{ TempleRunEvents.SlideRequested, TempleRunEvents.SlideStarting },
            // SlideStarting -> SlideStarted: Published by SlideController (when slide starts)
            // SlideEnding -> SlideEnded: Published by SlideController (when slide completes)

            // ================================================================================
            // DASH AUTO-CHAINS
            // ================================================================================
            { TempleRunEvents.DashRequested, TempleRunEvents.DashStarting },
            // DashStarting -> DashStarted: Published by DashController (when dash initiates)
            // DashEnding -> DashEnded: Published by DashController (when dash completes)

            // ================================================================================
            // JUMP AUTO-CHAINS
            // ================================================================================
            { TempleRunEvents.JumpRequested, TempleRunEvents.JumpStarting },
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
            EventsPublisherTempleRun.Instance.SubscribeToAllEnumEvents(AutoFireTempleRunEventFromTempleRunEvent);
        }

        protected virtual void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToAllEnumEvents(AutoFireTempleRunEventFromTempleRunEvent);
        }

        private void AutoFireTempleRunEventFromTempleRunEvent(string eventName, object sender, object data)
        {
            ReadOnlySpan<char> input = eventName.AsSpan();
            int index = input.LastIndexOf('/');
            if (index < 0) return;
            string result = input.Slice(index + 1).ToString();
            TempleRunEvents templeRunEvent = (TempleRunEvents)Enum.Parse(typeof(TempleRunEvents), result);
            if (_autoTempleRun2TempleRunEvents.TryGetValue(templeRunEvent, out TempleRunEvents autoEvent))
            {
                EventsPublisherTempleRun.Instance.PublishEvent(autoEvent, sender, data);
            }
        }
    }
}