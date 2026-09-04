using CrawfisSoftware.Events;

using System.Collections.Generic;


namespace CrawfisSoftware.Countdown.Events
{
    /// <summary>
    /// Auto-chain Countdown-specific events. Keep this focused on the ceremony's own lifecycle;
    /// the crossings live in CountdownGameFlowBridge (GameFlow -> Countdown) and
    /// Countdown2TempleRunBridge (Countdown -> TempleRun).
    /// </summary>
    internal class CountdownAutoEventFlow : AutoEventFlowBase<CountdownEvents, CountdownEvents>
    {
        private static readonly (CountdownEvents From, CountdownEvents To)[] ChainTable =
        {
            // ================================================================================
            // COUNTDOWN LIFECYCLE
            // ================================================================================
            (CountdownEvents.CountdownStartRequested, CountdownEvents.CountdownStarting),
            // CountdownStarting -> CountdownStarted -> CountdownTick(s) -> CountdownEnding:
            // published by CountdownController as the clock actually reaches each rung.
            // The last link is chained, so a "GO!" flash or start-line delay can be inserted
            // there without CountdownController changing.
            (CountdownEvents.CountdownEnding, CountdownEvents.CountdownEnded),
        };

        protected override IReadOnlyList<(CountdownEvents From, CountdownEvents To)> Chains => ChainTable;
    }
}
