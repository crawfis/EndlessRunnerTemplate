using CrawfisSoftware.TempleRun.GameConfig;

using System.Collections;

using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Handles auto-turning after a failure that originates from reaching the end of a track segment.
    ///    Dependencies: TurnController, EventsPublisherTempleRun
    ///    Subscribes: PlayerFailing
    ///    Publishes: Turn completed events via TurnController
    /// </summary>
    internal class PlayerFailureAutoTurnController : MonoBehaviour
    {
        [SerializeField] private TurnController _turnController;
        private Coroutine _advanceTrackCoroutine;

        private void Awake()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.PlayerFailingAtTurn, OnPlayerFailing);
        }

        private void OnPlayerFailing(string eventName, object sender, object data)
        {
            if (data is float)
            {
                // Note: This starts immediately and runs in parallel with pause behavior.
                _advanceTrackCoroutine = StartCoroutine(AdvanceAfterFailure());
            }
        }

        private IEnumerator AdvanceAfterFailure()
        {
            // Wait until pause is almost over before advancing the player to the next track segment.
            yield return new WaitForSecondsRealtime(TempleRunConstants.DelayAfterFailureBeforeAutoTurning);
            _turnController.ForceTurn();
        }

        private void OnDestroy()
        {
            StopAllCoroutines(); // Saved them so could call individually instead.
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.PlayerFailingAtTurn, OnPlayerFailing);
        }
    }
}
