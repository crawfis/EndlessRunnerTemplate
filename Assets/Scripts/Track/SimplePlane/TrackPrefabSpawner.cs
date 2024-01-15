using System;
using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    [Obsolete("Use SplineCreator2D and SplinePrefabSpawner instead.")]
    public class TrackPrefabSpawner : MonoBehaviour
    {
        [SerializeField] private float _widthScale = 1.0f;
        [SerializeField] private float _heightScale = 1.0f;
        [Tooltip("The prefab should have it's origin at the bottom-center with positive z-axis being the forward direction.")]
        [SerializeField] private GameObject _prefab;

        // Some documentation is needed. We will require the prefab's origin to be at the lower-center of the track.
        // We keep track of this anchor position and update every time a new track segment is spawned.
        // This is accomplished by moving the anchor the track distance along the direction of the new track.
        // To accomplish this we keep track of the direction using the _directionIndex into the constant _directionAxes's
        // which go clockwise around the x and z axes, starting with positive z. A left turn will decrement the index modulo 4.
        // A right turn will increase the index (modulo 4). The anchor point will move along this axis scaled by the distance.
        private float _anchorRotation = 0;
        private Vector3 _anchorPoint = Vector3.zero;
        private Vector3[] _directionAxes = { new(0, 0, 1), new(1, 0, 0), new(0, 0, -1), new(-1, 0, 0) };
        private int _directionIndex = 0; // Start in the positive z direction.
        private Transform _parentTransform;
        private GameObject _currentTrack;
        private int _trackNumber = 1;

        void Start()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.ActiveTrackChanged, OnTrackChanged);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.LeftTurnSucceeded, OnLeftTurn);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.RightTurnSucceeded, OnRightTurn);
            var parent = new GameObject("Generated Level");
            _parentTransform = parent.transform;
        }

        private void OnLeftTurn(object sender, object data)
        {
            UpdateRotation(Direction.Left);
            _directionIndex = (_directionIndex == 0) ? 3 : _directionIndex - 1;
            PreSpawnUpdateAnchorPoint();
        }

        private void OnRightTurn(object sender, object data)
        {
            UpdateRotation(Direction.Right);
            _directionIndex = (_directionIndex + 1) % 4;
            PreSpawnUpdateAnchorPoint();
        }

        private void PreSpawnUpdateAnchorPoint()
        {
            _anchorPoint += 0.5f * _widthScale * _directionAxes[_directionIndex];
        }

        private void PostSpawnUpdateAnchorPoint(float distance)
        {
            _anchorPoint += (_heightScale * distance - 0.5f * _widthScale) * _directionAxes[_directionIndex];
        }

        private void OnTrackChanged(object sender, object data)
        {
            var (direction, segmentDistance) = ((Direction direction, float segmentDistance))data;
            _currentTrack = new GameObject(string.Format("Track {0:D2}", _trackNumber));
            Transform trackTransform = _currentTrack.transform;
            trackTransform.parent = _parentTransform;
            trackTransform.localRotation = Quaternion.Euler(0, _anchorRotation, 0);
            trackTransform.localPosition = _anchorPoint;
            var trackSegment = Instantiate<GameObject>(_prefab, trackTransform);
            trackSegment.transform.localScale = new Vector3(1, 1, segmentDistance);
            PostSpawnUpdateAnchorPoint(segmentDistance);
            // Spawn next segment(s) so we need to see the turn. This can be deleted on next spawned track.
            _trackNumber++;
        }

        private void UpdateRotation(Direction direction)
        {
            switch (direction)
            {
                case Direction.Left:
                    _anchorRotation += -90f;
                    break;
                case Direction.Right:
                    _anchorRotation += 90;
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.ActiveTrackChanged, OnTrackChanged);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.LeftTurnSucceeded, OnLeftTurn);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.RightTurnSucceeded, OnRightTurn);
        }
    }
}