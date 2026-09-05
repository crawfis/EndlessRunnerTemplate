using CrawfisSoftware.Events;

using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;
using UserInputBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.Events.UserInitiatedEvents>;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Deterministic (and perfect?) AI that triggers a turn request whenever the current
    /// distances gets within a user-specified value of the end of the currently discovered track.
    ///
    /// <para>It is an input source and nothing else: it publishes the same
    /// <c>UserInitiatedEvents</c> the input actions do, and everything it knows about the turn
    /// ahead arrives on the segment message it already subscribes to. It holds no reference to
    /// any other controller - which is what makes it a demonstration that the player is
    /// replaceable rather than a counter-example.</para>
    ///    Dependency: Blackboard, DistanceTracker, EventsFor<TempleRunEvents>, EventsFor<UserInitiatedEvents>
    ///    Subscribes: TempleRunEvents.PlayerActivated — the autopilot arms when the player is
    ///                released, not when the run's systems come up
    ///    Subscribes: TempleRunEvents.TurnLeftStarting, TurnRightStarting — a turn is under way,
    ///                stop asking for it
    ///    Subscribes: TempleRunEvents.ActiveTrackChanging — a new segment: a new turn to ask for,
    ///                its direction, and the distance by which it must be taken
    ///    Publishes: UserInitiatedEvents.UserLeftTurnRequested
    ///    Publishes: UserInitiatedEvents.UserRightTurnRequested
    /// </summary>
    public class AIController : MonoBehaviour
    {
        [Tooltip("Distance from far wall to turn. Should be between (0,opening size]. Can try to turn easy but the difficulty config will determine if possible.")]
        [SerializeField] private float _turnDistance = .1f;
        [SerializeField] private bool _isEnabled = true;

        private bool _gameStarted = false;

        // Set when the gate accepts a turn, cleared when the track moves on. Update() has no other
        // memory: it re-tests a distance window every frame, so without this it re-asks on every
        // frame of a turn it already got - and a turn lasts as long as the teleport, with distance
        // frozen throughout, so the window cannot close on its own. Latching on Starting rather
        // than on the request means a turn the gate rejects is retried next frame, as before.
        private bool _turnUnderway = false;

        // The turn window, read off the segment message rather than off TurnController. Both values
        // are run-absolute / already resolved when they arrive, so there is nothing to convert.
        private float _turnFailureDistance = float.MaxValue;
        private Direction _nextTrackDirection;

        private void Awake()
        {
            TempleRunBus.Subscribe(TempleRunEvents.PlayerActivated, OnPlayerActivated);
            TempleRunBus.Subscribe(TempleRunEvents.TurnLeftStarting, OnTurnStarting);
            TempleRunBus.Subscribe(TempleRunEvents.TurnRightStarting, OnTurnStarting);
            TempleRunBus.Subscribe(TempleRunEvents.ActiveTrackChanging, OnTrackChanging);
        }

        private void OnPlayerActivated(string eventName, object sender, object data)
        {
            _gameStarted = true;
        }

        private void OnTurnStarting(string eventName, object sender, object data)
        {
            _turnUnderway = true;
        }

        private void OnTrackChanging(string eventName, object sender, object data)
        {
            var segment = (TrackSegmentInfo)data;
            _turnUnderway = false;
            _turnFailureDistance = segment.TurnFailureDistance;
            _nextTrackDirection = segment.Direction;
        }

        private void Update()
        {
            float distance = Blackboard.Instance.DistanceTracker.DistanceTravelled;
            if (!_gameStarted || !_isEnabled || _turnUnderway) return;
            if (_turnFailureDistance - _turnDistance > distance) return;
            switch (_nextTrackDirection)
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
            TempleRunBus.Unsubscribe(TempleRunEvents.PlayerActivated, OnPlayerActivated);
            TempleRunBus.Unsubscribe(TempleRunEvents.TurnLeftStarting, OnTurnStarting);
            TempleRunBus.Unsubscribe(TempleRunEvents.TurnRightStarting, OnTurnStarting);
            TempleRunBus.Unsubscribe(TempleRunEvents.ActiveTrackChanging, OnTrackChanging);
        }
    }
}