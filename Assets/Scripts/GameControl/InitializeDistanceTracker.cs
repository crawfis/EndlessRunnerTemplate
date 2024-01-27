using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Simple class that creates a distance tracker and assigns it to the Blackboard.
    /// Unused - Moved to GameInitialization.
    /// </summary>
    public class InitializeDistanceTracker : MonoBehaviour
    {
        private void Start()
        {
            var distanceTracker = new DistanceTracker();
            Blackboard.Instance.DistanceTracker = distanceTracker;
        }
    }
}