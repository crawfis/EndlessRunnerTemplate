using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// The straight run of path the player is currently following, published via
    /// <c>TempleRunEvents.CurrentSplineChanging</c> / <c>CurrentSplineChanged</c> and carried
    /// through the teleport ladder inside <see cref="TeleportInfo"/>.
    ///
    /// <para>A segment is delivered as two of these in turn: the <i>approach</i>, from the
    /// entrance to the pivot, always <see cref="Direction.Straight"/>; and, if the segment turns,
    /// the <i>exit</i>, from the shifted pivot along the new heading, carrying the turn's
    /// direction. <see cref="SegmentTransitionController"/> is the only publisher.</para>
    ///
    /// <para><b>Why this is a struct and not a tuple.</b> It replaces a
    /// <c>(Vector3, Vector3, Direction, float)</c> whose fourth slot and whose
    /// <see cref="Direction"/>-means-teleport convention lived only in comments, so every
    /// subscriber re-derived the rule for itself. The rule now has one name —
    /// <see cref="TeleportOwnsTransform"/> — and one definition, here.</para>
    /// </summary>
    public readonly struct SplineSection
    {
        /// <summary>Where this section begins, in track space.</summary>
        public readonly Vector3 Start;

        /// <summary>Where this section ends. For a turn's exit this is the teleport landing
        /// point, not the segment's far end: the exit is truncated to the teleport distance.</summary>
        public readonly Vector3 End;

        /// <summary>The turn this section is part of, or <see cref="Direction.Straight"/> for an
        /// approach. Test <see cref="TeleportOwnsTransform"/> rather than comparing this.</summary>
        public readonly Direction Direction;

        /// <summary>Run-absolute distance the player stands at once the teleport onto this
        /// section lands. Meaningful only when <see cref="TeleportOwnsTransform"/> is true;
        /// an approach carries no landing because nothing teleports onto it.</summary>
        public readonly float LandingDistance;

        /// <summary>
        /// Who writes the player's transform while this section is current.
        ///
        /// <para>A non-Straight section is a turn's exit, and <c>TeleportController</c> starts a
        /// teleport for exactly those, after which <c>CharacterTeleporter</c> lerps the player
        /// onto the section over the teleport's duration. <c>MoveCharacterByDistance</c> must
        /// therefore re-anchor but <i>not</i> place the player: snapping first made the lerp run
        /// from the destination to the destination, so the move was real but took zero frames.</para>
        /// </summary>
        public bool TeleportOwnsTransform => Direction != Direction.Straight;

        /// <summary>Unit vector from <see cref="Start"/> to <see cref="End"/>.</summary>
        public Vector3 Heading => (End - Start).normalized;

        public SplineSection(Vector3 start, Vector3 end, Direction direction, float landingDistance)
        {
            Start = start;
            End = end;
            Direction = direction;
            LandingDistance = landingDistance;
        }

        /// <summary>An approach section: straight, and nothing teleports onto it.</summary>
        public static SplineSection Approach(Vector3 start, Vector3 end)
            => new SplineSection(start, end, Direction.Straight, 0f);

        public override string ToString()
            => $"SplineSection: {Start}->{End} Dir={Direction} Landing={LandingDistance} TeleportOwns={TeleportOwnsTransform}";
    }
}
