namespace CrawfisSoftware.TempleRun
{
    public enum KnownEvents
    {
        LeftTurnRequested, LeftTurnSucceeded,
        RightTurnRequested, RightTurnSucceeded,
        PlayerFailed, PlayerDied,
        GameStarted, GameOver, Pause, Resume, PauseToggle,
        CountdownStarted, CountdownTick,
        SplineSegmentCreated, CurrentSplineChanging, CurrentSplineChanged,
        ActiveTrackChanging, TrackSegmentCreated,
        TeleportStarted, TeleportEnded
    };
}