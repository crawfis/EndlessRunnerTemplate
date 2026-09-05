using System;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Runtime data for a single track segment passed via
    /// <c>TempleRunEvents.TrackSegmentCreated</c> and <c>ActiveTrackChanging</c>.
    /// Carries the full <see cref="TrackSegmentDefinition"/>, the resolved turn
    /// <see cref="Direction"/>, and the run-absolute origin of the segment.
    ///
    /// <para><b>Units.</b> <see cref="TrackSegmentDefinition"/> measures everything from the
    /// segment's own entrance; every consumer of this message measures against
    /// <c>DistanceTracker.DistanceTravelled</c>, which is measured from the start of the run.
    /// The one number that converts between them is <see cref="StartDistance"/>, and
    /// <see cref="TrackManager"/> owns it — it is stamped once, when the segment is created,
    /// by the only component that knows the queue order and every segment's length.
    /// The distances on <i>this struct</i> are therefore run-absolute; the ones on
    /// <see cref="Definition"/> stay segment-relative, which is the form the geometry builders
    /// and visual spawners want. Read an absolute position off the message, a relative one off
    /// the definition, and never accumulate either.</para>
    ///
    /// <para><see cref="Length"/> and <see cref="TeleportDistance"/> have no absolute form
    /// because they are lengths rather than positions: the first spans the segment, the second
    /// is an offset past the pivot.</para>
    /// </summary>
    [Serializable]
    public struct TrackSegmentInfo
    {
        public TrackSegmentDefinition Definition;
        public Direction Direction;

        /// <summary>Distance from the start of the run at which this segment begins.</summary>
        public float StartDistance;

        public string SegmentId        => Definition?.Id ?? "unknown";
        public float  Length           => Definition?.Length ?? 0f;
        public float  TeleportDistance => Definition?.TeleportDistance ?? 0f;

        /// <summary>Run-absolute distance of the pivot (the turn / placeholder point).</summary>
        public float PivotDistance => StartDistance + (Definition?.ToPivotDistance ?? 0f);

        /// <summary>
        /// Run-absolute distance past which a required turn counts as failed.
        /// <see cref="float.MaxValue"/> for a Straight, which never fails.
        /// </summary>
        public float TurnFailureDistance => StartDistance + (Definition?.TurnFailureDistance ?? 0f);

        /// <summary>Run-absolute distance at which this segment ends and the next begins.</summary>
        public float EndDistance => StartDistance + Length;

        public TrackSegmentInfo(TrackSegmentDefinition definition, Direction direction, float startDistance)
        {
            Definition    = definition;
            Direction     = direction;
            StartDistance = startDistance;
        }

        public override string ToString()
        {
            return $"TrackSegmentInfo: Id={SegmentId}, Start={StartDistance}, Length={Length}, End={EndDistance}, PivotDistance={PivotDistance}, TurnFailureDistance={TurnFailureDistance}, TeleportDistance={TeleportDistance}, Direction={Direction}";
        }
    }
}
