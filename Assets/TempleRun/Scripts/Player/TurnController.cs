using CrawfisSoftware.Events;

using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Maps input events to game events. Will check if a turn request is the proper direction and within
    ///    the turn distance. If so, it will fire a turn successful event.
    ///    Dependencies: Blackboard, DistanceTracker, EventsPublisherTempleRun
    ///    Subscribes: LeftTurnRequested and RightTurnRequested. If it is a valid turn publishes corresponding turn events.
    ///    Subscribes: ActiveTrackChanged - adjusts the next valid turn distance.
    ///    Publishes: TurnLeftStarting, TurnLeftCompleted, TurnRightStarting, TurnRightCompleted
    ///    Publishes: SegmentRequested (data: Direction) when direction is committed at an Either junction
    /// </summary>
    public class TurnController : MonoBehaviour
    {
        public float TurnAvailableDistance { get { return _turnAvailableDistance; } }
        public float TurnFailedDistance { get { return _trackDistance; } }
        public Direction TurnDirection { get { return _nextTrackDirection; } }

        private float _safeTurnDistance = 1f;
        private float _trackDistance = 0;
        private float _turnAvailableDistance;
        // Cumulative distance at the START of the current segment, accumulated from segment
        // lengths so it matches the boundaries used by SegmentAdvanceTrigger and
        // TurnCollisionDetector.
        private float _segmentStartDistance = 0f;
        private float _previousSegmentLength = 0f;
        // Possible Bug: If Direction is changed to a Flag, then _nextTrackDirection needs to be masked.
        private Direction _nextTrackDirection;

        public void ForceTurn()
        {
            Direction chosenDirection;
            TempleRunEvents startingEvent;
            TempleRunEvents completedEvent;

            switch (_nextTrackDirection)
            {
                case Direction.Right:
                    chosenDirection = Direction.Right;
                    startingEvent   = TempleRunEvents.TurnRightStarting;
                    completedEvent  = TempleRunEvents.TurnRightCompleted;
                    break;
                case Direction.Either:
                case Direction.Left:
                default:
                    chosenDirection = Direction.Left;
                    startingEvent   = TempleRunEvents.TurnLeftStarting;
                    completedEvent  = TempleRunEvents.TurnLeftCompleted;
                    break;
            }
            OnTurnRequested(this, null, chosenDirection, startingEvent, completedEvent);
        }

        private void Awake()
        {
            EventsPublisherUserInitiated.Instance.SubscribeToEvent(UserInitiatedEvents.UserLeftTurnRequested, OnLeftTurnRequested);
            EventsPublisherUserInitiated.Instance.SubscribeToEvent(UserInitiatedEvents.UserRightTurnRequested, OnRightTurnRequested);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.ActiveTrackChanging, OnTrackChanging);
            _safeTurnDistance = Blackboard.Instance.GameConfig.SafePreTurnDistance;
        }

        private void OnTurnRequested(object sender, object data, Direction chosenDirection,
                                     TempleRunEvents startingEvent, TempleRunEvents completedEvent)
        {
            float distance = Blackboard.Instance.DistanceTracker.DistanceTravelled;
            if (distance > _turnAvailableDistance)
            {
                EventsPublisherTempleRun.Instance.PublishEvent(startingEvent,  this, distance);
                //EventsPublisherTempleRun.Instance.PublishEvent(TempleRunEvents.SegmentRequested, this, chosenDirection);
                EventsPublisherTempleRun.Instance.PublishEvent(completedEvent, this, distance);
            }
        }

        private void OnLeftTurnRequested(string eventName, object sender, object data)
        {
            if (_nextTrackDirection == Direction.Left || _nextTrackDirection == Direction.Either)
            {
                OnTurnRequested(sender, data, Direction.Left,
                                TempleRunEvents.TurnLeftStarting, TempleRunEvents.TurnLeftCompleted);
            }
        }

        private void OnRightTurnRequested(string eventName, object sender, object data)
        {
            if (_nextTrackDirection == Direction.Right || _nextTrackDirection == Direction.Either)
            {
                OnTurnRequested(sender, data, Direction.Right,
                                TempleRunEvents.TurnRightStarting, TempleRunEvents.TurnRightCompleted);
            }
        }

        private void OnTrackChanging(string eventName, object sender, object data)
        {
            var trackSegment = (TrackSegmentInfo)data;
            _nextTrackDirection  = trackSegment.Direction;
            // Anchor to this segment's start, not to the running sum of turn points. Summing
            // TurnPointDistance loses (Length - TurnPointDistance) per segment, which walked the
            // turn window earlier and earlier; for a Straight (TurnPointDistance == float.MaxValue)
            // it saturated _trackDistance to Infinity and disabled every later turn.
            _segmentStartDistance += _previousSegmentLength;
            _previousSegmentLength = trackSegment.Length;
            _trackDistance = _segmentStartDistance + trackSegment.TurnPointDistance;
            _turnAvailableDistance = _trackDistance - _safeTurnDistance;
        }

        private void OnDestroy()
        {
            EventsPublisherUserInitiated.Instance.UnsubscribeToEvent(UserInitiatedEvents.UserLeftTurnRequested, OnLeftTurnRequested);
            EventsPublisherUserInitiated.Instance.UnsubscribeToEvent(UserInitiatedEvents.UserRightTurnRequested, OnRightTurnRequested);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.ActiveTrackChanging, OnTrackChanging);
        }
    }
}
