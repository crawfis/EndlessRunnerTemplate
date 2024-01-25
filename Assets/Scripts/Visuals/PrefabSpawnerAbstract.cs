using System.Collections.Generic;
using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    public abstract class PrefabSpawnerAbstract : MonoBehaviour
    {
        [Tooltip("The prefab should have it's origin at the bottom-center with positive z-axis being the forward direction.")]
        [SerializeField] protected GameObject _prefab;
        [SerializeField] protected float _widthScale = 1;
        [Tooltip("Delete any older track segments keeping at most this number of prefabs.")]
        [SerializeField] protected float _debugDestroyDelayTime = 4f;

        protected Transform _parentTransform;
        protected readonly Dictionary<int, GameObject> _spawnedTracks = new();
        protected int _currentTrackID = -1;
        protected int _trackNumber = 0;
        protected virtual void Awake()
        {
            SubscribeToEvents();
        }

        protected void SubscribeToEvents()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.SplineSegmentCreated, OnSplineCreated);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.TeleportEnded, OnActiveSplineChanged);
            var parent = new GameObject("Generated Level");
            _parentTransform = parent.transform;
        }

        private void OnSplineCreated(object sender, object data)
        {
            var splineCreator = sender as SplineCreator2D;
            // Create prefab from the last two points.
            (Vector3 point1, Vector3 point2, Direction turnDirection) = ((Vector3, Vector3, Direction))data;
            Vector3 direction = (point2 - point1);
            Vector3 unitDirection = direction.normalized;
            // Rotation to look at point 2
            Quaternion rotation = Quaternion.LookRotation(unitDirection);
            var track = new GameObject(string.Format("Track {0:D2}", _trackNumber));
            _spawnedTracks.Add(_trackNumber, track);
            Transform trackTransform = track.transform;
            trackTransform.parent = _parentTransform;
            trackTransform.SetLocalPositionAndRotation(point1, rotation);
            CreateTrack(direction, trackTransform);
            _trackNumber++;
        }

        protected abstract void CreateTrack(Vector3 direction, Transform trackTransform);

        private void OnActiveSplineChanged(object sender, object data)
        {
            if (_currentTrackID >= 0 && _spawnedTracks.TryGetValue(_currentTrackID, out var track))
            {
                Destroy(track, _debugDestroyDelayTime);
                _spawnedTracks.Remove(_currentTrackID);
            }
            _currentTrackID++;
        }
        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.SplineSegmentCreated, OnSplineCreated);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.CurrentSplineChanged, OnActiveSplineChanged);
        }
    }
}