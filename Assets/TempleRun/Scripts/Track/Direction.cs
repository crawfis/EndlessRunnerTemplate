namespace CrawfisSoftware.TempleRun
{
    // For many directions this should be made a flag. All tests then need to mask it.
    public enum Direction
    {
        Left     = 0,
        Right    = 1,
        Straight = 2,  // No turn — track continues forward (subway-style)
        Either     = 3,  // T-junction — direction deferred until player swipes
    }
}
