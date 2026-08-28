using CrawfisSoftware.Events;

using System.Collections.Generic;

namespace CrawfisSoftware.TempleRun.Events
{
    internal class Input2TempleRunAutoEventBridge
        : AutoEventFlowBase<UserInitiatedEvents, TempleRunEvents>
    {
        // User input bridges: raw input events -> gameplay events.
        // This is the ONLY place in the codebase that may subscribe to UserInitiatedEvents.
        // Gameplay controllers subscribe to the TempleRun event on the right-hand side, never
        // to the raw input on the left, so a mechanic can be driven from any source: player
        // input, AI, replay, network.
        //
        // NOTE: the right-hand event is the RAW translation - it fires whether or not the
        // action is currently legal. A controller that validates must publish its own
        // *Starting event after its checks pass; see TempleRunAutoEventFlow.cs.
        private static readonly (UserInitiatedEvents From, TempleRunEvents To)[] ChainTable =
        {
            (UserInitiatedEvents.UserQuitRequested, TempleRunEvents.TempleRunEndRequested),
            (UserInitiatedEvents.UserSlideRequested, TempleRunEvents.SlideRequested),
            (UserInitiatedEvents.UserDashRequested, TempleRunEvents.DashRequested),
            (UserInitiatedEvents.UserJumpRequested, TempleRunEvents.JumpRequested),
            (UserInitiatedEvents.UserLeftTurnRequested, TempleRunEvents.TurnLeftRequested),
            (UserInitiatedEvents.UserRightTurnRequested, TempleRunEvents.TurnRightRequested),
            (UserInitiatedEvents.UserLeftLaneChangeRequested, TempleRunEvents.LaneChangeLeftRequested),
            (UserInitiatedEvents.UserRightLaneChangeRequested, TempleRunEvents.LaneChangeRightRequested),
            (UserInitiatedEvents.UserPauseToggle, TempleRunEvents.PlayerPauseToggleRequested),
        };

        protected override IReadOnlyList<(UserInitiatedEvents From, TempleRunEvents To)> Chains => ChainTable;
    }
}
