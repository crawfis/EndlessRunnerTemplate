namespace CrawfisSoftware.TempleRun
{
    public enum KnownEvents
    {
        LeftTurnRequested, LeftTurnSucceeded,
        RightTurnRequested, RightTurnSucceeded,
        PlayerFailed, PlayerDied,
        GameStarted, GameOver, Pause, Resume,
        CountdownStarted, CountdownTick,
        SplineSegmentCreated, CurrentSplineChanging, CurrentSplineChanged,
        ActiveTrackChanging, ActiveTrackChanged, TrackSegmentCreated,
        TeleportStarted, TeleportEnded
    };
}