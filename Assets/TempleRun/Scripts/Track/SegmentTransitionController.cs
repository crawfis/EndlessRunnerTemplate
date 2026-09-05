using System.Collections.Generic;

using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Bridges turn events + cached geometry into path-change events for
    /// TeleportController, CharacterTeleporter, and MoveCharacterByDistance.
    ///    Dependencies: EventsFor<TempleRunEvents>
    ///    Subscribes: SegmentGeometryReady — caches geometry by sequence index
    ///    Subscribes: ActiveTrackChanging — publishes CurrentSplineChanging (approach sub-spline)
    ///    Subscribes: TurnLeftStarted, TurnRightStarted — publishes CurrentSplineChanging (exit sub-spline);
    ///                the teleport onto it is the turn's duration, and ends with Turn*Ending
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

        // The active segment's message, kept for its run-absolute distances. TrackManager stamps
        // those at creation, so this class no longer keeps a private running sum of segment lengths.
        private TrackSegmentInfo _activeSegment;

        private void Awake()
        {
            TempleRunBus.Subscribe(TempleRunEvents.SegmentGeometryReady, OnGeometryReady);
            TempleRunBus.Subscribe(TempleRunEvents.ActiveTrackChanging, OnTrackChanging);
            TempleRunBus.Subscribe(TempleRunEvents.TurnLeftStarted, OnTurnStarted);
            TempleRunBus.Subscribe(TempleRunEvents.TurnRightStarted, OnTurnStarted);
            TempleRunBus.Subscribe(TempleRunEvents.SegmentExited, OnSegmentExited);
        }

        private void OnDestroy()
        {
            TempleRunBus.Unsubscribe(TempleRunEvents.SegmentGeometryReady, OnGeometryReady);
            TempleRunBus.Unsubscribe(TempleRunEvents.ActiveTrackChanging, OnTrackChanging);
            TempleRunBus.Unsubscribe(TempleRunEvents.TurnLeftStarted, OnTurnStarted);
            TempleRunBus.Unsubscribe(TempleRunEvents.TurnRightStarted, OnTurnStarted);
            TempleRunBus.Unsubscribe(TempleRunEvents.SegmentExited, OnSegmentExited);
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
            _activeSegment = segmentInfo;
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
            TempleRunBus.Publish(
                TempleRunEvents.CurrentSplineChanging, this, approachSpline);
        }

        /// <summary>
        /// Fires when a turn completes. Publishes CurrentSplineChanging with the exit
        /// sub-spline truncated to TeleportDistance.
        /// </summary>
        private void OnTurnStarted(string eventName, object sender, object data)
        {
            _isOnExitSection = true;
            // _activeGeometry is always current: Either junction updates are handled directly
            // in OnGeometryReady when geometry.SequenceIndex == _activeGeometry.SequenceIndex.

            // The exit runs from ExitStart (the laterally-shifted pivot the exit tiles begin at),
            // not Pivot (the centre-line approach end). Anchoring the exit sub-spline here puts the
            // player on the tiles and lines its end up with the next segment — no sideways jump.
            // Truncate to TeleportDistance.
            // Geometry supplies the points, the segment message supplies the distances - the same
            // TeleportDistance on both lines, so where the player lands and how far that is agree.
            Vector3 exitDir = (_activeGeometry.ExitEnd - _activeGeometry.ExitStart).normalized;
            Vector3 teleportLanding = _activeGeometry.ExitStart + exitDir * _activeSegment.TeleportDistance;

            // PivotDistance is run-absolute; TeleportDistance is a length past the pivot, so it
            // stays relative and is added on.
            float landingDistance = _activeSegment.PivotDistance + _activeSegment.TeleportDistance;

            var exitSpline = (_activeGeometry.ExitStart, teleportLanding, _activeGeometry.Direction, landingDistance);
            TempleRunBus.Publish(
                TempleRunEvents.CurrentSplineChanging, this, exitSpline);
        }

        private void OnSegmentExited(string eventName, object sender, object data)
        {
            // Publish the current sub-spline as "changed" (transition complete).
            float landingDistance = _activeSegment.EndDistance;
            var currentSpline = _isOnExitSection
                ? (_activeGeometry.ExitStart, _activeGeometry.ExitEnd, _activeGeometry.Direction, landingDistance)
                : (_activeGeometry.ApproachStart, _activeGeometry.Pivot, Direction.Straight, landingDistance);

            TempleRunBus.Publish(
                TempleRunEvents.CurrentSplineChanged, this, currentSpline);

            _isOnExitSection = false;
            // No cache cleanup needed: geometry is removed from the cache in OnTrackChanging
            // the moment it is consumed, so stale entries cannot accumulate.
        }

        private float ComputeLandingDistance(TrackSegmentInfo segmentInfo)
        {
            if (segmentInfo.Direction == Direction.Straight)
                return 0f; // No teleport for straights.

            return segmentInfo.TurnFailureDistance + segmentInfo.TeleportDistance;
        }
    }
}
