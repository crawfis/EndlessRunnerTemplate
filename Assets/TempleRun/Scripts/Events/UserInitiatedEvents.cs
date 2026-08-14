namespace CrawfisSoftware.Events
{
    [EventEnum]
    public enum UserInitiatedEvents
    {
        UserLeftTurnRequested,
        UserRightTurnRequested,
        UserPauseToggle,
        UserLeftLaneChangeRequested,
        UserRightLaneChangeRequested,
        UserJumpRequested,
        UserQuitRequested,
        UserSlideRequested,
        UserDashRequested,
    }
}