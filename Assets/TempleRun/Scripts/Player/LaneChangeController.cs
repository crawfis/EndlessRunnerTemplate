using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Validates lane change requests and publishes lane change events.
    /// Blocks lane changes at boundaries and while a change is in progress.
    ///    Dependencies: Blackboard, LaneConfig
    ///    Subscribes: TempleRunEvents.LaneChangeLeftRequested, LaneChangeRightRequested
    ///                (from bridge translating UserInitiated)
    ///    Subscribes: TempleRunEvents.LaneChangedLeft, LaneChangedRight (clear _isChanging)
    ///    Publishes: TempleRunEvents.LaneChangingLeft, LaneChangingRight (once validation passes)
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
            // Subscribe to TempleRun domain events, not UserInitiated.
            // This allows lane changes to be triggered from any source: player input, AI, replay,
            // network. The bridge translates UserInitiated.UserLeftLaneChangeRequested ->
            // TempleRunEvents.LaneChangeLeftRequested.
            TempleRunBus.Subscribe(
                TempleRunEvents.LaneChangeLeftRequested, OnLeftLaneChangeRequested);
            TempleRunBus.Subscribe(
                TempleRunEvents.LaneChangeRightRequested, OnRightLaneChangeRequested);

            // Subscribe to completion events to clear the _isChanging flag
            TempleRunBus.Subscribe(
                TempleRunEvents.LaneChangedLeft, OnLaneChangeCompleted);
            TempleRunBus.Subscribe(
                TempleRunEvents.LaneChangedRight, OnLaneChangeCompleted);

            // Subscribe to game start to reset lane state
            TempleRunBus.Subscribe(
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
            TempleRunBus.Unsubscribe(
                TempleRunEvents.LaneChangeLeftRequested, OnLeftLaneChangeRequested);
            TempleRunBus.Unsubscribe(
                TempleRunEvents.LaneChangeRightRequested, OnRightLaneChangeRequested);
            TempleRunBus.Unsubscribe(
                TempleRunEvents.LaneChangedLeft, OnLaneChangeCompleted);
            TempleRunBus.Unsubscribe(
                TempleRunEvents.LaneChangedRight, OnLaneChangeCompleted);
            TempleRunBus.Unsubscribe(
                TempleRunEvents.TempleRunStarting, OnGameStarting);

        }

        private void OnLeftLaneChangeRequested(string eventName, object sender, object data)
        {
            if (_isChanging)
            {
                //TempleRunBus.Publish(
                //    TempleRunEvents.LaneChangeLeftFailed, this, Blackboard.Instance.CurrentLane);
                return;
            }

            int currentLane = CurrentLane;
            if (currentLane <= _minLane)
            {
                // Already at leftmost lane
                TempleRunBus.Publish(
                    TempleRunEvents.LaneChangeLeftFailed, this, currentLane);
                return;
            }

            _isChanging = true;
            CurrentLane = currentLane - 1;
            TempleRunBus.Publish(
                TempleRunEvents.LaneChangingLeft, this, CurrentLane);
        }

        private void OnRightLaneChangeRequested(string eventName, object sender, object data)
        {
            if (_isChanging)
            {
                //TempleRunBus.Publish(
                //    TempleRunEvents.LaneChangeRightFailed, this, Blackboard.Instance.CurrentLane);
                return;
            }

            int currentLane = CurrentLane;
            if (currentLane >= _maxLane)
            {
                // Already at rightmost lane
                TempleRunBus.Publish(
                    TempleRunEvents.LaneChangeRightFailed, this, currentLane);
                return;
            }

            _isChanging = true;
            CurrentLane = currentLane + 1;
            TempleRunBus.Publish(
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
