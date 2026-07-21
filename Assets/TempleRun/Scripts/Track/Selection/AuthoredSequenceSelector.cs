using System.Collections.Generic;

namespace CrawfisSoftware.TempleRun.Track
{
    /// <summary>
    /// A selector that plays a fixed, authored order of segments and loops — useful for
    /// tutorials, boss runs, or scripted showcases. Provided to prove the
    /// <see cref="ISegmentSelector"/> seam; it is not wired to any level by default.
    ///
    /// Order source: the explicit id list passed to the constructor, or (when none is
    /// given) the pool's <see cref="ISegmentPool.Segments"/> order.
    ///    Determinism: fully deterministic by construction. It intentionally ignores
    ///    <see cref="SelectionContext.Random"/> — the sequence is authored, not random —
    ///    which still satisfies the "no non-seeded randomness" contract.
    /// </summary>
    public sealed class AuthoredSequenceSelector : ISegmentSelector
    {
        private readonly IReadOnlyList<string> _orderedIds;   // null => use pool order
        private List<TrackSegmentDefinition> _resolved;       // cached, built from the pool on first use
        private int _cursor;

        /// <summary>Play the pool's segments in registry order, looping.</summary>
        public AuthoredSequenceSelector()
        {
        }

        /// <summary>Play the given segment ids in order, looping. Ids missing from the pool are skipped.</summary>
        public AuthoredSequenceSelector(IReadOnlyList<string> orderedIds)
        {
            _orderedIds = orderedIds;
        }

        public TrackSegmentDefinition SelectStart(ISegmentPool pool, SelectionContext ctx)
        {
            _cursor = 0;

            // Honor an authored start segment when the level specifies one; otherwise
            // begin the sequence from its first entry.
            string startId = pool.StartSegmentId;
            if (!string.IsNullOrWhiteSpace(startId))
            {
                var segment = pool.ById(startId);
                if (segment != null) return segment;
            }

            return Advance(pool);
        }

        public TrackSegmentDefinition SelectNext(ISegmentPool pool, SelectionContext ctx)
        {
            return Advance(pool);
        }

        private TrackSegmentDefinition Advance(ISegmentPool pool)
        {
            var sequence = Resolve(pool);
            if (sequence.Count == 0) return null;

            var segment = sequence[_cursor % sequence.Count];
            _cursor++;
            return segment;
        }

        private List<TrackSegmentDefinition> Resolve(ISegmentPool pool)
        {
            if (_resolved != null) return _resolved;

            _resolved = new List<TrackSegmentDefinition>();

            if (_orderedIds != null)
            {
                foreach (var id in _orderedIds)
                {
                    var segment = pool.ById(id);
                    if (segment != null) _resolved.Add(segment);
                }
            }
            else
            {
                foreach (var segment in pool.Segments)
                    _resolved.Add(segment);
            }

            return _resolved;
        }
    }
}
