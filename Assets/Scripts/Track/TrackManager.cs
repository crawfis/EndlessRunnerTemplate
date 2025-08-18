using System.Collections.Generic;

using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Provides new track distance for each turn. It publishes a new track segment 
    ///       when needed (either to create visuals or to determine the currently active track).
    ///    Dependencies: EventsPublisherTempleRun and (currently) BlackBoard.GameConfig, Blackboard.MasterRandom
    ///        The Blackboard can be removed my having GameController create this instance and passing in data to the constructor.
    ///    Subscribes to the Turn Succeeded events (LeftTurnSucceeded, RightTurnSucceeded)
    ///    Publishes: TrackSegmentCreated. Useful for creating prefabs. Several of these will be created at the start. Data is a tuple (Direction, distance)
    ///    Publishes: ActiveTrackChanging. The track that we are transitioning to. Data is a tuple (Direction, distance)
    /// </summary>
    /// <remarks> Obstacle and gap distances should be in a separate class(es).
    /// Random distances (_random) could be replaced with a list of possible distances, but a better / cleaner solution would
    /// be to have another class subscribe to the event, massage the data and publish a new event. This may be needed
    /// for example to map the distance to a number of tiles.</remarks>
    /// <remarks>Used as a base class for integer-based tracks (voxels or tiles) and a fixed set of track lengths.</remarks>
    public class TrackManager : TrackManagerAbstract
    {
        // Todo: Remove MonoBehaviour
        protected const int TrackLength = 12;
        protected readonly Queue<(Direction direction, float distance)> _trackSegments = new(TrackLength);
        protected float _startDistance = 10f;
        protected float _minDistance = 3;
        protected float _maxDistance = 9;
        protected System.Random _random;

        protected virtual void Awake()
        {
            // Todo: Remove Awake and move to a constructor w/o the Blackboard.
            var gameConfig = Blackboard.Instance.GameConfig;
            Initialize(gameConfig.StartRunway, gameConfig.MinDistance,
                gameConfig.MaxDistance, Blackboard.Instance.MasterRandom);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.GameStarted, OnGameStarted);
        }

        protected virtual void OnGameStarted(string eventName, object sender, object data)
        {
            CreateInitialTrack();
        }

        protected virtual void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.GameStarted, OnGameStarted);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.LeftTurnSucceeded, OnTurnSucceeded);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.RightTurnSucceeded, OnTurnSucceeded);
        }

        public override void AdvanceToNextSegment()
        {
            _ = _trackSegments.Dequeue();
            AddTrackSegment();
            EventsPublisherTempleRun.Instance.PublishEvent(KnownEvents.ActiveTrackChanging, this, _trackSegments.Peek());
        }

        protected virtual void Initialize(float startDistance, float minDistance, float maxDistance, System.Random random)
        {
            _startDistance = startDistance;
            _minDistance = minDistance;
            _maxDistance = maxDistance;
            _random = random;
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.LeftTurnSucceeded, OnTurnSucceeded);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.RightTurnSucceeded, OnTurnSucceeded);
        }

        protected virtual void CreateInitialTrack()
        {
            _maxDistance = Mathf.Max(_minDistance, _maxDistance);
            var newTrackSegment = (GetNewDirection(), _startDistance);
            _trackSegments.Enqueue(newTrackSegment);
            EventsPublisherTempleRun.Instance.PublishEvent(KnownEvents.TrackSegmentCreated, this, newTrackSegment);
            for (int i = 1; i < TrackLength; i++)
            {
                AddTrackSegment();
            }
            EventsPublisherTempleRun.Instance.PublishEvent(KnownEvents.ActiveTrackChanging, this, _trackSegments.Peek());
        }

        protected virtual void OnTurnSucceeded(string eventName, object sender, object data)
        {
            AdvanceToNextSegment();
        }

        protected virtual void AddTrackSegment()
        {
            float segmentLength = GetNewSegmentLength();
            var newTrackSegment = (GetNewDirection(), segmentLength);
            _trackSegments.Enqueue(newTrackSegment);
            EventsPublisherTempleRun.Instance.PublishEvent(KnownEvents.TrackSegmentCreated, this, newTrackSegment);
        }

        protected virtual float GetNewSegmentLength()
        {
            return (float)_random.NextDouble() * (_maxDistance - _minDistance) + _minDistance;
        }

        protected virtual Direction GetNewDirection()
        {
            float randomValue = (float)_random.NextDouble();
            return randomValue switch
            {
                < 0.4f => Direction.Left,
                < 0.8f => Direction.Right,
                _ => Direction.Left,
            };
        }
    }
}