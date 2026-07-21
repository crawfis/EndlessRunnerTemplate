using UnityEngine;

namespace CrawfisSoftware.TempleRun.Track.Geometry
{
    /// <summary>
    /// The exact cardinal-axis math ported verbatim from the old
    /// <c>PathProvider._directionAxes</c> and its ±1 mod 4 turn rotation. Shared by
    /// <see cref="AxisAligned90Builder"/> and <see cref="ArcTurnBuilder"/> so both derive
    /// the post-turn axis identically. Kept as integer index math (not trig) so results
    /// stay bit-exact with the legacy generator.
    /// </summary>
    internal static class CardinalDirections
    {
        /// <summary>The four cardinal heading axes, in the legacy index order { +Z, +X, -Z, -X }.</summary>
        internal static readonly Vector3[] Axes =
        {
            new Vector3(0, 0, 1),
            new Vector3(1, 0, 0),
            new Vector3(0, 0, -1),
            new Vector3(-1, 0, 0),
        };

        /// <summary>
        /// Index of the axis matching <paramref name="forward"/>. Because headings are always
        /// assigned from <see cref="Axes"/>, the match is exact; defaults to 0 (+Z) otherwise.
        /// </summary>
        internal static int IndexOf(Vector3 forward)
        {
            for (int i = 0; i < Axes.Length; i++)
                if (Axes[i] == forward)
                    return i;
            return 0;
        }

        /// <summary>
        /// Rotates a cardinal index for a turn: Left = -1 (wrapping 0 → 3), Right = +1 mod 4.
        /// Straight/Either leave the index unchanged. Matches <c>TurnLeft</c>/<c>TurnRight</c>.
        /// </summary>
        internal static int Rotate(int index, Direction turn)
        {
            switch (turn)
            {
                case Direction.Left:  return (index == 0) ? 3 : index - 1;
                case Direction.Right: return (index + 1) % 4;
                default:              return index;
            }
        }
    }
}
