using UnityEngine;

namespace CrawfisSoftware.TempleRun.Track.Geometry
{
    /// <summary>
    /// Default geometry builder that reproduces the legacy <c>PathProvider</c> behaviour
    /// exactly: two-point straight spans, 90° cardinal turn snaps, and the
    /// <c>± TrackWidthOffset</c> centering nudge applied at an Either-junction exit.
    ///    Dependencies: <see cref="Blackboard.TrackWidthOffset"/> (read at Either-exit build,
    ///    exactly as the old <c>ApplyTurn</c> read it at <c>SegmentRequested</c> time).
    ///    Determinism: pure function of its inputs (no random, no time).
    /// </summary>
    /// <remarks>
    /// Mapping from the old <c>OnTrackCreated</c> switch:
    ///  - Straight  → one span entrance → entrance + EntranceDistance·F.
    ///  - Left/Right → approach span entrance → approachEnd, then exit span approachEnd →
    ///    approachEnd + ExitDistance·F' (F' = rotated axis). The old code set
    ///    <c>adjustedPivot = approachEnd</c>, so the ApplyTurn centering nudge did NOT move
    ///    the exit; it is intentionally omitted here to stay bit-identical.
    ///  - Either (approach) → one span entrance → pivot; heading unchanged; exit deferred.
    ///  - Either (exit, via <see cref="BuildEitherExit"/>) → the old <c>OnSegmentRequested</c>:
    ///    <c>adjustedPivot = _anchorPoint</c> AFTER the nudge, so the nudge IS applied here;
    ///    exit span is emitted only when ExitDistance &gt; 0.
    /// </remarks>
    public sealed class AxisAligned90Builder : IPathSegmentBuilder
    {
        /// <inheritdoc />
        public PathSegmentResult Build(PathPose entry, TrackSegmentDefinition segment, Direction resolved)
        {
            switch (resolved)
            {
                case Direction.Left:
                case Direction.Right:
                    return BuildTurn(entry, segment, resolved);
                case Direction.Either:
                    return BuildEitherApproach(entry, segment);
                case Direction.Straight:
                default:
                    return BuildStraight(entry, segment);
            }
        }

        private static PathSegmentResult BuildStraight(PathPose entry, TrackSegmentDefinition segment)
        {
            Vector3 forward  = entry.Forward;
            Vector3 entrance = entry.Position;
            Vector3 exit     = entrance + segment.EntranceDistance * forward;

            var span     = new PathSpan(new[] { entrance, exit }, Direction.Straight, segment);
            var exitPose = new PathPose(exit, forward, entry.Up);

            return new PathSegmentResult(
                new[] { span }, exitPose, pivot: exit,
                approachStart: entrance, exitEnd: exit,
                geometryDirection: Direction.Straight, exitResolved: true);
        }

        private static PathSegmentResult BuildTurn(PathPose entry, TrackSegmentDefinition segment, Direction resolved)
        {
            Vector3 forward     = entry.Forward;
            Vector3 entrance    = entry.Position;
            Vector3 approachEnd = entrance + segment.EntranceDistance * forward;

            int     newIndex   = CardinalDirections.Rotate(CardinalDirections.IndexOf(forward), resolved);
            Vector3 newForward = CardinalDirections.Axes[newIndex];

            // Legacy: adjustedPivot = approachEnd (the ApplyTurn nudge is discarded for Left/Right).
            Vector3 adjustedPivot = approachEnd;
            Vector3 exit          = adjustedPivot + segment.ExitDistance * newForward;

            var approachSpan = new PathSpan(new[] { entrance, approachEnd }, Direction.Straight, segment);
            var exitSpan     = new PathSpan(new[] { adjustedPivot, exit }, resolved, segment);
            var exitPose     = new PathPose(exit, newForward, entry.Up);

            return new PathSegmentResult(
                new[] { approachSpan, exitSpan }, exitPose, pivot: adjustedPivot,
                approachStart: entrance, exitEnd: exit,
                geometryDirection: resolved, exitResolved: true);
        }

        private static PathSegmentResult BuildEitherApproach(PathPose entry, TrackSegmentDefinition segment)
        {
            Vector3 forward  = entry.Forward;
            Vector3 entrance = entry.Position;
            Vector3 pivot    = entrance + segment.EntranceDistance * forward;

            var span = new PathSpan(new[] { entrance, pivot }, Direction.Either, segment);
            // Heading unchanged; the exit is resolved later from this pivot pose.
            var exitPose = new PathPose(pivot, forward, entry.Up);

            return new PathSegmentResult(
                new[] { span }, exitPose, pivot: pivot,
                approachStart: entrance, exitEnd: pivot,
                geometryDirection: Direction.Either, exitResolved: false);
        }

        /// <inheritdoc />
        public PathSegmentResult BuildEitherExit(PathPose atPivot, TrackSegmentDefinition segment, Direction chosen)
        {
            Vector3 forward    = atPivot.Forward;
            int     newIndex   = CardinalDirections.Rotate(CardinalDirections.IndexOf(forward), chosen);
            Vector3 newForward = CardinalDirections.Axes[newIndex];

            // Legacy ApplyTurn at SegmentRequested: subtract offset·oldAxis, rotate, add offset·newAxis.
            float   offset      = Blackboard.Instance.TrackWidthOffset;
            Vector3 nudgedPivot = atPivot.Position - offset * forward + offset * newForward;
            Vector3 exit        = nudgedPivot + segment.ExitDistance * newForward;

            // Legacy guard: the exit spline is published only when ExitDistance > 0.
            var spans = segment.ExitDistance > 0f
                ? new[] { new PathSpan(new[] { nudgedPivot, exit }, chosen, segment) }
                : System.Array.Empty<PathSpan>();

            var exitPose = new PathPose(exit, newForward, atPivot.Up);

            // Legacy: ApproachStart is approximated as nudgedPivot - EntranceDistance·newAxis.
            Vector3 approachStart = nudgedPivot - segment.EntranceDistance * newForward;

            return new PathSegmentResult(
                spans, exitPose, pivot: nudgedPivot,
                approachStart: approachStart, exitEnd: exit,
                geometryDirection: chosen, exitResolved: true);
        }
    }
}
