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
    }
}
