using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    public class SplineCreator2D : MonoBehaviour
    {
        /// <summary>
        /// Spline points to the middle of the turns. It starts a 1/2 width below in the z-axis to be consistent.
        /// </summary>
        public List<Vector3> LinearSpline { get; private set; } = new();

        [SerializeField] private float _widthScale = 1.0f;
        [SerializeField] private float _heightScale = 1.0f;

        private Vector3 _anchorPoint = Vector3.zero;
        private Vector3[] _directionAxes = { new(0, 0, 1), new(1, 0, 0), new(0, 0, -1), new(-1, 0, 0) };
        private int _directionIndex = 0; // Start in the positive z direction.

        private void Start()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.ActiveTrackChanged, OnTrackChanged);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.LeftTurnSucceeded, OnLeftTurn);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.RightTurnSucceeded, OnRightTurn);
            _anchorPoint = new Vector3(0, 0, -0.5f * _widthScale);
            AddSplinePoint(_anchorPoint);
        }
        private void AddSplinePoint(Vector3 point)
        {
            LinearSpline.Add(point);
            EventsPublisherTempleRun.Instance.PublishEvent(KnownEvents.SplinePointAdded, this, point);
        }

        private void OnLeftTurn(object sender, object data)
        {
            _directionIndex = (_directionIndex == 0) ? 3 : _directionIndex - 1;
        }

        private void OnRightTurn(object sender, object data)
        {
            _directionIndex = (_directionIndex + 1) % 4;
        }

        private void PostSpawnUpdateAnchorPoint(float distance)
        {
            _anchorPoint += _heightScale * distance * _directionAxes[_directionIndex];
            AddSplinePoint(_anchorPoint);
        }

        private void OnTrackChanged(object sender, object data)
        {
            var (_, segmentDistance) = ((Direction direction, float segmentDistance))data;
            PostSpawnUpdateAnchorPoint(segmentDistance);
        }
    }
}