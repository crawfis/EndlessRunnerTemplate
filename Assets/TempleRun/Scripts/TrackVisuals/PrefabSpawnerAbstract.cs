using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    public abstract class PrefabSpawnerAbstract : MonoBehaviour
    {
        [Tooltip("The prefab should have it's origin at the bottom-center with positive z-axis being the forward direction.")]
        [SerializeField] protected GameObject _prefab;
        [SerializeField] protected float _widthScale = 1;
        [SerializeField] protected float _lengthScale = 1.0f;
        [Tooltip("Delete any older track segments keeping them alive for this duration.")]
        [SerializeField] protected float _debugDestroyDelayTime = 0.05f;

        protected Transform _parentTransform;

        // Spawned track objects grouped by the owning segment's sequence index. A segment
        // publishes one SplineSegmentCreated per sub-spline — two for a turn (approach + exit),
        // one for a straight — so the group, not the individual object, is the unit of deletion.
        protected readonly Dictionary<int, List<GameObject>> _spawnedTracks = new();

        // Objects spawned since the last SegmentGeometryReady, i.e. the sub-splines of the
        // segment currently being built. Claimed by that event, which carries the sequence index.
        private readonly List<GameObject> _pendingTracks = new();

        // Sequence index of the next segment to destroy. Starts at -1 so the first
        // ActiveTrackChanged destroys nothing, keeping the just-exited segment alive for one
        // more segment rather than popping out from under the player.
        protected int _currentTrackID = -1;
        protected int _trackNumber = 0;
        protected virtual void Awake()
        {
            SubscribeToEvents();
        }

        protected void SubscribeToEvents()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.SplineSegmentCreated, OnSplineCreated);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.SegmentGeometryReady, OnSegmentGeometryReady);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.ActiveTrackChanged, OnActiveSplineChanged);
            var parent = new GameObject("Generated Level");
            _parentTransform = parent.transform;
        }

        private void OnSplineCreated(string eventName, object sender, object data)
        {
            var splineSegment = (SplineSegmentData)data;
            Vector3 point1 = splineSegment.Point1;
            Vector3 point2 = splineSegment.Point2;
            Direction turnDirection = splineSegment.EndDirection;
            Vector3 direction = (point2 - point1);
            Vector3 unitDirection = direction.normalized;
            // Rotation to look at point 2
            Quaternion rotation = Quaternion.LookRotation(unitDirection);
            var track = new GameObject(string.Format("Track {0:D2}", _trackNumber));
            _pendingTracks.Add(track);
            Transform trackTransform = track.transform;
            trackTransform.parent = _parentTransform;
            trackTransform.SetLocalPositionAndRotation(point1, rotation);
            CreateTrack(direction.magnitude, trackTransform, turnDirection);
            _trackNumber++;
        }

        /// <summary>
        /// Closes the current batch: every object spawned since the previous segment now belongs
        /// to this segment's sequence index. Fires once per segment, immediately after that
        /// segment's SplineSegmentCreated events (see PathProvider.OnTrackCreated). An Either
        /// junction publishes geometry twice for the SAME index — approach, then exit once the
        /// player commits — so the batches accumulate into one group rather than replacing it.
        /// </summary>
        private void OnSegmentGeometryReady(string eventName, object sender, object data)
        {
            if (_pendingTracks.Count == 0) return;

            var geometry = (SegmentGeometryData)data;
            if (!_spawnedTracks.TryGetValue(geometry.SequenceIndex, out var tracks))
            {
                tracks = new List<GameObject>();
                _spawnedTracks[geometry.SequenceIndex] = tracks;
            }
            tracks.AddRange(_pendingTracks);
            _pendingTracks.Clear();
        }

        protected abstract void CreateTrack(float length, Transform trackTransform, Direction endCapDirection);

        /// <summary>
        /// Fires once per segment, when the player has fully exited it. Destroys every object
        /// belonging to that segment — previously this destroyed a single object per segment,
        /// so a turn (two sub-splines) leaked one object each time and the delete cursor fell
        /// permanently behind the spawn cursor.
        /// </summary>
        private void OnActiveSplineChanged(string eventName, object sender, object data)
        {
            if (_currentTrackID >= 0 && _spawnedTracks.TryGetValue(_currentTrackID, out var tracks))
            {
                for (int i = 0; i < tracks.Count; i++)
                    StartCoroutine(DeactivateCoroutine(tracks[i], _debugDestroyDelayTime));
                _spawnedTracks.Remove(_currentTrackID);
            }
            _currentTrackID++;
        }
        private IEnumerator DeactivateCoroutine(GameObject target, float delay)
        {
            yield return new WaitForSeconds(delay);
            target?.SetActive(false);
            Destroy(target);
        }
        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.SplineSegmentCreated, OnSplineCreated);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.SegmentGeometryReady, OnSegmentGeometryReady);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.ActiveTrackChanged, OnActiveSplineChanged);
        }
    }
}