using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Maps input events to game events. Will check if a turn request is the proper direction and within 
    ///    the turn distance. If so, it will fire a turn successful event.
    ///    Dependencies: DistanceTracker, EventsPublisherTempleRun
    ///    Subscribes: LeftTurnRequested and RightTurnRequested. If it is a valid turn published corresponding turn successful event.
    ///    Subscribes: ActiveTrackChanged - adjusts the next valid turn distance.
    ///    Publishes: LeftTurnSucceeded, RightTurnSucceeded
    /// </summary>
    internal class PlayerController : MonoBehaviour
    {
        [SerializeField] private DistanceTracker _distanceTracker;
        
        private float _safeTurnDistance = 1f;
        private float _trackDistance = 0;
        float _turnAvailableDistance;
        // Possible Bug: If Direction is changed to a Flag, then _nextTrackDirection needs to be masked. Could be done now just in case.
        private Direction _nextTrackDirection;

        private void Awake()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.LeftTurnRequested, OnLeftTurnRequested);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.RightTurnRequested, OnRightTurnRequested);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.ActiveTrackChanged, OnTrackChanged);
            _safeTurnDistance = Blackboard.Instance.GameConfig.SafeTurnDistance;
        }

        private void OnTurnRequested(object sender, object data, KnownEvents turnSucceedEvent)
        {
            if (_distanceTracker.DistanceTravelled > _turnAvailableDistance)
            {
                float distance = _distanceTracker.DistanceTravelled;
                _distanceTracker.UpdateDistance(_trackDistance - distance);
                EventsPublisherTempleRun.Instance.PublishEvent(turnSucceedEvent, this, distance);
            }
        }

        private void OnLeftTurnRequested(object sender, object data)
        {
            if (_nextTrackDirection != Direction.Right)
            {
                OnTurnRequested(sender, data, KnownEvents.LeftTurnSucceeded);
            }
        }

        private void OnRightTurnRequested(object sender, object data)
        {
            if (_nextTrackDirection != Direction.Left) 
            {
                OnTurnRequested(sender, data, KnownEvents.RightTurnSucceeded);
            }
        }

        private void OnTrackChanged(object sender, object data)
        {
            var (direction, segmentDistance) = ((Direction direction, float segmentDistance)) data;
            _nextTrackDirection = direction;
            _trackDistance += segmentDistance;
            _turnAvailableDistance = _trackDistance - _safeTurnDistance;
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.LeftTurnRequested, OnLeftTurnRequested);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.RightTurnRequested, OnRightTurnRequested);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.ActiveTrackChanged, OnTrackChanged);
        }
    }
}