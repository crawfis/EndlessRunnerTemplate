using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    internal class InitializeDistanceTracker : MonoBehaviour
    {
        private void Start()
        {
            var distanceTracker = new DistanceTracker();
            Blackboard.Instance.DistanceTracker = distanceTracker;
        }
    }
}