using System;
using System.Collections.Generic;

namespace CrawfisSoftware.AssetManagement
{
    internal class EventsPublisherInternal : IEventsPublisher
    {
        // Define the events that occur in the game
        private readonly Dictionary<string, Action<object, object>> events = new Dictionary<string, Action<object, object>>();
        private readonly List<Action<string, object, object>> allSubscribers = new List<Action<string, object, object>>();

        public void RegisterEvent(string eventName)
        {
            if (!events.ContainsKey(eventName))
            {
                events.Add(eventName, NullCallback);
            }
        }
        public void SubscribeToEvent(string eventName, Action<object, object> callback)
        {
            RegisterEvent(eventName);
            if (events.ContainsKey(eventName))
                events[eventName] += callback;
        }

        public void UnsubscribeToEvent(string eventName, Action<object, object> callback)
        {
            if (events.ContainsKey(eventName))
                events[eventName] -= callback;
        }

        public void SubscribeToAllEvents(Action<string, object, object> callback)
        {
            allSubscribers.Add(callback);
        }

        public void UnsubscribeToAllEvents(Action<string, object, object> callback)
        {
            allSubscribers.Remove(callback);
        }

        public void PublishEvent(string eventName, object sender, object data)
        {
            if (events.TryGetValue(eventName, out Action<object, object> eventDelegate))
            {
                //eventDelegate(sender, data);
                var callbacks = eventDelegate.GetInvocationList();
                foreach (var callback in callbacks)
                    try
                    {
                        callback.DynamicInvoke(sender, data);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError($"Exception publishing {eventName}: {callback.Target} {e.InnerException.Message} {e.InnerException.StackTrace} {e.InnerException.Source}");
                    }
            }
            foreach (var handler in allSubscribers)
                handler(eventName, sender, data);
        }

        public IEnumerable<string> GetRegisteredEvents()
        {
            return events.Keys;
        }

        private void NullCallback(object sender, object data)
        {
        }

        public void Clear()
        {
            events.Clear();
            allSubscribers.Clear();
        }

        void IEventsPublisher.Push()
        {
        }

        IEventsPublisher IEventsPublisher.Pop()
        {
            return this;
        }
    }

}