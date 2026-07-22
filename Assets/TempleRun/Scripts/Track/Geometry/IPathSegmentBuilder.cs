namespace CrawfisSoftware.TempleRun.Track.Geometry
{
    /// <summary>
    /// The pluggable geometry policy: given where we are and which way we are heading
    /// (<see cref="PathPose"/>), a segment definition, and the resolved turn direction,
    /// produce the span(s) that realize the segment plus the pose to continue from.
    ///    Determinism contract: implementations MUST be pure functions of their inputs
    ///    (no <c>UnityEngine.Random</c>, no wall-clock time) so a given segment stream
    ///    always produces the same geometry.
    /// </summary>
    /// <remarks>
    /// Either-junction exits are deferred: <see cref="Build"/> with
    /// <see cref="Direction.Either"/> returns only the approach span(s) and an exit pose
    /// that sits at the pivot (heading unchanged). Once the player commits a direction,
    /// <see cref="BuildEitherExit"/> is called with that pivot pose and the chosen
    /// direction to produce the exit half.
    /// </remarks>
    public interface IPathSegmentBuilder
    {
        /// <summary>
        /// Build a full segment. For <see cref="Direction.Either"/> only the approach is
        /// built (the exit is deferred to <see cref="BuildEitherExit"/>).
        /// </summary>
        PathSegmentResult Build(PathPose entry, TrackSegmentDefinition segment, Direction resolved);

        /// <summary>
        /// Build the deferred exit half of an Either junction, starting from the pivot pose
        /// produced by the matching <see cref="Build"/> call, given the player's chosen
        /// <paramref name="chosen"/> direction.
        /// </summary>
        PathSegmentResult BuildEitherExit(PathPose atPivot, TrackSegmentDefinition segment, Direction chosen);
    }
}
