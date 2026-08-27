using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Manages the number of lives a player has, converting the PlayerFailing events to a PlayerDied event when
    /// all of the lives run out.
    ///    Dependencies: Blackboard, EventsFor<TempleRunEvents>
    ///    Subscribes: TempleRunEvents.PlayerFailing (auto-chained from any specific failure)
    ///    Subscribes: TempleRunEvents.TempleRunStarted (resets lives at game start)
    ///    Publishes: TempleRunEvents.PlayerDied — Data is the final score (float).
    /// </summary>
    /// <remarks>For local multi-player we may need a player ID. Would be good to include this in the event data.</remarks>
    internal class PlayerLifeController : MonoBehaviour
    {
        [SerializeField] private int _playerID = 0;

        private int _numberOfLives;

        private void Awake()
        {
            // One subscription, not one per kind of failure: the auto-flow funnels every
            // specific failure into PlayerFailing. A new failure cause costs no edit here.
            TempleRunBus.Subscribe(TempleRunEvents.PlayerFailing, OnPlayerFailed);
            TempleRunBus.Subscribe(TempleRunEvents.TempleRunStarted, OnGameStarted);
        }

        private void OnGameStarted(string eventName, object sender, object data)
        {
            _numberOfLives = Blackboard.Instance.GameConfig.NumberOfLives;
            Debug.Log($"PlayerLifeController: Lives reset to {_numberOfLives}");
        }

        private void OnPlayerFailed(string eventName, object sender, object data)
        {
            // Todo: Check playerID
            _numberOfLives--;
            Debug.Log($"PlayerLifeController: Life lost. Remaining: {_numberOfLives}");
            if (_numberOfLives <= 0)
            {
                float score = Blackboard.Instance.DistanceTracker.DistanceTravelled;
                TempleRunBus.Publish(TempleRunEvents.PlayerDied, this, score);
                // No resume published here. This class used to unpause on death purely to
                // cancel a pause PlayerFailedController had started - the lives system had to
                // know that failing causes a freeze. The freeze now ends on its own, when
                // PlayerFailedController publishes PlayerFailed.
            }
        }

        private void OnDestroy()
        {
            TempleRunBus.Unsubscribe(TempleRunEvents.PlayerFailing, OnPlayerFailed);
            TempleRunBus.Unsubscribe(TempleRunEvents.TempleRunStarted, OnGameStarted);
        }
    }
}
