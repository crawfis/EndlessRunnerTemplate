using CrawfisSoftware.Events;

using UnityEngine;

// Note: CrawfisSoftware.Events import needed for EventsPublisherUserInitiated

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Validates lane change requests and publishes lane change events.
    /// Blocks lane changes at boundaries and while a change is in progress.
    ///    Dependencies: Blackboard, LaneConfig
    ///    Subscribes: UserInitiatedEvents.LeftLaneChangeRequested, RightLaneChangeRequested
    ///    Subscribes: TempleRunEvents.LaneChangedLeft, LaneChangedRight (clear _isChanging)
    ///    Publishes: TempleRunEvents.LaneChangeLeftRequested, LaneChangeRightRequested
    ///    Publishes: TempleRunEvents.LaneChangeLeftFailed, LaneChangeRightFailed
    /// </summary>
    public class LaneChangeController : MonoBehaviour
    {
        private int _minLane;
        private int _maxLane;
        private bool _isChanging = false;

        public int CurrentLane { get; set; } = 0;            // -1=left, 0=center, 1=right (for 3 lanes)
        public float LateralLaneOffset { get; set; } = 0f;   // Smooth lateral offset in world units

        private void Awake()
        {
            Blackboard.Instance.LaneChangeController = this;
        }
        private void Start()
        {
            // Subscribe to raw input events
            EventsPublisherUserInitiated.Instance.SubscribeToEvent(
                UserInitiatedEvents.UserLeftLaneChangeRequested, OnLeftLaneChangeRequested);
            EventsPublisherUserInitiated.Instance.SubscribeToEvent(
                UserInitiatedEvents.UserRightLaneChangeRequested, OnRightLaneChangeRequested);

            // Subscribe to completion events to clear the _isChanging flag
            EventsPublisherTempleRun.Instance.SubscribeToEvent(
                TempleRunEvents.LaneChangedLeft, OnLaneChangeCompleted);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(
                TempleRunEvents.LaneChangedRight, OnLaneChangeCompleted);

            // Subscribe to game start to reset lane state
            EventsPublisherTempleRun.Instance.SubscribeToEvent(
                TempleRunEvents.TempleRunStarting, OnGameStarting);

            // Compute lane boundaries from config
            var laneConfig = Blackboard.Instance.LaneConfig;
            if (laneConfig != null)
            {
                int halfLanes = (laneConfig.LaneCount - 1) / 2;
                _minLane = -halfLanes;  // -1 for 3 lanes
                _maxLane = halfLanes;   //  1 for 3 lanes
            }
            else
            {
                // Sensible defaults if no config assigned
                _minLane = -1;
                _maxLane = 1;
                Debug.LogWarning("LaneChangeController: No LaneConfig assigned on Blackboard. Using default 3-lane layout.");
            }
        }

        private void OnDestroy()
        {
            EventsPublisherUserInitiated.Instance.UnsubscribeToEvent(
                UserInitiatedEvents.UserLeftLaneChangeRequested, OnLeftLaneChangeRequested);
            EventsPublisherUserInitiated.Instance.UnsubscribeToEvent(
                UserInitiatedEvents.UserRightLaneChangeRequested, OnRightLaneChangeRequested);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(
                TempleRunEvents.LaneChangedLeft, OnLaneChangeCompleted);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(
                TempleRunEvents.LaneChangedRight, OnLaneChangeCompleted);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(
                TempleRunEvents.TempleRunStarting, OnGameStarting);

        }

        private void OnLeftLaneChangeRequested(string eventName, object sender, object data)
        {
            if (_isChanging)
            {
                //EventsPublisherTempleRun.Instance.PublishEvent(
                //    TempleRunEvents.LaneChangeLeftFailed, this, Blackboard.Instance.CurrentLane);
                return;
            }

            int currentLane = CurrentLane;
            if (currentLane <= _minLane)
            {
                // Already at leftmost lane
                EventsPublisherTempleRun.Instance.PublishEvent(
                    TempleRunEvents.LaneChangeLeftFailed, this, currentLane);
                return;
            }

            _isChanging = true;
            CurrentLane = currentLane - 1;
            EventsPublisherTempleRun.Instance.PublishEvent(
                TempleRunEvents.LaneChangingLeft, this, CurrentLane);
        }

        private void OnRightLaneChangeRequested(string eventName, object sender, object data)
        {
            if (_isChanging)
            {
                //EventsPublisherTempleRun.Instance.PublishEvent(
                //    TempleRunEvents.LaneChangeRightFailed, this, Blackboard.Instance.CurrentLane);
                return;
            }

            int currentLane = CurrentLane;
            if (currentLane >= _maxLane)
            {
                // Already at rightmost lane
                EventsPublisherTempleRun.Instance.PublishEvent(
                    TempleRunEvents.LaneChangeRightFailed, this, currentLane);
                return;
            }

            _isChanging = true;
            CurrentLane = currentLane + 1;
            EventsPublisherTempleRun.Instance.PublishEvent(
                TempleRunEvents.LaneChangingRight, this, CurrentLane);
        }

        private void OnLaneChangeCompleted(string eventName, object sender, object data)
        {
            _isChanging = false;
        }

        private void OnGameStarting(string eventName, object sender, object data)
        {
            _isChanging = false;
            CurrentLane = 0;
            LateralLaneOffset = 0f;
        }
    }
}
