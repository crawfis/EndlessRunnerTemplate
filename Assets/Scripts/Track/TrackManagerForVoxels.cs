using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Overrides the TrackManager's GetNewSegmentLength and returns the nearest whole number.
    /// Also uses and alternating strategy for turns.
    ///     Dependencies: TrackManager
    /// </summary>
    public class TrackManagerForVoxels : TrackManager
    {
        protected Direction _lastDirection = Direction.Right;

        protected override float GetNewSegmentLength()
        {
            float length = base.GetNewSegmentLength();
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