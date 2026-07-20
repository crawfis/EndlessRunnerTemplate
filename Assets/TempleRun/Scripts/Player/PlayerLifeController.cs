using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Manages the number of lives a player has, converting the PlayerFailing events to a PlayerDied event when
    /// all of the lives run out.
    ///    Dependencies: Blackboard, EventsPublisherTempleRun
    ///    Subscribes: TempleRunEvents.PlayerFailingAtTurn
    ///    Subscribes: TempleRunEvents.PlayerFailingAtObstacle
    ///    Subscribes: TempleRunEvents.TempleRunStarted (resets lives at game start)
    ///    Publishes: TempleRunEvents.PlayerDied — Data is the final score (float).
    ///    Publishes: TempleRunEvents.PlayerResumed — Unpauses after death so the game-ending flow can proceed.
    /// </summary>
    /// <remarks>For local multi-player we may need a player ID. Would be good to include this in the event data.</remarks>
    internal class PlayerLifeController : MonoBehaviour
    {
        [SerializeField] private int _playerID = 0;

        private int _numberOfLives;

        private void Awake()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.PlayerFailingAtTurn, OnPlayerFailed);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.PlayerFailingAtObstacle, OnPlayerFailed);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.TempleRunStarted, OnGameStarted);
        }

        private void OnGameStarted(string eventName, object sender, object data)
        {
            _numberOfLives = Blackboard.Instance.GameConfig.NumberOfLives;
            Debug.Log($"PlayerLifeController: Lives reset to {_numberOfLives}");
        }

        private void OnPlayerFailed(string eventName, object sender, object data)
        {
            // Todo: Check playerID
            _numberOfLives--;
            Debug.Log($"PlayerLifeController: Life lost. Remaining: {_numberOfLives}");
            if (_numberOfLives <= 0)
            {
                float score = Blackboard.Instance.DistanceTracker.DistanceTravelled;
                EventsPublisherTempleRun.Instance.PublishEvent(TempleRunEvents.PlayerDied, this, score);
                EventsPublisherTempleRun.Instance.PublishEvent(TempleRunEvents.PlayerResumed, this, Time.time);
            }
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.PlayerFailingAtTurn, OnPlayerFailed);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.PlayerFailingAtObstacle, OnPlayerFailed);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.TempleRunStarted, OnGameStarted);
        }
    }
}
