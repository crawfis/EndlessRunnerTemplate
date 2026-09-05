using CrawfisSoftware.TempleRun.GameConfig;

using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Decides WHEN the player transitions between segments and publishes
    /// full lifecycle events: Entering, Entered, Exiting, Exited.
    /// Current implementation: distance-based polling of DistanceTracker in Update().
    /// Future: could be swapped for collider-based triggers or DistanceInterestService callbacks.
    ///    Dependencies: Blackboard.DistanceTracker, EventsFor<TempleRunEvents>
    ///    Subscribes: ActiveTrackChanging — tracks the current segment and exit distance
    ///    Subscribes: TempleRunStarted — enables distance checking
    ///    Subscribes: TempleRunEnded — disables distance checking, however the run ended
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
            TempleRunBus.Subscribe(TempleRunEvents.ActiveTrackChanging, OnTrackChanging);
            TempleRunBus.Subscribe(TempleRunEvents.DistanceUpdated, OnDistanceUpdated);
            TempleRunBus.Subscribe(TempleRunEvents.TempleRunStarted, OnGameStarted);
            TempleRunBus.Subscribe(TempleRunEvents.TempleRunEnded, OnGameEnding);
        }

        private void OnDestroy()
        {
            TempleRunBus.Unsubscribe(TempleRunEvents.ActiveTrackChanging, OnTrackChanging);
            TempleRunBus.Unsubscribe(TempleRunEvents.DistanceUpdated, OnDistanceUpdated);
            TempleRunBus.Unsubscribe(TempleRunEvents.TempleRunStarted, OnGameStarted);
            TempleRunBus.Unsubscribe(TempleRunEvents.TempleRunEnded, OnGameEnding);
        }

        private void OnDistanceUpdated(string eventName, object sender, object data)
        {
            var distance = (float)data;
            if (!_isRunning || !_gameStarted) return;

            // Fire SegmentExiting once when the player approaches the exit.
            if (!_exitingFired && distance >= _currentExitDistance - TempleRunConstants.SegmentExitingTriggerDistance)
            {
                _exitingFired = true;
                TempleRunBus.Publish(TempleRunEvents.SegmentExiting, this, _currentSegment);
            }

            // Fire SegmentExited when the player reaches or passes the exit distance.
            if (distance >= _currentExitDistance)
            {
                _isRunning = false;
                TempleRunBus.Publish(TempleRunEvents.SegmentExited, this, _currentSegment);
            }
        }

        private void OnTrackChanging(string eventName, object sender, object data)
        {
            var segment = (TrackSegmentInfo)data;
            _currentSegment = segment;
            _isRunning = true;
            _exitingFired = false;
            // Run-absolute off the message. This used to accumulate segment lengths privately, and
            // three other components kept the same sum so their boundaries would agree with it.
            _currentExitDistance = _currentSegment.EndDistance;

            DistanceInterestService.Instance.Register(_currentExitDistance - TempleRunConstants.SegmentExitingTriggerDistance);
            DistanceInterestService.Instance.Register(_currentExitDistance);
            // Publish lifecycle: entering/entered (synchronous, immediate on track change).
            // Move to AutoFire based on ActiveTrackChanging if we want to decouple from track changes and allow other triggers (e.g. teleport).
            TempleRunBus.Publish(TempleRunEvents.SegmentEntering, this, _currentSegment);
            TempleRunBus.Publish(TempleRunEvents.SegmentEntered, this, _currentSegment);
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
