using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Deterministic (and perfect?) AI that triggers a turn request whenever the current
    /// distances gets within a user-specified value of the end of the currently discovered track.
    ///    Dependency: TurnController, EventsPublisherTempleRun
    ///    Subscribes: GameStarted
    ///    Publishes: LeftTurnRequested
    ///    Publishes: RightTurnRequested
    /// </summary>
    public class AIController : MonoBehaviour
    {
        [SerializeField] private TurnController _turnController;
        [Tooltip("Distance from far wall to turn. Should be between (0,opening size]. Can try to turn easy but the difficulty config will determine if possible.")]
        [SerializeField] private float _turnDistance = .1f;

        private bool _gameStarted = false;

        private void Awake()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.GameStarted, OnGameStarted);
        }

        private void OnGameStarted(object arg1, object arg2)
        {
            _gameStarted = true;
        }

        private void Update()
        {
            float distance = Blackboard.Instance.DistanceTracker.DistanceTravelled;
            if (!_gameStarted || _turnController.TurnFailedDistance - _turnDistance > distance) return;
            switch (_turnController.TurnDirection)
            {
                case Direction.Left:
                    EventsPublisherTempleRun.Instance.PublishEvent(KnownEvents.LeftTurnRequested, this, distance);
                    break;
                default:
                    EventsPublisherTempleRun.Instance.PublishEvent(KnownEvents.RightTurnRequested, this, distance);
                    break;
            }
        }
    }
}