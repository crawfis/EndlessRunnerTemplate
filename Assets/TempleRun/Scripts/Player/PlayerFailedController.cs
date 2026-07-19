using CrawfisSoftware.TempleRun.GameConfig;

using System.Collections;

using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Simple behavior for failure. In this case, pauses the game for a fixed time and then resumes.
    ///    Dependencies: TempleRunConstants, EventsPublisherTempleRun
    ///    Subscribes: TempleRunEvents.PlayerFailingAtTurn
    ///    Subscribes: TempleRunEvents.PlayerFailingAtObstacle
    ///    Publishes: TempleRunEvents.PlayerPaused (pause the game)
    ///    Publishes: TempleRunEvents.PlayerResumeRequested (resume after delay)
    /// </summary>
    internal class PlayerFailedController : MonoBehaviour
    {
        private Coroutine _pauseCoroutine;

        private void Awake()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.PlayerFailingAtTurn, OnPlayerFailing);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.PlayerFailingAtObstacle, OnPlayerFailing);
        }

        private void OnPlayerFailing(string eventName, object sender, object data)
        {
            // Guard: ignore if already paused (e.g., turn failure + obstacle hit in same frame)
            if (_pauseCoroutine != null) return;
            _pauseCoroutine = StartCoroutine(DeathDelay());
        }
        private IEnumerator DeathDelay()
        {
            EventsPublisherTempleRun.Instance.PublishEvent(TempleRunEvents.PlayerPaused, this, UnityEngine.Time.time);
            yield return new WaitForSecondsRealtime(TempleRunConstants.ResumeDelay);
            _pauseCoroutine = null;
            EventsPublisherTempleRun.Instance.PublishEvent(TempleRunEvents.PlayerResumeRequested, this, UnityEngine.Time.time);
        }

        private void OnDestroy()
        {
            StopAllCoroutines(); // Saved them so could call individually instead.
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.PlayerFailingAtTurn, OnPlayerFailing);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.PlayerFailingAtObstacle, OnPlayerFailing);
        }
    }
}