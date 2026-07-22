using System.Collections.Generic;
using UnityEngine;

namespace CrawfisSoftware.TempleRun.Track
{
    /// <summary>
    /// The default selection policy — a verbatim port of the algorithm that previously
    /// lived in <c>TrackSegmentLibrary.SelectNext</c>/<c>SelectWeighted</c>/<c>IsAllowed</c>/
    /// <c>IsInDifficultyRange</c>. Behaviour-preserving: for a given pool, previous segment,
    /// repeat count and <see cref="System.Random"/> stream it reproduces the exact same
    /// choices in the exact same order.
    ///
    /// Pipeline: connection-filter → MaxRepeat gate → difficulty gate → weighted random,
    /// with the same fallbacks (ungated retry when the difficulty filter empties the set,
    /// then the whole pool when still empty).
    ///    Determinism: draws only from <see cref="SelectionContext.Random"/>.
    /// </summary>
    public sealed class WeightedDifficultySelector : ISegmentSelector
    {
        private readonly float _targetDifficulty;
        private readonly float _difficultyRange;

        /// <summary>
        /// Creates the selector. Defaults (<paramref name="targetDifficulty"/> = -1, i.e. ungated)
        /// exactly match the arguments <c>TrackManager</c> historically passed to
        /// <c>TrackSegmentLibrary.SelectNext(previousId, repeat, random)</c> — the two optional
        /// difficulty arguments were left at their defaults, so selection was ungated.
        /// Pass a non-negative <paramref name="targetDifficulty"/> to opt into difficulty gating.
        /// </summary>
        public WeightedDifficultySelector(float targetDifficulty = -1f, float difficultyRange = 2f)
        {
            _targetDifficulty = targetDifficulty;
            _difficultyRange  = difficultyRange;
        }

        /// <summary>
        /// Reproduces <c>TrackSegmentLibrary.GetStartSegment</c>: honor the pool's
        /// StartSegmentId when present, otherwise fall through to a normal (previous == null)
        /// selection — matching the old <c>SelectNext(null, 0, random)</c> call.
        /// </summary>
        public TrackSegmentDefinition SelectStart(ISegmentPool pool, SelectionContext ctx)
        {
            string startId = pool.StartSegmentId;
            if (!string.IsNullOrWhiteSpace(startId))
            {
                var segment = pool.ById(startId);
                if (segment != null) return segment;
            }

            return SelectInternal(pool, null, 0, ctx.Random, _targetDifficulty, _difficultyRange);
        }

        public TrackSegmentDefinition SelectNext(ISegmentPool pool, SelectionContext ctx)
        {
            return SelectInternal(pool, ctx.Previous?.Id, ctx.PreviousRepeatCount, ctx.Random,
                                  _targetDifficulty, _difficultyRange);
        }

        // ---------------------------------------------------------------------
        // Ported verbatim from TrackSegmentLibrary
        // ---------------------------------------------------------------------

        private static TrackSegmentDefinition SelectInternal(
            ISegmentPool pool,
            string previousSegmentId,
            int previousRepeatCount,
            System.Random random,
            float targetDifficulty,
            float difficultyRange)
        {
            bool  gated   = targetDifficulty >= 0f && difficultyRange >= 0f;
            float diffMin = targetDifficulty - difficultyRange;
            float diffMax = targetDifficulty + difficultyRange;

            var candidates = new List<TrackSegmentDefinition>();

            // A present-and-non-empty connection list is equivalent to the old
            // "_connectionsByFromId.TryGetValue succeeded" check (lists are only ever
            // created with at least one entry).
            IReadOnlyList<string> allowedIds =
                string.IsNullOrWhiteSpace(previousSegmentId) ? null : pool.ConnectionsFrom(previousSegmentId);

            if (allowedIds != null && allowedIds.Count > 0)
            {
                foreach (var id in allowedIds)
                {
                    var seg = pool.ById(id);
                    if (seg != null &&
                        IsAllowed(seg, previousSegmentId, previousRepeatCount) &&
                        (!gated || IsInDifficultyRange(seg, diffMin, diffMax)))
                        candidates.Add(seg);
                }
            }
            else
            {
                foreach (var seg in pool.Segments)
                {
                    if (IsAllowed(seg, previousSegmentId, previousRepeatCount) &&
                        (!gated || IsInDifficultyRange(seg, diffMin, diffMax)))
                        candidates.Add(seg);
                }
            }

            // Fall back to ungated if difficulty filter left us empty
            if (candidates.Count == 0 && gated)
                return SelectInternal(pool, previousSegmentId, previousRepeatCount, random, -1f, difficultyRange);

            if (candidates.Count == 0)
                candidates.AddRange(pool.Segments);

            return SelectWeighted(candidates, random);
        }

        private static bool IsAllowed(
            TrackSegmentDefinition segment, string previousSegmentId, int previousRepeatCount)
        {
            if (segment == null) return false;
            if (!string.IsNullOrWhiteSpace(previousSegmentId) &&
                segment.Id == previousSegmentId &&
                segment.MaxRepeat > 0)
                return previousRepeatCount < segment.MaxRepeat;
            return true;
        }

        private static bool IsInDifficultyRange(TrackSegmentDefinition segment, float min, float max)
            => segment.DifficultyRating >= min && segment.DifficultyRating <= max;

        private static TrackSegmentDefinition SelectWeighted(
            List<TrackSegmentDefinition> candidates, System.Random random)
        {
            if (candidates.Count == 0) return null;

            float totalWeight = 0f;
            foreach (var c in candidates) totalWeight += Mathf.Max(0f, c.Weight);

            if (totalWeight <= 0f) return candidates[random.Next(candidates.Count)];

            float pick = (float)random.NextDouble() * totalWeight;
            float cum  = 0f;
            foreach (var c in candidates)
            {
                cum += Mathf.Max(0f, c.Weight);
                if (pick <= cum) return c;
            }
            return candidates[^1];
        }
    }
}
