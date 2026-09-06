using CrawfisSoftware.Events;

using UnityEngine;

namespace CrawfisSoftware.Utility
{
    class TimedEvent : MonoBehaviour
    {
        [SerializeField] private float _delayInSeconds = 1.0f;
        [EventName][SerializeField] private string _eventName = "ERROR";
        [SerializeField] private bool _useRealtime = true;

        private void Start()
        {
            _ = FireEvent();
        }
        private async Awaitable FireEvent()
        {
            if (_useRealtime)
            {
                await Wait.ForSecondsRealtime(_delayInSeconds, destroyCancellationToken);
            }
            else
            {
                await Awaitable.WaitForSecondsAsync(_delayInSeconds, destroyCancellationToken);
            }
            EventsPublisher.Instance.PublishEvent(_eventName, this, null);
        }
    }
}