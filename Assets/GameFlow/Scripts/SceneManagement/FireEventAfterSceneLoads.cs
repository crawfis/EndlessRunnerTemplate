using CrawfisSoftware.Events;

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrawfisSoftware.GameFlow.SceneManagement
{
    public class FireEventAfterSceneLoads : MonoBehaviour
    {
        [SerializeField] private List<string> _scenesNeeded;
        [SerializeField] private string _eventToFire;
        [SerializeField] private string _resetOnEvent = null;

        private List<string> _scenesLoaded;

        private void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            _scenesLoaded = new List<string>(_scenesNeeded);
            if(!string.IsNullOrEmpty(_resetOnEvent))
            {
                EventsPublisher.Instance.SubscribeToEvent(_resetOnEvent, OnResetEvent);
            }
        }
        private void OnDestroy()
        {
            EventsPublisher.Instance.UnsubscribeToEvent(_resetOnEvent, OnResetEvent);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var sceneName = scene.name;
            if(_scenesLoaded.Contains(sceneName))
            {
                _scenesLoaded.Remove(sceneName);
                if (_scenesLoaded.Count == 0)
                {
                    EventsPublisher.Instance.PublishEvent(_eventToFire, this, null);
                }
            }
        }

        private void OnResetEvent(string eventName, object sender, object data)
        {
            Reset();
        }

        private void Reset()
        {
            _scenesLoaded = new List<string>(_scenesNeeded);
        }
    }
}