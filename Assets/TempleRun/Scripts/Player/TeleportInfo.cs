namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// The teleport a turn's exit sets off, published via <c>TempleRunEvents.TeleportStarting</c>
    /// and reaching subscribers on <c>TeleportStarted</c>. <c>TeleportController</c> is the only
    /// publisher; it owns the duration, the path owns the destination.
    ///
    /// <para>This replaces a <c>(float, object)</c> tuple whose second slot every subscriber had
    /// to know was a spline and cast a second time. <see cref="Destination"/> is always a section
    /// whose <see cref="SplineSection.TeleportOwnsTransform"/> is true — that test is what
    /// started this teleport.</para>
    /// </summary>
    public readonly struct TeleportInfo
    {
        /// <summary>Seconds the teleport takes. <c>CharacterTeleporter</c> lerps over it.</summary>
        public readonly float Duration;

        /// <summary>The section being teleported onto.</summary>
        public readonly SplineSection Destination;

        public TeleportInfo(float duration, SplineSection destination)
        {
            Duration = duration;
            Destination = destination;
        }

        public override string ToString() => $"TeleportInfo: {Duration}s -> {Destination}";
    }
}
