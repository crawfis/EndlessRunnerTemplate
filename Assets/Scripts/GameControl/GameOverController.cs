using CrawfisSoftware.Events;

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Handles quitting.
    ///    Dependency: EventsPublisherTempleRun
    ///    Subscribes: GameOver - Currently it quits the application.
    /// </summary>
    public class GameOverController : MonoBehaviour
    {
        private void Start()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.GameOver, OnGameOver);
        }

        private void OnGameOver(string EventName, object sender, object data)
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.GameOver, OnGameOver);
            StartCoroutine(Quit());
        }
        private IEnumerator Quit()
        {
            yield return new WaitForSecondsRealtime(GameConstants.QuitDelay);

            // Unload all scenes except for scene 0
            List<AsyncOperation> unloadOperations = new List<AsyncOperation>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.buildIndex != 0 && scene.isLoaded)
                {
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

            // This shows the proper way to quit a game both in Editor and with a build
#if UNITY_EDITOR
            EventsPublisher publisher = (EventsPublisher)(EventsPublisher.Instance);
            foreach ((string eventName, string targetName) subscriberData in publisher.GetSubscribers())
            {
                Debug.LogWarning($"{subscriberData.targetName} did not unsubscribe {subscriberData.eventName}.");
            }
            // Needed in Unity editor to clear any subscribers who forgot to unsubscribe.
            // Best to unsubscribe in the OnDestroy method of the subscriber.
            //If lazy, uncomment the next line to clear all subscribers.
            //EventsPublisher.Instance.Clear();
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            Application.Quit();
        }
    }
}
