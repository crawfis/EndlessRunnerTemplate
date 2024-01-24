using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    internal class TrackManagerForVoxels : TrackManager
    {
        protected Direction _lastDirection = Direction.Right;

        protected override float GetNewSegmentLength()
        {
            float length = base.GetNewSegmentLength();
            return Mathf.FloorToInt(length+0.5f);
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