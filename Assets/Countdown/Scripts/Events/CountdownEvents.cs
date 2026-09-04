using CrawfisSoftware.Events;

namespace CrawfisSoftware.Countdown.Events
{
    /// <summary>
    /// The pre-run ceremony, as its own domain. These members used to live in TempleRunEvents
    /// (values 30-36), which made the countdown look like a gameplay mechanic; it is not. It is a
    /// ceremony the session runs before releasing the player, and a project that wants a cutscene,
    /// a "tap to start" gate, or nothing at all replaces this domain without touching gameplay.
    ///
    /// The old CountdownCancelled (36) is deliberately dropped: nothing published or subscribed to
    /// it. Reintroduce it only alongside the code that actually cancels a countdown.
    /// </summary>
    [EventEnum]
    public enum CountdownEvents
    {
        // ---------- Countdown ceremony ----------
        CountdownStartRequested = 0,
        CountdownStarting = 1,
        CountdownStarted = 2,
        CountdownTick = 3,
        CountdownEnding = 4,
        CountdownEnded = 5,
    }
}
