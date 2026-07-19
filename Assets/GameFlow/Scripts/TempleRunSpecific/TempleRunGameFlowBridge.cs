using CrawfisSoftware.TempleRun;
using CrawfisSoftware.UGS;
using CrawfisSoftware.UGS.Events;

using System;
using System.Collections.Generic;

using UnityEngine;

namespace CrawfisSoftware.GameFlow.Events
{
    internal class TempleRunGameFlowBridge : MonoBehaviour
    {
        private Dictionary<TempleRunEvents, GameFlowEvents> _autoTempleRun2GameFlowEvents = new Dictionary<TempleRunEvents, GameFlowEvents>()
        {
            // TempleRun paused -> request GameFlow pause (for menus/UI)
            { TempleRunEvents.PlayerPaused, GameFlowEvents.PauseRequested },

            // Countdown ended -> game officially started (absorbed from GameController)
            { TempleRunEvents.CountdownEnded, GameFlowEvents.GameStarted },

            // Player died -> game ending (absorbed from GameController)
            { TempleRunEvents.TempleRunEnded, GameFlowEvents.GameEnding },
        };

        private Dictionary<GameFlowEvents, TempleRunEvents> _autoGameFlow2TempleRunEvents = new Dictionary<GameFlowEvents, TempleRunEvents>()
        {
            // Bridge start: when the broader game signals started, fire TempleRun start requested
            { GameFlowEvents.GameStarted, TempleRunEvents.TempleRunStartRequested },

            // GameFlow starting -> kick off countdown in TempleRun
            { GameFlowEvents.GameStarting, TempleRunEvents.CountdownStartRequested },

            // Config/scenes bridged to TempleRun domain
            { GameFlowEvents.GameConfigApplied, TempleRunEvents.TempleRunConfigApplied },
            { GameFlowEvents.TrackConfigApplied, TempleRunEvents.TempleRunTrackConfigApplied },
            { GameFlowEvents.GameScenesLoaded, TempleRunEvents.TempleRunScenesReady },
        };

        // TempleRun → UGS passthrough (bypasses GameFlow — this bridge is the authorized crossing point)
        private Dictionary<TempleRunEvents, UGS_EventsEnum> _autoTempleRun2UGSEvents = new Dictionary<TempleRunEvents, UGS_EventsEnum>()
        {
            // Distance updates for achievement tracking
            { TempleRunEvents.DistanceUpdated, UGS_EventsEnum.UGS_DistanceUpdated },

            // Coin collection for economy sync and achievement tracking
            { TempleRunEvents.CoinCollected, UGS_EventsEnum.UGS_CoinUpdated },
        };

        protected virtual void Awake()
        {
            EventsPublisherTempleRun.Instance.SubscribeToAllEnumEvents(AutoFireGameFlowEventFromTempleRunEvent);
            EventsPublisherGameFlow.Instance.SubscribeToAllEnumEvents(AutoFireTempleRunEventFromGameFlowEvent);
            EventsPublisherTempleRun.Instance.SubscribeToAllEnumEvents(AutoFireUGSEventFromTempleRunEvent);
        }

        protected virtual void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToAllEnumEvents(AutoFireGameFlowEventFromTempleRunEvent);
            EventsPublisherGameFlow.Instance.UnsubscribeToAllEnumEvents(AutoFireTempleRunEventFromGameFlowEvent);
            EventsPublisherTempleRun.Instance.UnsubscribeToAllEnumEvents(AutoFireUGSEventFromTempleRunEvent);
        }

        private void AutoFireGameFlowEventFromTempleRunEvent(string eventName, object sender, object data)
        {
            ReadOnlySpan<char> input = eventName.AsSpan();
            int index = input.LastIndexOf('/');
            if (index < 0) return;
            string result = input.Slice(index + 1).ToString();
            TempleRunEvents templeRunEvent = (TempleRunEvents)Enum.Parse(typeof(TempleRunEvents), result);
            if (_autoTempleRun2GameFlowEvents.TryGetValue(templeRunEvent, out GameFlowEvents autoEvent))
            {
                EventsPublisherGameFlow.Instance.PublishEvent(autoEvent, sender, data);
            }
        }

        private void AutoFireUGSEventFromTempleRunEvent(string eventName, object sender, object data)
        {
            ReadOnlySpan<char> input = eventName.AsSpan();
            int index = input.LastIndexOf('/');
            if (index < 0) return;
            string result = input.Slice(index + 1).ToString();
            TempleRunEvents templeRunEvent = (TempleRunEvents)Enum.Parse(typeof(TempleRunEvents), result);
            if (_autoTempleRun2UGSEvents.TryGetValue(templeRunEvent, out UGS_EventsEnum autoEvent))
            {
                EventsPublisherUGS.Instance.PublishEvent(autoEvent, sender, data);
            }
        }

        private void AutoFireTempleRunEventFromGameFlowEvent(string eventName, object sender, object data)
        {
            ReadOnlySpan<char> input = eventName.AsSpan();
            int index = input.LastIndexOf('/');
            if (index < 0) return;
            string result = input.Slice(index + 1).ToString();
            GameFlowEvents gameflowEvent = (GameFlowEvents)Enum.Parse(typeof(GameFlowEvents), result);
            if (_autoGameFlow2TempleRunEvents.TryGetValue(gameflowEvent, out TempleRunEvents autoEvent))
            {
                EventsPublisherTempleRun.Instance.PublishEvent(autoEvent, this, data);
            }
        }
    }
}