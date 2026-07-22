namespace CrawfisSoftware.TempleRun.Track
{
    /// <summary>
    /// The state a selector may use to decide the next segment. Passed by value on
    /// every call so selectors stay stateless with respect to game state (any policy
    /// state they keep — e.g. an authored cursor — is their own concern).
    /// </summary>
    public readonly struct SelectionContext
    {
        /// <summary>The previously chosen segment, or <c>null</c> at the start of a run.</summary>
        public readonly TrackSegmentDefinition Previous;

        /// <summary>How many times in a row <see cref="Previous"/> has been chosen (1-based; 0 at start).</summary>
        public readonly int PreviousRepeatCount;

        /// <summary>Total distance travelled so far — enables distance-based difficulty ramps.</summary>
        public readonly float DistanceTravelled;

        /// <summary>How many segments have been chosen so far this run.</summary>
        public readonly int SegmentIndex;

        /// <summary>
        /// The deterministic, seeded random source. Selectors MUST draw only from this
        /// (never <see cref="UnityEngine.Random"/> or wall-clock time) so replays with the
        /// same seed reproduce the same track exactly.
        /// </summary>
        public readonly System.Random Random;

        public SelectionContext(TrackSegmentDefinition previous, int previousRepeatCount,
                                float distanceTravelled, int segmentIndex, System.Random random)
        {
            Previous            = previous;
            PreviousRepeatCount = previousRepeatCount;
            DistanceTravelled   = distanceTravelled;
            SegmentIndex        = segmentIndex;
            Random              = random;
        }
    }

    /// <summary>
    /// A pluggable segment-selection policy. One instance is created per run and asked
    /// for the start segment and each subsequent segment.
    ///    Determinism contract: implementations MUST derive all randomness from
    ///    <see cref="SelectionContext.Random"/> so a given seed always yields the same
    ///    stream of segment ids in the same order.
    /// </summary>
    public interface ISegmentSelector
    {
        /// <summary>Choose the first segment of a run.</summary>
        TrackSegmentDefinition SelectStart(ISegmentPool pool, SelectionContext ctx);

        /// <summary>Choose the next segment given the current selection state.</summary>
        TrackSegmentDefinition SelectNext(ISegmentPool pool, SelectionContext ctx);
    }
}
