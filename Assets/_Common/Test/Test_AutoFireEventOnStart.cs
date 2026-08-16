using CrawfisSoftware.Events;

using UnityEngine;

namespace CrawfisSoftware.Test
{
    public class Test_AutoFireEventOnStart : MonoBehaviour
    {
        [EventName][SerializeField] private string _eventName = "GameFlowEvents/GameplayReady";
        void Start()
        {
            EventsPublisher.Instance.PublishEvent(_eventName, this, null);
        }
    }
}