using CrawfisSoftware.Events;
using CrawfisSoftware.GameFlow.Events;

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrawfisSoftware.GameFlow.GameControl
{
    public class UnloadNonActiveScenes : MonoBehaviour
    {
        [SerializeField] private int _lastSceneIndexToKeep = 0;
        [SerializeField] private GameFlowEvents _unloadScenesTriggerEvent = GameFlowEvents.GameEnded;
        [SerializeField] private GameFlowEvents _scenesUnloadedEvent = GameFlowEvents.GameScenesUnloaded;
        [SerializeField] private bool _unsubscribeOnEvent = true;

        private void Start()
        {
            EventsPublisherGameFlow.Instance.SubscribeToEvent(_unloadScenesTriggerEvent, OnGameOver);
        }
        private void OnDestroy()
        {
            EventsPublisherGameFlow.Instance.UnsubscribeToEvent(_unloadScenesTriggerEvent, OnGameOver);
        }

        private void OnGameOver(string eventName, object sender, object data)
        {
            if (_unsubscribeOnEvent) 
            {
                EventsPublisherGameFlow.Instance.UnsubscribeToEvent(_unloadScenesTriggerEvent, OnGameOver);
            }
            StartCoroutine(UnloadScenesAsync());
        }

        private IEnumerator UnloadScenesAsync()
        {
            // Unload all active scenes after scene _lastSceneToKeepIndex.
            // This does this in parallel and allows yielding until all are done.
            List<AsyncOperation> unloadOperations = new List<AsyncOperation>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.buildIndex > _lastSceneIndexToKeep && scene.isLoaded)
                {
                    //yield return SceneManager.UnloadSceneAsync(scene);  // This would do it one at a time.
                    AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(scene);
                    if (unloadOp != null)
                    {
                        unloadOperations.Add(unloadOp);
                    }
                }
            }
            // Wait for all unloads to finish
            foreach (var op in unloadOperations)
            {
                while (!op.isDone)
                {
                    yield return null;
                }
            }
            EventsPublisherGameFlow.Instance.PublishEvent(_scenesUnloadedEvent, this, null);
        }
    }
}