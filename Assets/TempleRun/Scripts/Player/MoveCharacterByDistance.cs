using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Moves the player along the current spline with lateral lane offset, jump height, and slide height.
    ///    Dependencies: Blackboard, DistanceTracker, LaneChangeController, EventsFor<TempleRunEvents>
    ///    Subscribes: CurrentSplineChanging — re-anchors at the START of each new sub-spline
    /// </summary>
    /// <remarks>
    /// CurrentSplineChanging (not CurrentSplineChanged) is intentional: Changing fires at the
    /// start of each section, so Section.Start is where the player actually is and the anchor is
    /// correct. Changed fires at segment END carrying the section's original start, which would
    /// reset the anchor backward. For turn segments, Changing fires twice (approach + exit); each
    /// call correctly re-anchors to the new section.
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
            TempleRunBus.Subscribe(TempleRunEvents.CurrentSplineChanging, OnSplineChanging);
            _yPosition = transform.localPosition.y;
        }

        private void OnSplineChanging(string eventName, object sender, object data)
        {
            var section = (SplineSection)data;
            _currentDirection = section.Heading;
            _lastAnchorPoint = section.Start;
            _lastAnchorDistance = Blackboard.Instance.DistanceTracker.DistanceTravelled;
            // Re-anchor always; only place the player when nobody else is going to. Who that is
            // is named on the message - see SplineSection.TeleportOwnsTransform, which is where
            // the reason this must not snap now lives.
            if (section.TeleportOwnsTransform) return;

            float yPos = _yPosition + Blackboard.Instance.JumpHeightOffset + Blackboard.Instance.SlideHeightOffset;
            Vector3 basePos = new Vector3(section.Start.x, yPos, section.Start.z);
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
            TempleRunBus.Unsubscribe(TempleRunEvents.CurrentSplineChanging, OnSplineChanging);
        }
    }
}