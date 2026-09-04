using CrawfisSoftware.Countdown.Events;
using CrawfisSoftware.Events;

using System.Collections.Generic;


namespace CrawfisSoftware.GameFlow.Events
{
    /// <summary>
    /// The sanctioned crossing from GameFlow into the Countdown domain. One direction only, so it
    /// inherits AutoEventFlowBase rather than holding two dispatchers the way
    /// TempleRunGameFlowBridge does.
    /// </summary>
    internal class CountdownGameFlowBridge : AutoEventFlowBase<GameFlowEvents, CountdownEvents>
    {
        private static readonly (GameFlowEvents From, CountdownEvents To)[] GameFlowToCountdown =
        {
            // Session milestone -> ceremony trigger. The ceremony's OUTCOME does not come back
            // here: CountdownEnded goes to gameplay (Countdown2TempleRunBridge), and GameFlow
            // owns GameStarted through its own GameStarting -> GameStarted chain. That is the
            // whole point of the split - the session no longer waits on a ceremony to declare
            // itself started.
            (GameFlowEvents.GameStarting, CountdownEvents.CountdownStartRequested),
        };

        protected override IReadOnlyList<(GameFlowEvents From, CountdownEvents To)> Chains => GameFlowToCountdown;
    }
}
