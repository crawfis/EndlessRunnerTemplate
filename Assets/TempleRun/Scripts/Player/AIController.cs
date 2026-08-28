using CrawfisSoftware.Events;

using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;
using UserInputBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.Events.UserInitiatedEvents>;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Deterministic (and perfect?) AI that triggers a turn request whenever the current
    /// distances gets within a user-specified value of the end of the currently discovered track.
    ///    Dependency: TurnController, EventsFor<TempleRunEvents>, EventsFor<UserInitiatedEvents>
    ///    Subscribes: TempleRunEvents.TempleRunStarted
    ///    Publishes: UserInitiatedEvents.UserLeftTurnRequested
    ///    Publishes: UserInitiatedEvents.UserRightTurnRequested
    /// </summary>
    public class AIController : MonoBehaviour
    {
        [SerializeField] private TurnController _turnController;
        [Tooltip("Distance from far wall to turn. Should be between (0,opening size]. Can try to turn easy but the difficulty config will determine if possible.")]
        [SerializeField] private float _turnDistance = .1f;
        [SerializeField] private bool _isEnabled = true;

        private bool _gameStarted = false;

        private void Awake()
        {
            TempleRunBus.Subscribe(TempleRunEvents.TempleRunStarted, OnTempleRunStarted);
        }

        private void OnTempleRunStarted(string eventName, object sender, object data)
        {
            _gameStarted = true;
        }

        private void Update()
        {
            float distance = Blackboard.Instance.DistanceTracker.DistanceTravelled;
            if (!_gameStarted || !_isEnabled || _turnController.TurnFailedDistance - _turnDistance > distance) return;
            switch (_turnController.TurnDirection)
            {
                case Direction.Left:
                    UserInputBus.Publish(UserInitiatedEvents.UserLeftTurnRequested, this, distance);
                    break;
                default:
                    UserInputBus.Publish(UserInitiatedEvents.UserRightTurnRequested, this, distance);
                    break;
            }
        }
        private void OnDestroy()
        {
            TempleRunBus.Unsubscribe(TempleRunEvents.TempleRunStarted, OnTempleRunStarted);
        }
    }
}