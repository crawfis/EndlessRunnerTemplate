using CrawfisSoftware.Events;
using CrawfisSoftware.GameFlow.Events;

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;
using GameFlowBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.GameFlow.Events.GameFlowEvents>;

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
            GameFlowBus.Subscribe(_unloadScenesTriggerEvent, OnGameOver);
        }
        private void OnDestroy()
        {
            GameFlowBus.Unsubscribe(_unloadScenesTriggerEvent, OnGameOver);
        }

        private void OnGameOver(string eventName, object sender, object data)
        {
            if (_unsubscribeOnEvent) 
            {
                GameFlowBus.Unsubscribe(_unloadScenesTriggerEvent, OnGameOver);
            }
            _ = UnloadScenesAsync();
        }

        private async Awaitable UnloadScenesAsync()
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
            // Wait for all unloads to finish. FromAsyncOperation rather than a bare `await op`
            // so this await carries a token like every other one in the project.
            foreach (var op in unloadOperations)
                await Awaitable.FromAsyncOperation(op, destroyCancellationToken);
            GameFlowBus.Publish(_scenesUnloadedEvent, this, null);
        }
    }
}