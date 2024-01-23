using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    public class SplineCreator2D : MonoBehaviour
    {
        [SerializeField] private float _widthScale = 1.0f;
        [SerializeField] private float _heightScale = 1.0f;
        [SerializeField] private Vector3 _anchorPoint = Vector3.zero;

        public List<(Vector3 point1, Vector3 point2, Direction endDirection)> Splines { get; private set; } = new();
        public (Vector3 point1,  Vector3 point2, Direction endDirection) ActiveSpline
        {
            get
            {
                return (Splines[_trackCounter]);
            }
        }
        public float Offset {  get {  return -0.5f*_widthScale; } }

        private Vector3[] _directionAxes = { new(0, 0, 1), new(1, 0, 0), new(0, 0, -1), new(-1, 0, 0) };
        private int _directionIndex = 0; // Start in the positive z direction.
        float _totalDistance = 0;
        private int _trackCounter = 0;
        Vector3 _point0 = Vector3.zero;

        private void Start()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.ActiveTrackChanged, OnTrackChanged);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.TrackSegmentCreated, OnTrackCreated);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.GameStarted, OnGameStarted);
            Blackboard.Instance.TrackWidthOffset = 0.5f * _widthScale;
        }

        private void OnGameStarted(object sender, object data)
        {
            Debug.Log("GameStarted in SplineCreator2D");
        }

        private void TrackTurnedLeft()
        {
            _directionIndex = (_directionIndex == 0) ? 3 : _directionIndex - 1;
        }

        private void OnRightTurn(object sender, object data)
        {
            _directionIndex = (_directionIndex + 1) % 4;
        }

        private void CreateSplineSegment(float distance, Direction direction)
        {
            var point1 = _anchorPoint + (_heightScale * distance) * _directionAxes[_directionIndex];
            Splines.Add((_anchorPoint, point1, direction));
            EventsPublisherTempleRun.Instance.PublishEvent(KnownEvents.SplineSegmentCreated, this, Splines[^1]);
            _totalDistance += Vector3.Distance(point1, _point0);
            _anchorPoint = point1;
        }

        private void OnTrackChanged(object sender, object data)
        {
            EventsPublisherTempleRun.Instance.PublishEvent(KnownEvents.CurrentSplineChanged, this, ActiveSpline);
            _trackCounter++;
        }

        private void OnTrackCreated(object sender, object data)
        {
            var (direction, segmentDistance) = ((Direction direction, float segmentDistance))data;
            CreateSplineSegment(segmentDistance, direction);
            _anchorPoint -= Blackboard.Instance.TrackWidthOffset * _directionAxes[_directionIndex];
            switch (direction)
            {
                case Direction.Left: TrackTurnedLeft(); break;
                case Direction.Right: OnRightTurn(this, null);break;
            }
            _anchorPoint += Blackboard.Instance.TrackWidthOffset * _directionAxes[_directionIndex];
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.ActiveTrackChanged, OnTrackChanged);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.TrackSegmentCreated, OnTrackCreated);
        }
    }
}