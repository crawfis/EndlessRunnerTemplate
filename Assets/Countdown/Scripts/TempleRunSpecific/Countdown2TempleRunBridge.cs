using CrawfisSoftware.Events;
using CrawfisSoftware.TempleRun;

using System.Collections.Generic;


namespace CrawfisSoftware.Countdown.Events
{
    /// <summary>
    /// The sanctioned crossing from the Countdown domain into TempleRun. One direction only:
    /// gameplay never talks back to the ceremony.
    /// </summary>
    internal class Countdown2TempleRunBridge : AutoEventFlowBase<CountdownEvents, TempleRunEvents>
    {
        private static readonly (CountdownEvents From, TempleRunEvents To)[] CountdownToTempleRun =
        {
            // The translation seam. In gameplay vocabulary the countdown's end means exactly one
            // thing: release the player. TempleRun cannot tell - and must not care - whether a
            // countdown, a cutscene, or nothing at all sat between TempleRunStartRequested (its
            // systems coming up) and PlayerActivateRequested (the player going). Swap this domain
            // for another ceremony and gameplay is untouched.
            (CountdownEvents.CountdownEnded, TempleRunEvents.PlayerActivateRequested),
        };

        protected override IReadOnlyList<(CountdownEvents From, TempleRunEvents To)> Chains => CountdownToTempleRun;
    }
}
