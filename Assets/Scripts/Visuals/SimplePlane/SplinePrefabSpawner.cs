using System.Collections.Generic;
using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    internal class SplinePrefabSpawner : MonoBehaviour
    {
        [SerializeField] private float _widthScale = 1.0f;
        [SerializeField] private float _heightScale = 1.0f;
        [Tooltip("The prefab should have it's origin at the bottom-center with positive z-axis being the forward direction.")]
        [SerializeField] private GameObject _prefab;
        [Tooltip("Delete any older track segments keeping at most this number of prefabs.")]
        [SerializeField] private int _maxTrackSegments = 3;
        [SerializeField] private float _debugDestroyDelayTime = 4f;

        private Transform _parentTransform;
        private Queue<GameObject> _spawnedTracks = new();
        private GameObject _currentTrack;
        private int _trackNumber = 1;
        private Vector3 _lastPoint;
        private bool _isFirstPoint = true;
        private void Awake()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.SplinePointAdded, OnSplineChanged);
            //EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.CurrentSplineChanged, OnActiveSplineChanged);
            var parent = new GameObject("Generated Level");
            _parentTransform = parent.transform;
        }

        private void OnSplineChanged(object sender, object data)
        {
            Vector3 point2 = (Vector3)data;
            if (_isFirstPoint)
            {
                _isFirstPoint = false;
                _lastPoint = point2;
                return;
            }
            if (_spawnedTracks.Count >= _maxTrackSegments) GameObject.Destroy(_spawnedTracks.Dequeue(), _debugDestroyDelayTime);

            var splineCreator = sender as SplineCreator2D;
            // Create prefab from the last two points.
            int count = splineCreator.LinearSpline.Count;
            Vector3 point1 = _lastPoint;
            float zScale = Mathf.Abs(Vector3.Distance(point1, point2));
            Vector3 direction = (point2 - point1).normalized;
            Vector3 widthOffset = -splineCreator.Offset * direction;

            // Rotation to look at point 2
            Quaternion rotation = Quaternion.LookRotation(direction);
            var track = new GameObject(string.Format("Track {0:D2}", _trackNumber));
            _spawnedTracks.Enqueue(track);
            Transform trackTransform = track.transform;
            trackTransform.parent = _parentTransform;
            trackTransform.SetLocalPositionAndRotation(point1 + widthOffset, rotation);
            var trackSegment = Instantiate<GameObject>(_prefab, trackTransform);
            trackSegment.transform.localScale = new Vector3(1, 1, zScale);
            _lastPoint = point2;
            _trackNumber++;
        }

        private void OnActiveSplineChanged(object sender, object data)
        {
            // Delete some old splines.
        }
        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.SplinePointAdded, OnSplineChanged);
        }
    }
}