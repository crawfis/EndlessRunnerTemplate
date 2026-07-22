using System.Collections.Generic;
using UnityEngine;

namespace CrawfisSoftware.TempleRun.Track.Geometry
{
    /// <summary>
    /// One polyline span that realizes (part of) a segment. Straight geometry uses two
    /// points; curved geometry samples the curve into a polyline (>= 2 ordered points).
    /// <see cref="PathProvider"/> publishes one <c>SplineSegmentCreated</c> per consecutive
    /// point pair, so downstream spawners/visuals — which assume straight start→end spans —
    /// keep working unchanged.
    /// </summary>
    public readonly struct PathSpan
    {
        /// <summary>Ordered points from start to end. Always contains at least two entries.</summary>
        public readonly IReadOnlyList<Vector3> Points;

        /// <summary>The turn direction reported for this span's published spline(s).</summary>
        public readonly Direction EndDirection;

        /// <summary>The segment definition this span belongs to (carried for spawners).</summary>
        public readonly TrackSegmentDefinition Definition;

        public PathSpan(IReadOnlyList<Vector3> points, Direction endDirection,
                        TrackSegmentDefinition definition)
        {
            Points       = points;
            EndDirection = endDirection;
            Definition   = definition;
        }
    }
}
