using System;
using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Full 3-point geometry for a track segment, published via
    /// <c>TempleRunEvents.SegmentGeometryReady</c>.
    /// Built by PathProvider; cached by SegmentTransitionController.
    /// </summary>
    [Serializable]
    public struct SegmentGeometryData
    {
        public Vector3 ApproachStart;
        public Vector3 Pivot;
        public Vector3 ExitEnd;
        public Direction Direction;
        public TrackSegmentDefinition Definition;
        public int SequenceIndex;
        public bool ExitResolved;

        public override string ToString()
        {
            return $"SegmentGeometry[{SequenceIndex}]: {ApproachStart}->{Pivot}->{ExitEnd} Dir={Direction} Resolved={ExitResolved}";
        }
    }
}
