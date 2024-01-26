using CrawfisSoftware.Unity3D.Utility;
using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
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