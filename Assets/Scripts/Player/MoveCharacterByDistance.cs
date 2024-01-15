using System;
using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    internal class MoveCharacterByDistance : MonoBehaviour
    {
        [SerializeField] private Transform _objectToMove;

        private Vector3 _currentDirection = Vector3.forward;
        private Vector3 _lastAnchorPoint;
        private float _lastAnchorDistance;

        private float _currentDistance = 0;

        private void Awake()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.SplinePointAdded, OnSplineChanged);
        }

        private void OnSplineChanged(object sender, object data)
        {
            var splineCreator = sender as SplineCreator2D;
            if (splineCreator == null || splineCreator.LinearSpline.Count < 2) return;
            // Create prefab from the last two points.
            int count = splineCreator.LinearSpline.Count;
            Vector3 point1 = splineCreator.LinearSpline[count - 2];
            Vector3 point2 = splineCreator.LinearSpline[count - 1];
            _currentDirection = (point2 - point1).normalized;
            _lastAnchorPoint = point1 + splineCreator.Offset * _currentDirection;
            _lastAnchorDistance = Blackboard.Instance.DistanceTracker.DistanceTravelled;
            RotateCharacter(_currentDirection);
        }

        private void RotateCharacter(Vector3 direction)
        {
            Quaternion rotation = Quaternion.LookRotation(direction);
            _objectToMove.localRotation = rotation;
        }

        private void Update()
        {
            float distance = Blackboard.Instance.DistanceTracker.DistanceTravelled;
            if (distance - _currentDistance < 0.001f) return;

            Vector3 newPosition = _lastAnchorPoint + (distance - _lastAnchorDistance) * _currentDirection;
            _objectToMove.localPosition = newPosition;
            _currentDistance = distance;
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.SplinePointAdded, OnSplineChanged);
        }
    }
}