using UnityEngine;

namespace CrawfisSoftware.TempleRun.Track.Geometry
{
    /// <summary>
    /// Default geometry builder: two-point straight spans and 90° cardinal turn snaps. At a turn the
    /// exit (tiles, exit sub-spline and the pose seeding the next segment) is shifted by
    /// <c>TrackWidthOffset</c> so the exit tiles butt flush against the approach at the corner, while
    /// <c>Pivot</c> — the end of the straight approach the player runs — stays on the centre line.
    /// <c>ExitStart</c> carries the shifted point so the player's lane-aware teleport crosses from
    /// the centre-line pivot onto the tiles at the corner (approach stays straight, exit lands on
    /// the tiles, no sideways jump). Hard Left/Right and deferred Either share this — see
    /// <see cref="BuildEitherExit"/>.
    ///    Dependencies: <see cref="Blackboard.TrackWidthOffset"/> (read at turn-exit build,
    ///    exactly as the old <c>ApplyTurn</c> read it at <c>SegmentRequested</c> time).
    ///    Determinism: pure function of its inputs (no random, no time).
    /// </summary>
    /// <remarks>
    /// Mapping from the old <c>OnTrackCreated</c> switch:
    ///  - Straight  → one span entrance → entrance + ToPivotDistance·F.
    ///  - Left/Right → approach span entrance → approachEnd, then exit span from the nudged pivot
    ///    (approachEnd − offset·F + offset·F') → nudged pivot + ExitDistance·F' (F' = rotated axis).
    ///    Pivot stays on the centre line (approachEnd) for the straight approach; ExitStart/ExitEnd
    ///    are the nudged pivot / nudged exit, and exitPose = nudged exit seeds the next segment. The
    ///    player teleports Pivot → ExitStart at the corner, so approach is straight and the exit
    ///    runs along the tiles.
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
            Vector3 exit     = entrance + segment.ToPivotDistance * forward;

            var span     = new PathSpan(new[] { entrance, exit }, Direction.Straight, segment);
            var exitPose = new PathPose(exit, forward, entry.Up);

            return new PathSegmentResult(
                new[] { span }, exitPose, pivot: exit,
                approachStart: entrance, exitStart: exit, exitEnd: exit,
                geometryDirection: Direction.Straight, exitResolved: true);
        }

        private static PathSegmentResult BuildTurn(PathPose entry, TrackSegmentDefinition segment, Direction resolved)
        {
            Vector3 forward     = entry.Forward;
            Vector3 entrance    = entry.Position;
            Vector3 approachEnd = entrance + segment.ToPivotDistance * forward;

            int     newIndex   = CardinalDirections.Rotate(CardinalDirections.IndexOf(forward), resolved);
            Vector3 newForward = CardinalDirections.Axes[newIndex];

            // Shift the exit by ±TrackWidthOffset (same nudge as BuildEitherExit) so the exit tiles
            // butt flush against the approach at the corner. The exit sub-spline, the exit tiles and
            // the running pose that seeds the next segment all live on this shifted line, so they
            // stay mutually consistent — no gap into the next segment.
            float   offset      = Blackboard.Instance.TrackWidthOffset;
            Vector3 nudgedPivot = approachEnd - offset * forward + offset * newForward;
            Vector3 nudgedExit  = nudgedPivot + segment.ExitDistance * newForward;

            var approachSpan = new PathSpan(new[] { entrance, approachEnd }, Direction.Straight, segment);
            var exitSpan     = new PathSpan(new[] { nudgedPivot, nudgedExit }, resolved, segment);
            var exitPose     = new PathPose(nudgedExit, newForward, entry.Up);

            // Pivot stays on the centre line: it is the end of the straight approach the player runs
            // (ApproachStart -> Pivot). ExitStart/ExitEnd are the shifted line the exit runs along.
            // The player crosses from Pivot to ExitStart via its lane-aware teleport at the corner,
            // so the approach is straight AND the exit lands on the tiles with no sideways jump.
            return new PathSegmentResult(
                new[] { approachSpan, exitSpan }, exitPose, pivot: approachEnd,
                approachStart: entrance, exitStart: nudgedPivot, exitEnd: nudgedExit,
                geometryDirection: resolved, exitResolved: true);
        }

        private static PathSegmentResult BuildEitherApproach(PathPose entry, TrackSegmentDefinition segment)
        {
            Vector3 forward  = entry.Forward;
            Vector3 entrance = entry.Position;
            Vector3 pivot    = entrance + segment.ToPivotDistance * forward;

            var span = new PathSpan(new[] { entrance, pivot }, Direction.Either, segment);
            // Heading unchanged; the exit is resolved later from this pivot pose.
            var exitPose = new PathPose(pivot, forward, entry.Up);

            return new PathSegmentResult(
                new[] { span }, exitPose, pivot: pivot,
                approachStart: entrance, exitStart: pivot, exitEnd: pivot,
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

            // ExitDistance > 0 is guaranteed for turn segments by TrackSegmentLibrary.Normalize,
            // so the exit span is always well-formed — same as BuildTurn.
            var spans = new[] { new PathSpan(new[] { nudgedPivot, exit }, chosen, segment) };

            var exitPose = new PathPose(exit, newForward, atPivot.Up);

            // Legacy: ApproachStart is approximated as nudgedPivot - ToPivotDistance·newAxis.
            Vector3 approachStart = nudgedPivot - segment.ToPivotDistance * newForward;

            return new PathSegmentResult(
                spans, exitPose, pivot: nudgedPivot,
                approachStart: approachStart, exitStart: nudgedPivot, exitEnd: exit,
                geometryDirection: chosen, exitResolved: true);
        }
    }
}
