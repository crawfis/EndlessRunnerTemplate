using CrawfisSoftware.Events;

using System;
using System.Collections;
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
        [SerializeField] private string _eventToListenTo;
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
                StartCoroutine(DelayedLoadScene());
        }

        private IEnumerator DelayedLoadScene()
        {
            yield return new WaitForSecondsRealtime(_delayInSeconds);
            SceneManager.LoadSceneAsync(_sceneName, _loadadditively ? LoadSceneMode.Additive : LoadSceneMode.Single);
        }
    }
}