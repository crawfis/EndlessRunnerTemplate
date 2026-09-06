using CrawfisSoftware.Events;
using CrawfisSoftware.Utility;

using System;
using System.Collections.Generic;
using System.Text;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrawfisSoftware.GameFlow.SceneManagement
{
    class LoadSceneAfterGameControlEvent : MonoBehaviour
    {
        [SerializeField] private string _sceneName;
        [SerializeField] private bool _loadadditively = true;
        [EventName][SerializeField] private string _eventToListenTo;
        [SerializeField] private int _delayInSeconds = 0;

        private void Start()
        {
            // Todo: Move to Enum-based events later. Will need to create a new class for each enum.
            EventsPublisher.Instance.SubscribeToEvent(_eventToListenTo, OnEventFired);
        }
        private void OnDestroy()
        {
            EventsPublisher.Instance.UnsubscribeToEvent(_eventToListenTo, OnEventFired);
        }
        private void OnEventFired(string eventName, object sender, object data)
        {
            if (_delayInSeconds <= 0f)
                SceneManager.LoadSceneAsync(_sceneName, _loadadditively ? LoadSceneMode.Additive : LoadSceneMode.Single);
            else
                _ = DelayedLoadScene();
        }

        private async Awaitable DelayedLoadScene()
        {
            await Wait.ForSecondsRealtime(_delayInSeconds, destroyCancellationToken);
            // Fire and forget, as the undelayed branch is: nothing here waits for the load.
            _ = SceneManager.LoadSceneAsync(_sceneName, _loadadditively ? LoadSceneMode.Additive : LoadSceneMode.Single);
        }
    }
}