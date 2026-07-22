using System.Collections.Generic;

namespace CrawfisSoftware.TempleRun.Track
{
    /// <summary>
    /// Read-only view of the segment pool and level configuration that an
    /// <see cref="ISegmentSelector"/> reads from when choosing the next segment.
    ///    Implemented by <see cref="TrackSegmentLibrary"/> (the data owner).
    ///    This is the "data" half of the old TrackSegmentLibrary; the "algorithm"
    ///    half now lives in <see cref="ISegmentSelector"/> implementations.
    /// </summary>
    /// <remarks>
    /// The pool is treated as immutable for the duration of a run. Selectors must
    /// not mutate the returned collections.
    /// </remarks>
    public interface ISegmentPool
    {
        /// <summary>All segments available for this level, in registry order.</summary>
        IReadOnlyList<TrackSegmentDefinition> Segments { get; }

        /// <summary>Look up a segment by its id, or <c>null</c> when it is not in the pool.</summary>
        TrackSegmentDefinition ById(string id);

        /// <summary>
        /// The ids a segment may connect to. Returns an empty list (never <c>null</c>)
        /// when the segment has no explicit connections — meaning the choice is
        /// unconstrained and the whole pool is eligible.
        /// </summary>
        IReadOnlyList<string> ConnectionsFrom(string id);

        /// <summary>Number of lanes for this level.</summary>
        int LaneCount { get; }

        /// <summary>Width of a single lane, in world units.</summary>
        float LaneWidth { get; }

        /// <summary>
        /// The authored start segment id, or <c>null</c>/empty when the start segment
        /// should be chosen by the selector.
        /// </summary>
        string StartSegmentId { get; }
    }
}
