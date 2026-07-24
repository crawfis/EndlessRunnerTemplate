using System.Collections.Generic;
using UnityEngine;

namespace CrawfisSoftware.TempleRun.Track.Geometry
{
    /// <summary>
    /// Rounded-corner geometry builder. When a Left/Right turn (or resolved Either exit)
    /// has <see cref="TrackSegmentDefinition.TurnRadius"/> &gt; 0, the single straight exit
    /// span is replaced by a sampled quarter-circle arc of that radius, emitted as a handful
    /// of short straight <see cref="PathSpan"/>s (polyline approximation). Every other case —
    /// and any turn with <c>TurnRadius &lt;= 0</c> — delegates to <see cref="AxisAligned90Builder"/>,
    /// so behaviour is bit-identical to the default builder unless a segment opts in.
    ///    Determinism: pure function of its inputs (no random, no time).
    /// </summary>
    /// <remarks>
    /// The post-turn heading is taken from the exact cardinal rotation (not from the sampled
    /// trig) so subsequent segments stay perfectly on-grid. Only the intermediate arc-sample
    /// positions use trig. Downstream consumers see only extra 2-point straight spans, so no
    /// spawner/movement change is required; the movement/teleport code still treats each span
    /// as straight, which is the documented drift caveat deferred to plan phase C4.
    /// </remarks>
    public sealed class ArcTurnBuilder : IPathSegmentBuilder
    {
        private readonly AxisAligned90Builder _fallback = new AxisAligned90Builder();
        private readonly int _arcSegments;

        /// <param name="arcSegments">Number of straight chords used to approximate the quarter arc.</param>
        public ArcTurnBuilder(int arcSegments = 8)
        {
            _arcSegments = Mathf.Max(1, arcSegments);
        }

        /// <inheritdoc />
        public PathSegmentResult Build(PathPose entry, TrackSegmentDefinition segment, Direction resolved)
        {
            if (segment.TurnRadius <= 0f || (resolved != Direction.Left && resolved != Direction.Right))
                return _fallback.Build(entry, segment, resolved);

            Vector3 forward     = entry.Forward;
            Vector3 entrance    = entry.Position;
            Vector3 approachEnd = entrance + segment.ToPivotDistance * forward;
            var     approachSpan = new PathSpan(new[] { entrance, approachEnd }, Direction.Straight, segment);

            BuildArc(approachEnd, forward, entry.Up, resolved, segment,
                     out List<PathSpan> arcSpans, out Vector3 arcEnd, out Vector3 newForward);

            var spans = new List<PathSpan>(1 + arcSpans.Count) { approachSpan };
            spans.AddRange(arcSpans);

            var exitPose = new PathPose(arcEnd, newForward, entry.Up);
            // Arc corners curve smoothly from the pivot, so the exit starts at the pivot (no lateral
            // shift): ExitStart == Pivot.
            return new PathSegmentResult(
                spans, exitPose, pivot: approachEnd,
                approachStart: entrance, exitStart: approachEnd, exitEnd: arcEnd,
                geometryDirection: resolved, exitResolved: true);
        }

        /// <inheritdoc />
        public PathSegmentResult BuildEitherExit(PathPose atPivot, TrackSegmentDefinition segment, Direction chosen)
        {
            if (segment.TurnRadius <= 0f || (chosen != Direction.Left && chosen != Direction.Right))
                return _fallback.BuildEitherExit(atPivot, segment, chosen);

            Vector3 forward    = atPivot.Forward;
            int     newIndex   = CardinalDirections.Rotate(CardinalDirections.IndexOf(forward), chosen);
            Vector3 newForward = CardinalDirections.Axes[newIndex];

            // Apply the same centering nudge the default builder uses at an Either exit.
            float   offset      = Blackboard.Instance.TrackWidthOffset;
            Vector3 nudgedPivot = atPivot.Position - offset * forward + offset * newForward;

            BuildArc(nudgedPivot, forward, atPivot.Up, chosen, segment,
                     out List<PathSpan> arcSpans, out Vector3 arcEnd, out Vector3 _);

            var exitPose = new PathPose(arcEnd, newForward, atPivot.Up);
            Vector3 approachStart = nudgedPivot - segment.ToPivotDistance * newForward;
            return new PathSegmentResult(
                arcSpans, exitPose, pivot: nudgedPivot,
                approachStart: approachStart, exitStart: nudgedPivot, exitEnd: arcEnd,
                geometryDirection: chosen, exitResolved: true);
        }

        /// <summary>
        /// Samples a 90° arc of <see cref="TrackSegmentDefinition.TurnRadius"/> starting at
        /// <paramref name="start"/> heading <paramref name="forward"/>, turning
        /// <paramref name="turn"/>. Emits <c>_arcSegments</c> straight chord spans; the exit
        /// heading is the exact cardinal rotation of <paramref name="forward"/>.
        /// </summary>
        private void BuildArc(Vector3 start, Vector3 forward, Vector3 up, Direction turn,
                              TrackSegmentDefinition segment,
                              out List<PathSpan> spans, out Vector3 arcEnd, out Vector3 newForward)
        {
            int newIndex = CardinalDirections.Rotate(CardinalDirections.IndexOf(forward), turn);
            newForward   = CardinalDirections.Axes[newIndex];

            float   radius = segment.TurnRadius;
            Vector3 right  = Vector3.Cross(up, forward).normalized;   // +X for forward +Z
            float   sign   = (turn == Direction.Right) ? 1f : -1f;    // Right curves toward +right
            Vector3 center = start + sign * radius * right;
            Vector3 radial = start - center;                          // center → start vector
            float   sweep  = sign * 90f;                              // degrees about up

            var points = new List<Vector3>(_arcSegments + 1) { start };
            for (int i = 1; i <= _arcSegments; i++)
            {
                float      t   = (float)i / _arcSegments;
                Quaternion rot = Quaternion.AngleAxis(sweep * t, up);
                points.Add(center + rot * radial);
            }

            spans = new List<PathSpan>(_arcSegments);
            for (int i = 0; i < points.Count - 1; i++)
                spans.Add(new PathSpan(new[] { points[i], points[i + 1] }, turn, segment));

            arcEnd = points[points.Count - 1];
        }
    }
}
