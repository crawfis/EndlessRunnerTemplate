namespace CrawfisSoftware.TempleRun.Track
{
    /// <summary>
    /// Alternates calm and challenge stretches by segment index, so the run has a rhythm instead of
    /// a uniform grind: <see cref="_calmLength"/> segments aimed at an easy target, then
    /// <see cref="_challengeLength"/> aimed at a hard one, repeating. Selection itself is the
    /// standard pipeline — this policy only decides what to aim at.
    ///
    /// Counted in segments rather than distance, because the point is the shape of the sequence the
    /// player experiences. <see cref="DistanceRampSelector"/> is the distance-based counterpart, and
    /// the two are complementary: a ramp sets the overall trend, a wave sets the local texture.
    ///    Determinism: the phase is a pure function of <see cref="SelectionContext.SegmentIndex"/>,
    ///    and the choice draws only from <see cref="SelectionContext.Random"/>.
    /// </summary>
    /// <remarks>
    /// The difficulty gate is a soft preference: when nothing in the pool falls within range the
    /// shared pipeline retries ungated, so a pool with no genuinely hard segments simply reads as
    /// flat rather than stalling.
    /// </remarks>
    public sealed class WaveSelector : ISegmentSelector
    {
        private readonly int   _calmLength;
        private readonly int   _challengeLength;
        private readonly float _calmDifficulty;
        private readonly float _challengeDifficulty;
        private readonly float _difficultyRange;

        /// <param name="calmLength">Segments per calm stretch. Clamped to at least 1.</param>
        /// <param name="challengeLength">Segments per challenge stretch. Clamped to at least 1.</param>
        /// <param name="calmDifficulty">Target during a calm stretch.</param>
        /// <param name="challengeDifficulty">Target during a challenge stretch.</param>
        /// <param name="difficultyRange">Half-width of the accepted band around the target.</param>
        public WaveSelector(int calmLength = 3, int challengeLength = 2,
                            float calmDifficulty = 2f, float challengeDifficulty = 8f,
                            float difficultyRange = 2f)
        {
            _calmLength          = calmLength    < 1 ? 1 : calmLength;
            _challengeLength     = challengeLength < 1 ? 1 : challengeLength;
            _calmDifficulty      = calmDifficulty;
            _challengeDifficulty = challengeDifficulty;
            _difficultyRange     = difficultyRange;
        }

        /// <summary>True while the given segment index falls in a challenge stretch. Public so a
        /// test or a debug overlay can show the rhythm without running a selection.</summary>
        public bool IsChallengeAt(int segmentIndex)
        {
            int period = _calmLength + _challengeLength;
            // Negative indices should not occur, but flooring the modulus keeps the phase stable
            // rather than mirroring it if one ever does.
            int phase = ((segmentIndex % period) + period) % period;
            return phase >= _calmLength;
        }

        /// <summary>The difficulty aimed for at a given segment index.</summary>
        public float TargetAt(int segmentIndex)
            => IsChallengeAt(segmentIndex) ? _challengeDifficulty : _calmDifficulty;

        public TrackSegmentDefinition SelectStart(ISegmentPool pool, SelectionContext ctx)
        {
            // An authored start segment wins over the wave, as it does for every other policy.
            string startId = pool.StartSegmentId;
            if (!string.IsNullOrWhiteSpace(startId))
            {
                var segment = pool.ById(startId);
                if (segment != null) return segment;
            }

            return WeightedDifficultySelector.SelectByDifficulty(
                pool, ctx, TargetAt(ctx.SegmentIndex), _difficultyRange);
        }

        public TrackSegmentDefinition SelectNext(ISegmentPool pool, SelectionContext ctx)
        {
            return WeightedDifficultySelector.SelectByDifficulty(
                pool, ctx, TargetAt(ctx.SegmentIndex), _difficultyRange);
        }
    }
}
