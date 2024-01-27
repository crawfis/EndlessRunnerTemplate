namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// A simple class to update a float distance and retreive it.
    /// </summary>
    public class DistanceTracker
    {
        public float DistanceTravelled { get; private set; }
        public void UpdateDistance(float deltaDistance)
        {
            DistanceTravelled += deltaDistance;
        }
    }
}