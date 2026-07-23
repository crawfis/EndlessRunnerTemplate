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
    /// <remarks>
    /// Execution order -20 puts this Update() ahead of DistanceInterestService (order 0), which is
    /// what publishes DistanceUpdated and therefore drives SegmentAdvanceTrigger's SegmentExited.
    /// If both thresholds are crossed in the same frame the failure must win, because SegmentExited
    /// advances the track and re-arms this detector for the next segment.
    /// </remarks>
    [DefaultExecutionOrder(-20)]
    internal class TurnCollisionDetector : MonoBehaviour
    {
        // Cumulative distance at the START of the current segment. Accumulated from segment
        // lengths rather than sampled from DistanceTracker so it matches the exact boundaries
        // SegmentAdvanceTrigger and SegmentTransitionController use (sampling drifts forward by
        // the per-frame overshoot, pushing the failure point past the segment exit).
        private float _currentSegmentInitialDistance = 0f;
        private float _previousSegmentLength = 0f;
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
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.TurnLeftCompleted, OnSuccessfullTurn);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.TurnRightCompleted, OnSuccessfullTurn);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.TempleRunStarted, OnGameStarted);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.PlayerDied, OnGameEnding);
        }

        private void OnTrackChanging(string eventName, object sender, object data)
        {
            TrackSegmentInfo trackSegmentInfo = (TrackSegmentInfo)data;
            _isCurrentSegmentStraight = trackSegmentInfo.Direction == Direction.Straight;
            _isRunning = true;
            _currentSegmentInitialDistance += _previousSegmentLength;
            _previousSegmentLength = trackSegmentInfo.Length;
            // TurnPointDistance is float.MaxValue for straights (never fails) and is clamped by
            // TrackSegmentLibrary.NormalizeSegments to stay strictly inside the segment for turns.
            _turnFailureDistance = _currentSegmentInitialDistance + trackSegmentInfo.TurnPointDistance;
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
