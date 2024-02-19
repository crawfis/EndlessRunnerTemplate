using System;
using UnityEngine;

namespace CrawfisSoftware.AssetManagement
{
    public class EventsPublisherEnums<T> : MonoBehaviour where T : Enum
    {
        public static EventsPublisherEnums<T> Instance { get; private set; }
        // Todo: May want to add the nameof(T) to the string to avoid conflicts with several enums.
        //    This may lead to user errors defining the same event in two Enums though and provide greater confusion
        //public static Dictionary<KnownEvents, string> KnownEventsMap { get; private set; } = new();

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

        public void PublishEvent(T eventEnum, object sender, object data)
        {
            EventsPublisher.Instance.PublishEvent(eventEnum.ToString(), sender, data);
        }

        public void SubscribeToEvent(T eventEnum, Action<object, object> callback)
        {
            EventsPublisher.Instance.SubscribeToEvent(eventEnum.ToString(), callback);
        }

        public void UnsubscribeToEvent(T eventEnum, Action<object, object> callback)
        {
            EventsPublisher.Instance.UnsubscribeToEvent(eventEnum.ToString(), callback);
        }

        private static void RegisterKnownEvents()
        {
            foreach (T eventEnum in Enum.GetValues(typeof(T)))
            {
                string eventName = eventEnum.ToString();
                //KnownEventsMap.Add(eventEnum, eventName);
                EventsPublisher.Instance.RegisterEvent(eventName);
            }
        }

        [RuntimeInitializeOnLoadMethod]
        public static void ResetOnPlayMode()
        {
            Instance = null;
        }
    }
}