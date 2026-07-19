using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Overrides the TrackManager's GetNewSegmentLength. Ensures that the segment length is a multiple of the tile length.
    /// Also uses and alternating strategy for turns.
    ///     Dependencies: TrackManager
    /// </summary>
    public class TrackManagerForTiles : TrackManager
    {
        protected Direction _lastDirection = Direction.Right;

        protected override float GetNewSegmentLength()
        {
            float length = base.GetNewSegmentLength();
            // There is a non-direct (implicit) coupling now between the visuals (the tiles)
            // and the track segment length, but works for any tiling system.
            // This returns the number of tiles that will fit in the segment length.
            return Blackboard.Instance.TileLength * Mathf.FloorToInt((length + 0.5f) / Blackboard.Instance.TileLength);
        }

        // A different Direction strategy just to show flexibility
        // We simply alternate between turning left and turning right.
        protected override Direction GetNewDirection()
        {
            Direction newDirection = Direction.Right;
            if (_lastDirection == Direction.Right)
            {
                newDirection = Direction.Left;
            }
            _lastDirection = newDirection;
            return newDirection;
        }
    }
}