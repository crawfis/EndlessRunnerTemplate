using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    public class VoxelPrefabSpawner : MonoBehaviour
    {
        [SerializeField] private float _widthScale = 1.0f;
        [SerializeField] private float _heightScale = 1.0f;
        [Tooltip("The prefab should have it's origin at the bottom-center with positive z-axis being the forward direction.")]
        [SerializeField] private GameObject _prefab;
        [Tooltip("Delete any older track segments keeping at most this number of prefabs.")]
        [SerializeField] private int _maxTrackSegments = 3;
        [SerializeField] private float _debugDestroyDelayTime = 4f;

        private Transform _parentTransform;
        private readonly Queue<GameObject> _spawnedTracks = new();
        private GameObject _currentTrack;
        private int _trackNumber = 1;

        private void Awake()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.SplineSegmentCreated, OnSplineChanged);
            var parent = new GameObject("Generated Level");
            _parentTransform = parent.transform;
        }

        private void OnSplineChanged(object sender, object data)
        {
            var splineCreator = sender as SplineCreator2D;
            // Create prefab from the last two points.
            (Vector3 point1, Vector3 point2, Direction turnDirection) = ((Vector3, Vector3, Direction))data;
            Vector3 direction = (point2 - point1);
            int numberOfVoxels = Mathf.FloorToInt(direction.magnitude + 0.2f);
            direction = Vector3.Normalize(direction);
            // Rotation to look at point 2
            Quaternion rotation = Quaternion.LookRotation(direction);
            var track = new GameObject(string.Format("Track {0:D2}-{0:D2}", _trackNumber));
            _spawnedTracks.Enqueue(track);
            Transform trackTransform = track.transform;
            trackTransform.parent = _parentTransform;
            trackTransform.SetLocalPositionAndRotation(point1, rotation);
            for (int i = 0; i < numberOfVoxels; i++)
            {
                var trackSegment = Instantiate<GameObject>(_prefab, trackTransform);
                trackSegment.transform.localPosition = new Vector3(0, 0, _widthScale * i);
            }
            _trackNumber++;
        }

        private void OnActiveSplineChanged(object sender, object data)
        {
            // Delete some old voxels.
        }
        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.SplineSegmentCreated, OnSplineChanged);
        }
    }
}