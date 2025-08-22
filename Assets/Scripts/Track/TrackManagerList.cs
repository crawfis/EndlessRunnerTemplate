using CrawfisSoftware.Scriptables;

using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Overrides the TrackManager's GetNewSegmentLength and returns one of the values from a list of integers.
    ///     Dependencies: TrackManager, IntListScriptable
    /// </summary>
    public class TrackManagerList : TrackManager
    {
        [SerializeField] private IntListScriptable _validTrackLengths;

        protected override float GetNewSegmentLength()
        {
            int index = Blackboard.Instance.MasterRandom.Next(_validTrackLengths.List.Count);
            return _validTrackLengths.List[index];
        }
    }
}