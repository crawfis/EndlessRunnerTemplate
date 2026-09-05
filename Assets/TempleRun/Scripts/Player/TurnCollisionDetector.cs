using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Compares the distance from DistanceTracker to the current track segment length.
    /// Fires PlayerFailingAtTurn when the player exceeds a turn segment distance without turning.
    /// Straight segments are handled by SegmentAdvanceTrigger (SegmentExiting / SegmentExited).
    ///    Dependencies: Blackboard, DistanceTracker, EventsFor<TempleRunEvents>
    ///    Subscribes: TempleRunEvents.ActiveTrackChanging — increases the active track length
    ///    Subscribes: TempleRunEvents.PlayerActivated — begins distance checking; failure detection
    ///                arms when the player is released, not while the countdown ceremony runs
    ///    Subscribes: TempleRunEvents.TempleRunEnded — stops distance checking, however the run ended
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
        private float _turnFailureDistance;
        private bool _isRunning = false;
        private bool _gameStarted = false;
        private bool _isCurrentSegmentStraight = false;

        private void Awake()
        {
            TempleRunBus.Subscribe(TempleRunEvents.ActiveTrackChanging, OnTrackChanging);
            TempleRunBus.Subscribe(TempleRunEvents.TurnLeftStarted, OnSuccessfullTurn);
            TempleRunBus.Subscribe(TempleRunEvents.TurnRightStarted, OnSuccessfullTurn);
            TempleRunBus.Subscribe(TempleRunEvents.PlayerActivated, OnPlayerActivated);
            TempleRunBus.Subscribe(TempleRunEvents.TempleRunEnded, OnGameEnding);
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
                TempleRunBus.Publish(TempleRunEvents.PlayerFailingAtTurn, this, distance);
            }
        }

        private void OnDestroy()
        {
            TempleRunBus.Unsubscribe(TempleRunEvents.ActiveTrackChanging, OnTrackChanging);
            TempleRunBus.Unsubscribe(TempleRunEvents.TurnLeftStarted, OnSuccessfullTurn);
            TempleRunBus.Unsubscribe(TempleRunEvents.TurnRightStarted, OnSuccessfullTurn);
            TempleRunBus.Unsubscribe(TempleRunEvents.PlayerActivated, OnPlayerActivated);
            TempleRunBus.Unsubscribe(TempleRunEvents.TempleRunEnded, OnGameEnding);
        }

        private void OnTrackChanging(string eventName, object sender, object data)
        {
            var trackSegmentInfo = (TrackSegmentInfo)data;
            _isCurrentSegmentStraight = trackSegmentInfo.Direction == Direction.Straight;
            _isRunning = true;
            // Run-absolute, straight off the message: TrackManager stamped the segment's origin at
            // creation, so this no longer keeps a private running sum that has to agree by hand with
            // the boundaries SegmentAdvanceTrigger and SegmentTransitionController use.
            // TurnFailureDistance is float.MaxValue for straights (never fails) and is clamped by
            // TrackSegmentLibrary.Normalize to stay strictly inside the segment for turns.
            _turnFailureDistance = trackSegmentInfo.TurnFailureDistance;
        }

        private void OnPlayerActivated(string eventName, object sender, object data)
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
