using System.Collections;
using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Simple behavior for failure. In this case, pauses the game, advances to the next track and then resumes.
    ///    Dependency: TrackManager, EventsPublisherTempleRun
    ///    Subscribes: PlayerFailed - pauses game for a fixed time and then resumes.
    ///    Publishes: Pause and Resume
    /// </summary>
    internal class PlayerFailedController : MonoBehaviour
    {
        [SerializeField] private TrackManager _trackManager;

        private Coroutine _pauseCoroutine;
        private Coroutine _advanceTrackCoroutine;

        private void Awake()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.PlayerFailed, OnPlayerFailed);
        }

        private void OnPlayerFailed(object sender, object data)
        {
            _pauseCoroutine = StartCoroutine(PauseGame());
            // Note: This starts immediately and runs in parallel with the above coroutine.
            _advanceTrackCoroutine = StartCoroutine(AdvanceTrack());
        }
        private IEnumerator PauseGame()
        {
            EventsPublisherTempleRun.Instance.PublishEvent(KnownEvents.Pause, this, null);
            yield return new WaitForSecondsRealtime(GameConstants.ResumeDelay);
            EventsPublisherTempleRun.Instance.PublishEvent(KnownEvents.Resume, this, null);
        }

        private IEnumerator AdvanceTrack()
        {
            // Wait until pause is almost over before advancing the player to the next track segment.
            yield return new WaitForSecondsRealtime(GameConstants.ZeroHoldForResumeDelay);
            _trackManager.AdvanceToNextSegment();
        }

        private void OnDestroy()
        {
            StopAllCoroutines(); // Saved them so could call individually instead.
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.PlayerFailed, OnPlayerFailed);
        }
    }
}