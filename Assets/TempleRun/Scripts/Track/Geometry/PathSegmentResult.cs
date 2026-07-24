using System.Collections.Generic;
using UnityEngine;

namespace CrawfisSoftware.TempleRun.Track.Geometry
{
    /// <summary>
    /// The result of building one segment (or the deferred exit half of an Either
    /// junction): the span(s) to publish, the pose the next segment continues from,
    /// and everything <see cref="PathProvider"/> needs to fill a
    /// <see cref="SegmentGeometryData"/> without re-deriving geometry.
    /// </summary>
    /// <remarks>
    /// The <see cref="ApproachStart"/>/<see cref="Pivot"/>/<see cref="ExitEnd"/> triple
    /// mirrors the old three-point geometry the transition controller consumes.
    /// <see cref="Spans"/> may be empty (an Either exit with no exit section) — in which
    /// case no <c>SplineSegmentCreated</c> is published, matching the legacy guard.
    /// </remarks>
    public readonly struct PathSegmentResult
    {
        /// <summary>Approach span(s) plus exit span(s); ordered as they should be published.</summary>
        public readonly IReadOnlyList<PathSpan> Spans;

        /// <summary>The pose the next segment starts from.</summary>
        public readonly PathPose ExitPose;

        /// <summary>The turn / placeholder point (goes to <c>SegmentGeometryData.Pivot</c>).</summary>
        public readonly Vector3 Pivot;

        /// <summary>Segment start (goes to <c>SegmentGeometryData.ApproachStart</c>).</summary>
        public readonly Vector3 ApproachStart;

        /// <summary>Where the exit sub-spline begins (goes to <c>SegmentGeometryData.ExitStart</c>).
        /// Equals <see cref="Pivot"/> except at a hard turn, where it is the laterally-shifted pivot
        /// the exit tiles start from.</summary>
        public readonly Vector3 ExitStart;

        /// <summary>Segment end (goes to <c>SegmentGeometryData.ExitEnd</c>).</summary>
        public readonly Vector3 ExitEnd;

        /// <summary>The direction stored in the published <c>SegmentGeometryData.Direction</c>.</summary>
        public readonly Direction GeometryDirection;

        /// <summary>Whether the exit half is resolved (false only for an Either approach).</summary>
        public readonly bool ExitResolved;

        public PathSegmentResult(IReadOnlyList<PathSpan> spans, PathPose exitPose, Vector3 pivot,
                                 Vector3 approachStart, Vector3 exitStart, Vector3 exitEnd,
                                 Direction geometryDirection, bool exitResolved)
        {
            Spans             = spans;
            ExitPose          = exitPose;
            Pivot             = pivot;
            ApproachStart     = approachStart;
            ExitStart         = exitStart;
            ExitEnd           = exitEnd;
            GeometryDirection = geometryDirection;
            ExitResolved      = exitResolved;
        }
    }
}
