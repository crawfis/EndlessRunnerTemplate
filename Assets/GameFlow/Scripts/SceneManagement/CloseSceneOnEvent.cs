using CrawfisSoftware.Events;

using UnityEngine;

namespace CrawfisSoftware.GameFlow.SceneManagement
{
    class CloseSceneOnEvent : MonoBehaviour
    {
        [SerializeField] private string _eventToSubscribeTo;
        [SerializeField] private bool _unsubscribeOnEvent = true;

        private void Awake()
        {
            EventsPublisher.Instance.SubscribeToEvent(_eventToSubscribeTo, OnEventReceived);
        }
        private void OnDestroy()
        {
            EventsPublisher.Instance.UnsubscribeToEvent(_eventToSubscribeTo, OnEventReceived);
        }

        private void OnEventReceived(string eventName, object sender, object data)
        {
            if(_unsubscribeOnEvent)
                EventsPublisher.Instance.UnsubscribeToEvent(_eventToSubscribeTo, OnEventReceived);
            UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(this.gameObject.scene);
        }
    }
}