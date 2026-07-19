using System.Collections.Generic;
using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Bridges turn events + cached geometry into path-change events for
    /// TeleportController, CharacterTeleporter, and MoveCharacterByDistance.
    ///    Dependencies: EventsPublisherTempleRun
    ///    Subscribes: SegmentGeometryReady — caches geometry by sequence index
    ///    Subscribes: ActiveTrackChanging — publishes CurrentSplineChanging (approach sub-spline)
    ///    Subscribes: TurnLeftCompleted, TurnRightCompleted — publishes CurrentSplineChanging (exit sub-spline)
    ///    Subscribes: SegmentExited — publishes CurrentSplineChanged
    ///    Publishes: CurrentSplineChanging (data: (Vector3, Vector3, Direction, float landingDistance))
    ///    Publishes: CurrentSplineChanged (data: (Vector3, Vector3, Direction, float landingDistance))
    /// </summary>
    [DefaultExecutionOrder(-5)]
    internal class SegmentTransitionController : MonoBehaviour
    {
        // Geometry cache ordered by sequence index (FIFO).
        private readonly SortedList<int, SegmentGeometryData> _geometryCache = new();

        // The geometry for the currently active segment.
        private SegmentGeometryData _activeGeometry;
        private bool _hasActiveGeometry = false;  // true once the first segment is activated
        private bool _isOnExitSection = false;

        // Tracks how many segments have been activated (consumed from the cache).
        private int _activatedCount = 0;

        // Cumulative distance at the START of the current segment.
        private float _segmentStartDistance = 0f;
        private float _previousSegmentLength = 0f;

        private void Awake()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.SegmentGeometryReady, OnGeometryReady);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.ActiveTrackChanging, OnTrackChanging);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.TurnLeftCompleted, OnTurnCompleted);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.TurnRightCompleted, OnTurnCompleted);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.SegmentExited, OnSegmentExited);
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.SegmentGeometryReady, OnGeometryReady);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.ActiveTrackChanging, OnTrackChanging);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.TurnLeftCompleted, OnTurnCompleted);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.TurnRightCompleted, OnTurnCompleted);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.SegmentExited, OnSegmentExited);
        }

        private void OnGeometryReady(string eventName, object sender, object data)
        {
            var geometry = (SegmentGeometryData)data;
            // If this is an update to the currently active segment (Either junction resolution),
            // update _activeGeometry in-place rather than storing in the cache.
            // Guard with _hasActiveGeometry to avoid the startup false-positive where the default
            // SequenceIndex (0) would match the first incoming geometry before activation.
            if (_hasActiveGeometry && geometry.SequenceIndex == _activeGeometry.SequenceIndex)
            {
                _activeGeometry = geometry;
                return;
            }
            _geometryCache[geometry.SequenceIndex] = geometry;
        }

        private void OnTrackChanging(string eventName, object sender, object data)
        {
            var segmentInfo = (TrackSegmentInfo)data;
            _segmentStartDistance += _previousSegmentLength;
            _previousSegmentLength = segmentInfo.Length;
            _isOnExitSection = false;

            // Pop the next geometry in FIFO order (lowest sequence index = next segment).
            // Remove immediately so the cache never contains already-consumed geometry.
            if (_geometryCache.Count > 0)
            {
                _activeGeometry = _geometryCache.Values[0];
                _geometryCache.RemoveAt(0);
                _hasActiveGeometry = true;
                _activatedCount++;
            }

            // Publish approach sub-spline (Entrance -> Pivot, direction Straight).
            float landingDistance = ComputeLandingDistance(segmentInfo);
            var approachSpline = (_activeGeometry.ApproachStart, _activeGeometry.Pivot, Direction.Straight, landingDistance);
            EventsPublisherTempleRun.Instance.PublishEvent(
                TempleRunEvents.CurrentSplineChanging, this, approachSpline);
        }

        /// <summary>
        /// Fires when a turn completes. Publishes CurrentSplineChanging with the exit
        /// sub-spline truncated to TeleportDistance.
        /// </summary>
        private void OnTurnCompleted(string eventName, object sender, object data)
        {
            _isOnExitSection = true;
            // _activeGeometry is always current: Either junction updates are handled directly
            // in OnGeometryReady when geometry.SequenceIndex == _activeGeometry.SequenceIndex.

            // Truncate exit spline to TeleportDistance.
            Vector3 exitDir = (_activeGeometry.ExitEnd - _activeGeometry.Pivot).normalized;
            Vector3 teleportLanding = _activeGeometry.Pivot + exitDir * _activeGeometry.Definition.TeleportDistance;

            float landingDistance = _segmentStartDistance
                + _activeGeometry.Definition.EntranceDistance
                + _activeGeometry.Definition.TeleportDistance;

            var exitSpline = (_activeGeometry.Pivot, teleportLanding, _activeGeometry.Direction, landingDistance);
            EventsPublisherTempleRun.Instance.PublishEvent(
                TempleRunEvents.CurrentSplineChanging, this, exitSpline);
        }

        private void OnSegmentExited(string eventName, object sender, object data)
        {
            // Publish the current sub-spline as "changed" (transition complete).
            float landingDistance = _segmentStartDistance + _previousSegmentLength;
            var currentSpline = _isOnExitSection
                ? (_activeGeometry.Pivot, _activeGeometry.ExitEnd, _activeGeometry.Direction, landingDistance)
                : (_activeGeometry.ApproachStart, _activeGeometry.Pivot, Direction.Straight, landingDistance);

            EventsPublisherTempleRun.Instance.PublishEvent(
                TempleRunEvents.CurrentSplineChanged, this, currentSpline);

            _isOnExitSection = false;
            // No cache cleanup needed: geometry is removed from the cache in OnTrackChanging
            // the moment it is consumed, so stale entries cannot accumulate.
        }

        private float ComputeLandingDistance(TrackSegmentInfo segmentInfo)
        {
            if (segmentInfo.Direction == Direction.Straight)
                return 0f; // No teleport for straights.

            return _segmentStartDistance
                + segmentInfo.TurnPointDistance
                + segmentInfo.TeleportDistance;
        }
    }
}
