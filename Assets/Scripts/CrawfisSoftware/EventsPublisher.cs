using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrawfisSoftware.AssetManagement
{
    public class EventsPublisher : IEventsPublisher
    {
        public static IEventsPublisher Instance { get; private set; }

        static EventsPublisher()
        {
            Instance = new EventsPublisher();
            Instance.Push();
        }
        private EventsPublisher() { }

        private readonly Stack<IEventsPublisher> _eventsPublishers = new Stack<IEventsPublisher>();

        public void Push()
        {
            IEventsPublisher eventsPublisher = new EventsPublisherInternal();
            _eventsPublishers.Push(eventsPublisher);
        }

        public IEventsPublisher Pop()
        {
            return _eventsPublishers.Pop();
        }

        public IEnumerable<string> GetRegisteredEvents()
        {
            foreach (var publisher in _eventsPublishers)
            {
                foreach (string eventName in publisher.GetRegisteredEvents()) { yield return eventName; }
            }
        }

        public void PublishEvent(string eventName, object sender, object data)
        {
            foreach (IEventsPublisher publisher in _eventsPublishers) { publisher.PublishEvent(eventName, sender, data); }
        }

        public void RegisterEvent(string eventName)
        {
            _eventsPublishers.Peek().RegisterEvent(eventName);
        }

        public void SubscribeToAllEvents(Action<string, object, object> callback)
        {
            _eventsPublishers.Peek().SubscribeToAllEvents(callback);
        }

        public void SubscribeToEvent(string eventName, Action<object, object> callback)
        {
            _eventsPublishers.Peek().SubscribeToEvent(eventName, callback);
        }

        public void UnsubscribeToAllEvents(Action<string, object, object> callback)
        {
            _eventsPublishers.Peek().UnsubscribeToAllEvents(callback);
        }

        public void UnsubscribeToEvent(string eventName, Action<object, object> callback)
        {
            _eventsPublishers.Peek().UnsubscribeToEvent(eventName, callback);
        }

        public void Clear()
        {
            foreach (IEventsPublisher publisher in _eventsPublishers)
            {
                publisher.Clear();
            }
        }

        [RuntimeInitializeOnLoadMethod]
        public static void ResetOnPlayMode()
        {
            Instance?.Clear();
            //Instance = null;
        }
    }
}