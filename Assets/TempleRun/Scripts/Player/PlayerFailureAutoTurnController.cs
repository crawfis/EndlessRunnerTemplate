using CrawfisSoftware.TempleRun.GameConfig;
using CrawfisSoftware.Utility;

using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Handles auto-turning after a failure that originates from reaching the end of a track segment.
    ///    Dependencies: TurnController, EventsFor<TempleRunEvents>
    ///    Subscribes: PlayerFailingAtTurn — deliberately the SPECIFIC failure, not the generic
    ///                PlayerFailing: only a failed turn should auto-advance the track.
    ///    Calls: TurnController.ForceTurn() — a direct call rather than an event; see the
    ///           seam audit in docs/event-review/
    /// </summary>
    internal class PlayerFailureAutoTurnController : MonoBehaviour
    {
        [SerializeField] private TurnController _turnController;

        private void Awake()
        {
            TempleRunBus.Subscribe(TempleRunEvents.PlayerFailingAtTurn, OnPlayerFailing);
        }

        private void OnPlayerFailing(string eventName, object sender, object data)
        {
            if (data is float)
            {
                // Note: This starts immediately and runs in parallel with pause behavior.
                _ = AdvanceAfterFailure();
            }
        }

        private async Awaitable AdvanceAfterFailure()
        {
            // Wait until pause is almost over before advancing the player to the next track segment.
            await Wait.ForSecondsRealtime(TempleRunConstants.DelayAfterFailureBeforeAutoTurning, destroyCancellationToken);
            _turnController.ForceTurn();
        }

        private void OnDestroy()
        {
            TempleRunBus.Unsubscribe(TempleRunEvents.PlayerFailingAtTurn, OnPlayerFailing);
        }
    }
}
