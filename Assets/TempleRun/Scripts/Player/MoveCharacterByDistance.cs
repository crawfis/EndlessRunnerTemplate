using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Moves the player along the current spline with lateral lane offset, jump height, and slide height.
    ///    Dependencies: Blackboard, DistanceTracker, LaneChangeController, EventsPublisherTempleRun
    ///    Subscribes: CurrentSplineChanging — re-anchors at the START of each new sub-spline
    /// </summary>
    /// <remarks>
    /// CurrentSplineChanging (not CurrentSplineChanged) is intentional: Changing fires at the
    /// start of each sub-spline with point1 = sub-spline start and distance ≈ start distance,
    /// giving a correct anchor. Changed fires at segment END with point1 = segment start but
    /// distance = exit distance, which would reset the anchor backward. For turn segments,
    /// Changing fires twice (approach + exit); each call correctly re-anchors to the new sub-spline.
    /// </remarks>
    public class MoveCharacterByDistance : MonoBehaviour
    {
        [SerializeField] private Transform _objectToMove;

        private Vector3 _currentDirection = Vector3.forward;
        private Vector3 _lastAnchorPoint;
        private float _lastAnchorDistance;
        private float _currentDistance = 0;
        private float _yPosition;

        private void Awake()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.CurrentSplineChanging, OnSplineChanging);
            _yPosition = transform.localPosition.y;
        }

        private void OnSplineChanging(string eventName, object sender, object data)
        {
            var (point1, point2, direction, _) = ((Vector3, Vector3, Direction, float))data;
            _currentDirection = (point2 - point1).normalized;
            _lastAnchorPoint = point1;
            _lastAnchorDistance = Blackboard.Instance.DistanceTracker.DistanceTravelled;
            float yPos = _yPosition + Blackboard.Instance.JumpHeightOffset + Blackboard.Instance.SlideHeightOffset;
            Vector3 basePos = new Vector3(point1.x, yPos, point1.z);
            basePos += GetLateralOffset();
            _objectToMove.localPosition = basePos;
            SetRotation(_currentDirection);
        }

        private void SetRotation(Vector3 direction)
        {
            Quaternion rotation = Quaternion.LookRotation(direction);
            _objectToMove.localRotation = rotation;
        }

        private void Update()
        {
            float distance = Blackboard.Instance.DistanceTracker.DistanceTravelled;
            if (distance - _currentDistance < 0.001f) return;

            Vector3 newPosition = _lastAnchorPoint + (distance - _lastAnchorDistance) * _currentDirection;
            float yPos = _yPosition + Blackboard.Instance.JumpHeightOffset + Blackboard.Instance.SlideHeightOffset;
            newPosition = new Vector3(newPosition.x, yPos, newPosition.z);
            newPosition += GetLateralOffset();
            _objectToMove.localPosition = newPosition;
            _currentDistance = distance;
        }

        /// <summary>
        /// Computes the lateral offset perpendicular to the current movement direction.
        /// Positive LateralLaneOffset shifts right, negative shifts left (from the player's perspective).
        /// </summary>
        private Vector3 GetLateralOffset()
        {
            var laneChangeController = Blackboard.Instance.LaneChangeController;
            if (laneChangeController == null)
                return Vector3.zero;

            float laneOffset = laneChangeController.LateralLaneOffset;
            if (Mathf.Abs(laneOffset) < 0.001f) return Vector3.zero;

            Vector3 perpendicular = Vector3.Cross(_currentDirection, Vector3.up).normalized;
            return laneOffset * perpendicular;
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.CurrentSplineChanging, OnSplineChanging);
        }
    }
}