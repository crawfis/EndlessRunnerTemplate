using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Compares the distance from DistanceTracker to the current track segment length.
    /// Fires PlayerFailingAtTurn when the player exceeds a turn segment distance without turning.
    /// Straight segments are handled by SegmentAdvanceTrigger (SegmentExiting / SegmentExited).
    ///    Dependencies: Blackboard, DistanceTracker, EventsPublisherTempleRun
    ///    Subscribes: TempleRunEvents.ActiveTrackChanging — increases the active track length
    ///    Subscribes: TempleRunEvents.TempleRunStarted — begins distance checking
    ///    Subscribes: TempleRunEvents.PlayerDied — stops distance checking
    ///    Publishes: TempleRunEvents.PlayerFailingAtTurn — Data is the current player distance (float). Turn segments only.
    /// </summary>
    /// <remarks>For local multi-player we may need a player ID. Would be good to include this in the event data.</remarks>
    internal class TurnCollisionDetector : MonoBehaviour
    {

        private float _currentSegmentInitialDistance = 0f;
        private float _turnFailureDistance;
        private bool _isRunning = false;
        private bool _gameStarted = false;
        private bool _isCurrentSegmentStraight = false;

        private void Awake()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.ActiveTrackChanging, OnTrackChanging);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.TurnLeftCompleted, OnSuccessfullTurn);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.TurnRightCompleted, OnSuccessfullTurn);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.TempleRunStarted, OnGameStarted);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.PlayerDied, OnGameEnding);
        }

        private void Update()
        {
            // Only check turn segments — straight segments are handled by SegmentAdvanceTrigger.
            if (!_isRunning || !_gameStarted || _isCurrentSegmentStraight) return;

            float distance = Blackboard.Instance.DistanceTracker.DistanceTravelled;
            if (distance >= _turnFailureDistance)
            {
                _isRunning = false;
                Debug.LogWarning($"Player failed turn at distance: {distance}, should have turned before {_turnFailureDistance}");
                EventsPublisherTempleRun.Instance.PublishEvent(TempleRunEvents.PlayerFailingAtTurn, this, distance);
            }
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.ActiveTrackChanging, OnTrackChanging);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.TempleRunStarted, OnGameStarted);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.PlayerDied, OnGameEnding);
        }

        private void OnTrackChanging(string eventName, object sender, object data)
        {
            TrackSegmentInfo trackSegmentInfo = (TrackSegmentInfo)data;
            _isCurrentSegmentStraight = trackSegmentInfo.Direction == Direction.Straight;
            _isRunning = true;
            /// Bug: We need the TrackManager to tell us the Distance this section is anchored to. This will cause drift.
            _currentSegmentInitialDistance = Blackboard.Instance.DistanceTracker.DistanceTravelled;
            _turnFailureDistance = _currentSegmentInitialDistance + trackSegmentInfo.TurnPointDistance + 0.5f;
        }

        private void OnGameStarted(string eventName, object sender, object data)
        {
            _gameStarted = true;
        }

        private void OnSuccessfullTurn(string eventName, object sender, object data)
        {
            _turnFailureDistance = float.MaxValue;
        }

        private void OnGameEnding(string eventName, object sender, object data)
        {
            _gameStarted = false;
        }
    }
}
