namespace CrawfisSoftware.TempleRun.GameConfig
{
    internal static class TempleRunConstants
    {
        public const float CountdownSeconds = 3f;
        public const float DelayAfterFailureBeforeAutoTurning = 0.85f;
        public const float ResumeDelay = 1.5f;

        /// <summary>
        /// Fixed distance before the segment exit at which SegmentExiting fires.
        /// Should satisfy: ExitDistance - SegmentExitingTriggerDistance > TeleportDistance
        /// so that SegmentExiting fires after the teleport lands.
        /// </summary>
        public const float SegmentExitingTriggerDistance = 2f;

        /// <summary>
        /// Minimum gap kept between a turn segment's TurnFailureDistance and its Length.
        /// SegmentExited fires at Length and immediately re-arms TurnCollisionDetector for the
        /// next segment, so a failure distance at or past Length can never be observed and the
        /// player would silently survive a missed turn.
        /// </summary>
        public const float TurnFailureMarginBeforeExit = 0.5f;
    }
}
