using CrawfisSoftware.AssetManagement;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Singleton event publisher that interfaces to the CrawfisSoftware.AssetManagement.EventsPublisher singleton.
    /// Avoids the problem with strings and misspelling when dealing with the EventsPublisher. Several of these could be used with
    /// different enum types for more modularity.
    /// </summary>
    internal class EventsPublisherTempleRun : MonoBehaviour
    {
        public static EventsPublisherTempleRun Instance { get; private set; }
        public static Dictionary<KnownEvents, string> KnownEventsMap { get; private set; } = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            RegisterKnownEvents();
        }

        public void PublishEvent(KnownEvents eventEnum, object sender, object data)
        {
            EventsPublisher.Instance.PublishEvent(eventEnum.ToString(), sender, data);
        }

        public void SubscribeToEvent(KnownEvents eventEnum, Action<object, object> callback)
        {
            EventsPublisher.Instance.SubscribeToEvent(eventEnum.ToString(), callback);
        }

        public void UnsubscribeToEvent(KnownEvents eventEnum, Action<object, object> callback)
        {
            EventsPublisher.Instance.UnsubscribeToEvent(eventEnum.ToString(), callback);
        }

        private static void RegisterKnownEvents()
        {
            foreach (KnownEvents eventEnum in Enum.GetValues(typeof(KnownEvents)))
            {
                string eventName = eventEnum.ToString();
                KnownEventsMap.Add(eventEnum, eventName);
                EventsPublisher.Instance.RegisterEvent(eventName);
            }
        }
    }
}