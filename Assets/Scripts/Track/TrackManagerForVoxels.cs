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

        // A different Direction strategy just for grins.
        protected override Direction GetNewDirection()
        {
            //float randomValue = (float)_random.NextDouble();
            //return randomValue switch
            //{
            //    < 0.4f => Direction.Left,
            //    < 0.8f => Direction.Right,
            //    _ => Direction.Left,
            //};
            Direction newDirection = Direction.Both;
            switch (_lastDirection)
            {
                case Direction.Left:
                    newDirection = Direction.Right;
                    break;
                case Direction.Right:
                    newDirection = Direction.Left;
                    break;
                default:
                    newDirection = Direction.Both;
                    break;
            }
            _lastDirection = newDirection;
            return newDirection;
        }
    }
}