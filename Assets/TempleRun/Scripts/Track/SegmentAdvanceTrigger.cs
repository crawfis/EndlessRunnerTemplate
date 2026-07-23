using CrawfisSoftware.TempleRun.GameConfig;
using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Decides WHEN the player transitions between segments and publishes
    /// full lifecycle events: Entering, Entered, Exiting, Exited.
    /// Current implementation: distance-based polling of DistanceTracker in Update().
    /// Future: could be swapped for collider-based triggers or DistanceInterestService callbacks.
    ///    Dependencies: Blackboard.DistanceTracker, EventsPublisherTempleRun
    ///    Subscribes: ActiveTrackChanging — tracks the current segment and exit distance
    ///    Subscribes: TempleRunStarted — enables distance checking
    ///    Subscribes: PlayerDied — disables distance checking
    ///    Publishes: SegmentEntering, SegmentEntered, SegmentExiting, SegmentExited
    /// </summary>
    /// <remarks>
    /// This class no longer polls in Update(); it reacts to DistanceUpdated, which is published by
    /// DistanceInterestService (execution order 0). The ordering that matters for the missed-turn
    /// death chain is therefore TurnCollisionDetector (order -20) vs DistanceInterestService, not
    /// this class's own order. TurnCollisionDetector runs first, so if the player fails a turn the
    /// death chain fires synchronously and clears _gameStarted before SegmentExited can advance
    /// the track.
    /// </remarks>
    [DefaultExecutionOrder(10)]
    internal class SegmentAdvanceTrigger : MonoBehaviour
    {
        private float _currentExitDistance = 0f;
        private bool _gameStarted = false;
        private bool _isRunning = false;
        private bool _exitingFired = false;
        private TrackSegmentInfo _currentSegment;

        private void Awake()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.ActiveTrackChanging, OnTrackChanging);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.DistanceUpdated, OnDistanceUpdated);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.TempleRunStarted, OnGameStarted);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.PlayerDied, OnGameEnding);
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.ActiveTrackChanging, OnTrackChanging);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.DistanceUpdated, OnDistanceUpdated);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.TempleRunStarted, OnGameStarted);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.PlayerDied, OnGameEnding);
        }

        private void OnDistanceUpdated(string eventName, object sender, object data)
        {
            if (!_isRunning || !_gameStarted) return;

            float distance = (float)data;

            // Fire SegmentExiting once when the player approaches the exit.
            if (!_exitingFired && distance >= _currentExitDistance - TempleRunConstants.SegmentExitingTriggerDistance)
            {
                _exitingFired = true;
                EventsPublisherTempleRun.Instance.PublishEvent(TempleRunEvents.SegmentExiting, this, _currentSegment);
            }

            // Fire SegmentExited when the player reaches or passes the exit distance.
            if (distance >= _currentExitDistance)
            {
                _isRunning = false;
                EventsPublisherTempleRun.Instance.PublishEvent(TempleRunEvents.SegmentExited, this, _currentSegment);
            }
        }

        private void OnTrackChanging(string eventName, object sender, object data)
        {
            _currentSegment = (TrackSegmentInfo)data;
            _isRunning = true;
            _exitingFired = false;
            _currentExitDistance += _currentSegment.Length;

            DistanceInterestService.Instance.Register(_currentExitDistance - TempleRunConstants.SegmentExitingTriggerDistance);
            DistanceInterestService.Instance.Register(_currentExitDistance);
            // Publish lifecycle: entering/entered (synchronous, immediate on track change).
            // Move to AutoFire based on ActiveTrackChanging if we want to decouple from track changes and allow other triggers (e.g. teleport).
            EventsPublisherTempleRun.Instance.PublishEvent(TempleRunEvents.SegmentEntering, this, _currentSegment);
            EventsPublisherTempleRun.Instance.PublishEvent(TempleRunEvents.SegmentEntered, this, _currentSegment);
        }

        private void OnGameStarted(string eventName, object sender, object data)
        {
            _gameStarted = true;
        }

        private void OnGameEnding(string eventName, object sender, object data)
        {
            _gameStarted = false;
        }
    }
}
