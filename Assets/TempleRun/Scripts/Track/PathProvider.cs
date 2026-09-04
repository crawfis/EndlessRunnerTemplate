using System.Collections.Generic;

using UnityEngine;

using CrawfisSoftware.TempleRun.Track.Geometry;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Pure geometry orchestrator. Owns the running <see cref="PathPose"/>, sequence index,
    /// and Either-junction deferral, and delegates the actual segment *shape* to a pluggable
    /// <see cref="IPathSegmentBuilder"/> (default <see cref="AxisAligned90Builder"/>, which
    /// reproduces the legacy axis-aligned 90° geometry exactly). It publishes
    /// SplineSegmentCreated (for spawners) and SegmentGeometryReady (for
    /// SegmentTransitionController).
    ///    Dependencies: IPathSegmentBuilder, EventsFor<TempleRunEvents>
    ///    Subscribes: TrackSegmentCreated — builds segment geometry
    ///    Subscribes: SegmentRequested — completes Either junction exit geometry
    ///    Publishes: SplineSegmentCreated (data: SplineSegmentData) — per sub-spline, for spawners
    ///    Publishes: SegmentGeometryReady (data: SegmentGeometryData) — full segment geometry
    /// </summary>
    /// <remarks>
    /// Execution order -10 ensures SegmentRequested is processed here (updating the pose)
    /// before TrackManager (default order 0) publishes new TrackSegmentCreated events.
    /// </remarks>
    [DefaultExecutionOrder(-10)]
    public class PathProvider : MonoBehaviour
    {
        [Tooltip("The initial position of the starting track and the character")]
        [SerializeField] private Vector3 _anchorPoint = Vector3.zero;

        // Pluggable geometry policy. Default reproduces the legacy behaviour bit-for-bit.
        private readonly IPathSegmentBuilder _builder = new AxisAligned90Builder();

        // Running pose (position + heading). Replaces the old (_anchorPoint, _directionIndex):
        // the initial heading +Z corresponds to the old _directionIndex == 0.
        private PathPose _pose;
        private int _sequenceIndex = 0;

        // Holds the definition + pose of an active Either segment while waiting for
        // SegmentRequested to provide the chosen exit direction.
        private TrackSegmentDefinition _pendingEitherDefinition;
        private int _pendingEitherSequenceIndex;
        private PathPose _pendingEitherPivotPose;

        private void Awake()
        {
            _pose = new PathPose(_anchorPoint, new Vector3(0, 0, 1), new Vector3(0, 1, 0));

            TempleRunBus.Subscribe(TempleRunEvents.TrackSegmentCreated, OnTrackCreated);
            TempleRunBus.Subscribe(TempleRunEvents.SegmentRequested, OnSegmentRequested);
        }

        private void OnDestroy()
        {
            TempleRunBus.Unsubscribe(TempleRunEvents.TrackSegmentCreated, OnTrackCreated);
            TempleRunBus.Unsubscribe(TempleRunEvents.SegmentRequested, OnSegmentRequested);
        }

        private void OnTrackCreated(string eventName, object sender, object data)
        {
            var segment = (TrackSegmentInfo)data;
            var definition = segment.Definition;
            int seqIdx = _sequenceIndex++;

            PathSegmentResult result = _builder.Build(_pose, definition, segment.Direction);

            if (segment.Direction == Direction.Either)
            {
                // Approach only — stash the pivot pose so the exit resolves identically later.
                _pendingEitherDefinition   = definition;
                _pendingEitherSequenceIndex = seqIdx;
                _pendingEitherPivotPose    = result.ExitPose;
            }

            // Advance state before publishing, for the same reason as OnSegmentRequested: a publish
            // re-enters the drain, so anything it reaches must see this segment already accounted for.
            _pose = result.ExitPose;

            PublishSpans(result);
            PublishGeometry(result, definition, seqIdx);
        }

        /// <summary>
        /// Fires when the player commits a turn direction at an Either junction.
        /// Completes the exit geometry and publishes the exit SplineSegmentCreated.
        /// </summary>
        private void OnSegmentRequested(string eventName, object sender, object data)
        {
            var chosen = (Direction)data;
            if (_pendingEitherDefinition == null)
            {
                return;
            }

            TrackSegmentDefinition definition = _pendingEitherDefinition;
            int seqIdx = _pendingEitherSequenceIndex;

            PathSegmentResult result =
                _builder.BuildEitherExit(_pendingEitherPivotPose, definition, chosen);

            // Advance our own state BEFORE publishing anything. Publishing re-enters the dispatch
            // drain, and SegmentRequested's other subscriber - TrackManager - is still queued behind
            // us, so it generates the next segments from _pose during PublishSpans. Setting _pose
            // afterwards built everything past the junction from the stale pivot, i.e. straight on
            // instead of down the branch the player just chose.
            _pose = result.ExitPose;
            _pendingEitherDefinition = null;

            PublishSpans(result);
            PublishGeometry(result, definition, seqIdx);
        }

        /// <summary>
        /// Publishes one SplineSegmentCreated per consecutive point pair of every span.
        /// A two-point span therefore publishes exactly one spline (Point1 = Points[0],
        /// Point2 = Points[^1]) — identical to the legacy per-sub-spline publishing.
        /// </summary>
        private void PublishSpans(PathSegmentResult result)
        {
            IReadOnlyList<PathSpan> spans = result.Spans;
            for (int s = 0; s < spans.Count; s++)
            {
                PathSpan span = spans[s];
                IReadOnlyList<Vector3> points = span.Points;
                for (int p = 0; p < points.Count - 1; p++)
                {
                    var splineData = new SplineSegmentData(
                        points[p], points[p + 1], span.EndDirection, span.Definition);
                    TempleRunBus.Publish(
                        TempleRunEvents.SplineSegmentCreated, this, splineData);
                }
            }
        }

        private void PublishGeometry(PathSegmentResult result, TrackSegmentDefinition definition, int seqIdx)
        {
            var geometry = new SegmentGeometryData
            {
                ApproachStart = result.ApproachStart,
                Pivot         = result.Pivot,
                ExitStart     = result.ExitStart,
                ExitEnd       = result.ExitEnd,
                Direction     = result.GeometryDirection,
                Definition    = definition,
                SequenceIndex = seqIdx,
                ExitResolved  = result.ExitResolved
            };

            TempleRunBus.Publish(
                TempleRunEvents.SegmentGeometryReady, this, geometry);
        }
    }
}
