using System.Collections.Generic;
using System.Text;

using UnityEngine;

namespace CrawfisSoftware.Events
{
    internal class EventHistory : MonoBehaviour
    {
        [SerializeField] private List<string> _eventsInterestedIn = new List<string>();
        [SerializeField][TextArea(5, 100)] private string _events;
        private StringBuilder _eventsBuilder = new StringBuilder();
        private HashSet<string> _interestedEventsHashSet = new HashSet<string>();

        private void Awake()
        {
            EventsPublisher.Instance.SubscribeToAllEvents(OnEventPublished);
            foreach (string eventName in _eventsInterestedIn)
            {
                _interestedEventsHashSet.Add(eventName);
            }
        }

        private void OnDestroy()
        {
            EventsPublisher.Instance.UnsubscribeToAllEvents(OnEventPublished);
        }

        private void OnEventPublished(string eventName, object sender, object data)
        {
            if (!_interestedEventsHashSet.Contains(eventName)) return;
            _eventsBuilder.AppendLine($"{eventName}: {data?.ToString()}");
            _events = _eventsBuilder.ToString();
        }
    }
}
