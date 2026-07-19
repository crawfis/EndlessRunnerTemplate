using CrawfisSoftware.Events;

using System;
using System.Collections.Generic;

using UnityEngine;

namespace CrawfisSoftware.TempleRun.Events
{
    internal class Input2TempleRunAutoEventBridge : MonoBehaviour
    {
        private Dictionary<UserInitiatedEvents, TempleRunEvents> _autoUserInitiated2TempleRunEvents = new Dictionary<UserInitiatedEvents, TempleRunEvents>()
        {
            // User input bridges: raw input events -> gameplay events
            // This allows gameplay mechanics to be triggered from any source (player input, AI, replay, network)
            // Controllers subscribe to TempleRun domain events, not UserInitiated events
            { UserInitiatedEvents.UserQuitRequested, TempleRunEvents.TempleRunEndRequested },
            { UserInitiatedEvents.UserSlideRequested, TempleRunEvents.SlideRequested },
            { UserInitiatedEvents.UserDashRequested, TempleRunEvents.DashRequested },
        };

        protected virtual void Awake()
        {
            EventsPublisherUserInitiated.Instance.SubscribeToAllEnumEvents(AutoFireTempleRunEventFromUserInitiatedEvent);
        }

        protected virtual void OnDestroy()
        {
            EventsPublisherUserInitiated.Instance.UnsubscribeToAllEnumEvents(AutoFireTempleRunEventFromUserInitiatedEvent);
        }

        private void AutoFireTempleRunEventFromUserInitiatedEvent(string eventName, object sender, object data)
        {
            ReadOnlySpan<char> input = eventName.AsSpan();
            int index = input.LastIndexOf('/');
            if (index < 0) return;
            string result = input.Slice(index + 1).ToString();
            UserInitiatedEvents userInitiatedEvent = Enum.Parse<UserInitiatedEvents>(result);
            if (_autoUserInitiated2TempleRunEvents.TryGetValue(userInitiatedEvent, out TempleRunEvents autoEvent))
            {
                EventsPublisherTempleRun.Instance.PublishEvent(autoEvent, sender, data);
            }
        }
    }
}