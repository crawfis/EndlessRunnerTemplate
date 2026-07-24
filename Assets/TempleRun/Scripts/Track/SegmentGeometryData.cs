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
        // Where the exit sub-spline (and the exit tiles) begin. For a turn this is the shifted
        // pivot — offset laterally from Pivot by TrackWidthOffset so the exit tiles butt flush at
        // the corner — while Pivot stays on the centre line for the straight approach. For straights
        // and unresolved Either approaches it equals Pivot. The player teleports onto ExitStart at a
        // turn, so its exit runs along the tiles rather than the centre line (no sideways jump).
        public Vector3 ExitStart;
        public Vector3 ExitEnd;
        public Direction Direction;
        public TrackSegmentDefinition Definition;
        public int SequenceIndex;
        public bool ExitResolved;

        public override string ToString()
        {
            return $"SegmentGeometry[{SequenceIndex}]: {ApproachStart}->{Pivot}~>{ExitStart}->{ExitEnd} Dir={Direction} Resolved={ExitResolved}";
        }
    }
}
