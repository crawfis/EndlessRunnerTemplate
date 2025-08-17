using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Moves the player along the current spline.
    ///    Dependencies: Blackboard, DistanceTracker, EventsPublisherTempleRun
    ///    Subscribes: CurrentSplineChanged
    /// </summary>
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
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.CurrentSplineChanged, OnSplineChanged);
            _yPosition = transform.localPosition.y;
        }

        private void OnSplineChanged(string eventName, object sender, object data)
        {
            // Create prefab from the two points.
            var (point1, point2, direction) = ((Vector3 point1, Vector3 point2, Direction direction))(data);
            _currentDirection = (point2 - point1).normalized;
            _lastAnchorPoint = point1;
            _lastAnchorDistance = Blackboard.Instance.DistanceTracker.DistanceTravelled;
            _objectToMove.localPosition = new Vector3(point1.x, _yPosition, point1.z);
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
            _objectToMove.localPosition = new Vector3(newPosition.x, _yPosition, newPosition.z);
            _currentDistance = distance;
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.CurrentSplineChanged, OnSplineChanged);
        }
    }
}