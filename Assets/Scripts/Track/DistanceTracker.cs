using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Access to a simple MonoBehaviour float. Can be Queried an updated.
    /// Safe to use in Awake, Start, etc.
    /// </summary>
    internal class DistanceTracker : MonoBehaviour
    {
        public float DistanceTravelled { get; private set; }
        public void UpdateDistance(float deltaDistance)
        {
            DistanceTravelled += deltaDistance;
        }
    }
}