using UnityEngine;

namespace CrawfisSoftware.TempleRun.Track
{
    /// <summary>
    /// Raises the difficulty target as the run goes on: the track starts easy and works up to a
    /// ceiling over <see cref="_rampDistance"/> world units, then stays there. Selection itself is
    /// the standard pipeline — this policy only decides what to aim at.
    ///
    /// Because the target moves with distance rather than with segment count, the ramp is unaffected
    /// by how long individual segments happen to be.
    ///    Determinism: the target is a pure function of <see cref="SelectionContext.DistanceTravelled"/>,
    ///    and the choice draws only from <see cref="SelectionContext.Random"/>.
    /// </summary>
    /// <remarks>
    /// The difficulty gate is a soft preference, not a guarantee: when no segment in the pool falls
    /// within range the shared pipeline retries ungated rather than stalling, so a sparse pool
    /// degrades to ordinary weighted selection instead of failing.
    /// </remarks>
    public sealed class DistanceRampSelector : ISegmentSelector
    {
        private readonly float _startDifficulty;
        private readonly float _endDifficulty;
        private readonly float _rampDistance;
        private readonly float _difficultyRange;

        /// <param name="startDifficulty">Target at distance 0.</param>
        /// <param name="endDifficulty">Target once <paramref name="rampDistance"/> is reached.</param>
        /// <param name="rampDistance">World units over which the target climbs. Must be &gt; 0.</param>
        /// <param name="difficultyRange">Half-width of the accepted band around the target.</param>
        public DistanceRampSelector(float startDifficulty = 1f, float endDifficulty = 8f,
                                    float rampDistance = 500f, float difficultyRange = 2f)
        {
            _startDifficulty = startDifficulty;
            _endDifficulty   = endDifficulty;
            _rampDistance    = rampDistance;
            _difficultyRange = difficultyRange;
        }

        /// <summary>The difficulty aimed for at a given distance. Public so a HUD or a test can
        /// plot the curve without running a selection.</summary>
        public float TargetAt(float distanceTravelled)
        {
            if (_rampDistance <= 0f) return _endDifficulty;
            float t = Mathf.Clamp01(distanceTravelled / _rampDistance);
            return Mathf.Lerp(_startDifficulty, _endDifficulty, t);
        }

        public TrackSegmentDefinition SelectStart(ISegmentPool pool, SelectionContext ctx)
        {
            // An authored start segment wins over the ramp, exactly as it does for the default
            // policy — the first segment is a level's own choice, not a difficulty decision.
            string startId = pool.StartSegmentId;
            if (!string.IsNullOrWhiteSpace(startId))
            {
                var segment = pool.ById(startId);
                if (segment != null) return segment;
            }

            return WeightedDifficultySelector.SelectByDifficulty(
                pool, ctx, TargetAt(ctx.DistanceTravelled), _difficultyRange);
        }

        public TrackSegmentDefinition SelectNext(ISegmentPool pool, SelectionContext ctx)
        {
            return WeightedDifficultySelector.SelectByDifficulty(
                pool, ctx, TargetAt(ctx.DistanceTravelled), _difficultyRange);
        }
    }
}
