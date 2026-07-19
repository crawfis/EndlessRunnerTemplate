using CrawfisSoftware.Events;
using CrawfisSoftware.GameFlow.Events;
using CrawfisSoftware.GameFlow.GameConfig;

using System.Collections;

using UnityEngine;

namespace CrawfisSoftware.GameFlow
{
    /// <summary>
    /// Handles quitting.
    ///    Dependency: GameConstants
    ///    Subscribes: GameFlowEvents.Quitting - Currently it quits the application.
    /// </summary>
    public class QuitController : MonoBehaviour
    {
        private void Start()
        {
            EventsPublisherGameFlow.Instance.SubscribeToEvent(GameFlowEvents.Quitting, OnQuitted);
        }

        private void OnQuitted(string EventName, object sender, object data)
        {
            EventsPublisherGameFlow.Instance.UnsubscribeToEvent(GameFlowEvents.Quitting, OnQuitted);
            StartCoroutine(Quit());
        }
        private IEnumerator Quit()
        {
            yield return new WaitForSecondsRealtime(GameConstants.QuitDelay);

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
            EventsPublisher.Instance.Clear();
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}