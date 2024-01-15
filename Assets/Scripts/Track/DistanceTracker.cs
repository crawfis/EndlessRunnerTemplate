namespace CrawfisSoftware.TempleRun
{
    internal class DistanceTracker
    {
        public float DistanceTravelled { get; private set; }
        public void UpdateDistance(float deltaDistance)
        {
            DistanceTravelled += deltaDistance;
        }
    }
}