using CrawfisSoftware.Events;

using UnityEngine;

namespace CrawfisSoftware.GameFlow.SceneManagement
{
    class FireEventWhenSceneCloses : MonoBehaviour
    {
        [SerializeField] private string _sceneName;
        [SerializeField] private string _eventName;
        private void OnEnable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;
        }
        private void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }
        private void OnSceneUnloaded(UnityEngine.SceneManagement.Scene scene)
        {
            if(scene.name == _sceneName)
            {
                EventsPublisher.Instance.PublishEvent(_eventName, this, _sceneName);
            }
        }
    }
}