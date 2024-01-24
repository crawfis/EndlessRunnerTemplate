using System.Collections.Generic;
using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    internal class SplinePrefabSpawner : MonoBehaviour
    {
        [Tooltip("The prefab should have it's origin at the bottom-center with positive z-axis being the forward direction.")]
        [SerializeField] private GameObject _prefab;
        [SerializeField] private float _widthScale = 1;
        [Tooltip("Delete any older track segments keeping at most this number of prefabs.")]
        [SerializeField] private int _maxTrackSegments = 3;
        [SerializeField] private float _debugDestroyDelayTime = 4f;

        private Transform _parentTransform;
        private readonly Dictionary<(Vector3 point1, Vector3 point2, Direction turnDirection),GameObject> _spawnedTracks = new();
        private GameObject _currentTrack;
        private int _trackNumber = 1;

        private void Awake()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.SplineSegmentCreated, OnSplineChanged);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.CurrentSplineChanged, OnActiveSplineChanged);
            var parent = new GameObject("Generated Level");
            _parentTransform = parent.transform;
            Blackboard.Instance.TrackWidthOffset = 0.5f * _widthScale;
        }

        private void OnSplineChanged(object sender, object data)
        {
            (Vector3 point1, Vector3 point2, Direction turnDirection) = ((Vector3, Vector3, Direction))data;
            var splineCreator = sender as SplineCreator2D;
            // Create prefab from the last two points.
            int count = splineCreator.Splines.Count;
            float zScale = Mathf.Abs(Vector3.Distance(point1, point2));
            Vector3 direction = (point2 - point1).normalized;

            // Rotation to look at point 2
            Quaternion rotation = Quaternion.LookRotation(direction);
            var track = new GameObject(string.Format("Track {0:D2}", _trackNumber));
            _spawnedTracks.Add((point1, point2, turnDirection),track);
            Transform trackTransform = track.transform;
            trackTransform.parent = _parentTransform;
            trackTransform.SetLocalPositionAndRotation(point1, rotation);
            var trackSegment = Instantiate<GameObject>(_prefab, trackTransform);
            trackSegment.transform.localScale = new Vector3(1, 1, zScale);
            _trackNumber++;
        }

        private void OnActiveSplineChanged(object sender, object data)
        {
            (Vector3 point1, Vector3 point2, Direction turnDirection) = ((Vector3, Vector3, Direction))data;
            if(_spawnedTracks.TryGetValue((point1, point2, turnDirection), out var track))
            {
                Destroy(track, _debugDestroyDelayTime);
            }
        }
        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.SplineSegmentCreated, OnSplineChanged);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.CurrentSplineChanged, OnActiveSplineChanged);
        }
    }
}