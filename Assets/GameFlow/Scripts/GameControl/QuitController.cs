using CrawfisSoftware.Events;
using CrawfisSoftware.GameFlow.Events;
using CrawfisSoftware.GameFlow.GameConfig;
using CrawfisSoftware.Utility;

using UnityEngine;
using GameFlowBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.GameFlow.Events.GameFlowEvents>;

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
            GameFlowBus.Subscribe(GameFlowEvents.Quitting, OnQuitted);
        }

        private void OnQuitted(string EventName, object sender, object data)
        {
            GameFlowBus.Unsubscribe(GameFlowEvents.Quitting, OnQuitted);
            _ = Quit();
        }
        private async Awaitable Quit()
        {
            await Wait.ForSecondsRealtime(GameConstants.QuitDelay, destroyCancellationToken);

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