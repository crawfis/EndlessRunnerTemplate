using CrawfisSoftware.Events;

using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Checks whether a turn request is the proper direction and within the turn distance. If so,
    /// it fires a turn successful event.
    ///    Dependencies: Blackboard, DistanceTracker, EventsFor<TempleRunEvents>
    ///    Subscribes: TempleRunEvents.TurnLeftRequested, TurnRightRequested (from bridge
    ///                translating UserInitiated). If it is a valid turn publishes corresponding turn events.
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

        private static readonly EventId<TrackSegmentInfo> TrackChanging =
            TempleRunBus.Id<TrackSegmentInfo>(TempleRunEvents.ActiveTrackChanging);
        private static readonly EventId<Direction> SegmentRequested =
            TempleRunBus.Id<Direction>(TempleRunEvents.SegmentRequested);

        private void Awake()
        {
            // Subscribe to TempleRun domain events, not UserInitiated.
            // This allows turns to be triggered from any source: player input, AI, replay, network.
            // The bridge translates UserInitiated.UserLeftTurnRequested -> TempleRunEvents.TurnLeftRequested
            TempleRunBus.Subscribe(TempleRunEvents.TurnLeftRequested, OnLeftTurnRequested);
            TempleRunBus.Subscribe(TempleRunEvents.TurnRightRequested, OnRightTurnRequested);
            TrackChanging.Subscribe(OnTrackChanging);
            _safeTurnDistance = Blackboard.Instance.GameConfig.SafePreTurnDistance;
        }

        private void OnTurnRequested(object sender, object data, Direction chosenDirection,
                                     TempleRunEvents startingEvent, TempleRunEvents completedEvent)
        {
            float distance = Blackboard.Instance.DistanceTracker.DistanceTravelled;
            if (distance > _turnAvailableDistance)
            {
                TempleRunBus.Publish(startingEvent,  this, distance);

                // ONLY at an Either junction. A Left or Right segment has one exit, already built
                // when the segment was created, so there is nothing to commit - and publishing this
                // for an ordinary turn is destructive: TrackManager would clear
                // _awaitingEitherDirection and generate straight past a junction still waiting for
                // its direction, while PathProvider would resolve that junction's exit using the
                // direction of an unrelated turn somewhere else on the track.
                //
                // Position between starting and completed is load-bearing: PathProvider resolves the
                // junction's exit geometry from this, and SegmentTransitionController consumes that
                // geometry when it sees the completed event. Publishing returns only once the event
                // has been delivered, so the geometry is in place by the time completed is published.
                if (_nextTrackDirection == Direction.Either)
                    SegmentRequested.Publish(this, chosenDirection);

                TempleRunBus.Publish(completedEvent, this, distance);
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

        private void OnTrackChanging(string eventName, object sender, TrackSegmentInfo trackSegment)
        {
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
            TempleRunBus.Unsubscribe(TempleRunEvents.TurnLeftRequested, OnLeftTurnRequested);
            TempleRunBus.Unsubscribe(TempleRunEvents.TurnRightRequested, OnRightTurnRequested);
            TrackChanging.Unsubscribe(OnTrackChanging);
        }
    }
}
