using System.Collections.Generic;
using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    public class SplineCreator2D : MonoBehaviour
    {
        [SerializeField] private Vector3 _anchorPoint = Vector3.zero;

        private readonly Vector3[] _directionAxes = { new(0, 0, 1), new(1, 0, 0), new(0, 0, -1), new(-1, 0, 0) };
        private int _directionIndex = 0; // Start in the positive z direction.
        float _totalSplineDistance = 0;
        float _totalEventDistance = 0;

        public Queue<(Vector3 point1, Vector3 point2, Direction endDirection)> Splines { get; private set; } = new();
        public (Vector3 point1, Vector3 point2, Direction endDirection) ActiveSpline
        {
            get
            {
                return (Splines.Peek());
            }
        }
        Vector3 _point0 = Vector3.zero;

        private void Start()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.ActiveTrackChanging, OnTrackChanging);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.TrackSegmentCreated, OnTrackCreated);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.TeleportEnded, OnTrackChanged);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.GameStarted, OnGameStarted);
        }

        private void OnGameStarted(object sender, object data)
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.GameStarted, OnGameStarted);
            Debug.Log("GameStarted in SplineCreator2D");
        }

        private void TurnLeft()
        {
            _directionIndex = (_directionIndex == 0) ? 3 : _directionIndex - 1;
        }

        private void TurnRight()
        {
            _directionIndex = (_directionIndex + 1) % 4;
        }

        private void CreateSplineSegment(float distance, Direction direction)
        {
            var point1 = _anchorPoint + distance * _directionAxes[_directionIndex];
            Splines.Enqueue((_anchorPoint, point1, direction));
            EventsPublisherTempleRun.Instance.PublishEvent(KnownEvents.SplineSegmentCreated, this, (_anchorPoint, point1, direction));
            _totalSplineDistance += Vector3.Distance(point1, _point0);
            _totalEventDistance += distance;
            _anchorPoint = point1;
        }

        private void OnTrackChanging(object sender, object data)
        {
            EventsPublisherTempleRun.Instance.PublishEvent(KnownEvents.CurrentSplineChanging, this, ActiveSpline);
        }

        private void OnTrackChanged(object sender, object data)
        {
            EventsPublisherTempleRun.Instance.PublishEvent(KnownEvents.CurrentSplineChanged, this, ActiveSpline);
            _ = Splines.Dequeue();
        }

        private void OnTrackCreated(object sender, object data)
        {
            var (direction, segmentDistance) = ((Direction direction, float segmentDistance))data;
            CreateSplineSegment(segmentDistance, direction);
            _anchorPoint -= Blackboard.Instance.TrackWidthOffset * _directionAxes[_directionIndex];
            switch (direction)
            {
                case Direction.Left: TurnLeft(); break;
                case Direction.Right: TurnRight(); break;
            }
            _anchorPoint += Blackboard.Instance.TrackWidthOffset * _directionAxes[_directionIndex];
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.ActiveTrackChanging, OnTrackChanged);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.TrackSegmentCreated, OnTrackCreated);
        }
    }
}