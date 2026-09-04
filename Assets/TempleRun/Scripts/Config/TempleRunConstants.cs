namespace CrawfisSoftware.TempleRun.GameConfig
{
    internal static class TempleRunConstants
    {
        // CountdownSeconds moved to a serialized field on the Countdown domain's
        // CountdownController - the ceremony's length is not TempleRun's business.
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

        /// <summary>
        /// Exit-section length applied to a turn segment authored without one.
        /// A turn must have somewhere to run after the pivot: with ExitDistance 0 the exit
        /// sub-spline collapses to a point, which has no direction to face and nothing to build.
        /// Matches the smallest exit used by the authored registry segments.
        /// </summary>
        public const float MinimumTurnExitDistance = 1f;
    }
}
