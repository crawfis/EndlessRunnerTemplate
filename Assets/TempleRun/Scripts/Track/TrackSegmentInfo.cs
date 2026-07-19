using System;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Runtime data for a single track segment passed via
    /// <c>TempleRunEvents.TrackSegmentCreated</c>.
    /// Carries the full <see cref="TrackSegmentDefinition"/> plus the
    /// resolved turn <see cref="Direction"/>.
    /// </summary>
    [Serializable]
    public struct TrackSegmentInfo
    {
        public TrackSegmentDefinition Definition;
        public Direction Direction;

        public string SegmentId        => Definition?.Id ?? "unknown";
        public float  Length           => Definition?.Length ?? 0f;
        public float  TurnPointDistance => Definition?.TurnFailureDistance ?? 0f;
        public float  ToPivotDistance => Definition?.ToPivotDistance ?? 0f;
        public float  ExitDistance     => Definition?.ExitDistance ?? 0f;
        public float  TeleportDistance => Definition?.TeleportDistance ?? 0f;

        public TrackSegmentInfo(TrackSegmentDefinition definition, Direction direction)
        {
            Definition = definition;
            Direction  = direction;
        }

        public override string ToString()
        {
            return $"TrackSegmentInfo: Id={SegmentId}, Length={Length}, EntranceDistance={ToPivotDistance}, ExitDistance={ExitDistance}, TeleportDistance={TeleportDistance}, Direction={Direction}";
        }
    }
}
