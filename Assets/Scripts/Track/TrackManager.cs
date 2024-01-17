using System.Collections.Generic;
using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Provides new track distance for each turn. It does one thing. It publishes a new track segment 
    ///       when needed (when AdvanceToNextSegment is called).
    ///    Dependencies: EventsPublisherTempleRun and (currently) BlackBoard.GameConfig, Blackboard.MasterRandom
    ///        The Blackboard can be removed my having GameController create this instance and passing in data to the constructor.
    ///    Subscribes to the Turn Succeeded events (LeftTurnSucceeded, RightTurnSucceeded)
    ///    Publishes an event each time it provides a new track. Data is a tuple (Direction, distance)
    /// </summary>
    /// <remarks> Obstacle and gap distances should be in a separate class(es).
    /// Random distances (_random) could be replaced with a list of possible distances, but a better / cleaner solution would
    /// be to have another class subscribe to the event, massage the data and publish a new event. This may be needed
    /// for example to map the distance to a number of tiles.</remarks>
    public class TrackManager : TrackManagerAbstract
    {
        // Todo: Remove MonoBehaviour
        const int TrackLength = 3;
        private readonly Queue<(Direction direction,float distance)> _trackSegments = new(TrackLength);
        private float _startDistance = 10f;
        private float _minDistance = 3;
        private float _maxDistance = 9;
        private System.Random _random;

        private void Awake()
        {
            // Todo: Remove Awake and move to a constructor w/o the Blackboard.
            var gameConfig = Blackboard.Instance.GameConfig;
            Initialize(gameConfig.StartRunway, gameConfig.MinDistance,
                gameConfig.MaxDistance, Blackboard.Instance.MasterRandom);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.GameStarted, OnGameStarted);
        }

        public void OnGameStarted(object sender, object data)
        {
            CreateInitialTrack();
        }

        public void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.LeftTurnSucceeded, OnTurnSucceeded);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.RightTurnSucceeded, OnTurnSucceeded);
        }

        public override void AdvanceToNextSegment()
        {
            _ = _trackSegments.Dequeue();
            AddTrackSegment();
            EventsPublisherTempleRun.Instance.PublishEvent(KnownEvents.ActiveTrackChanged, this, _trackSegments.Peek());
        }

        private void Initialize(float startDistance, float minDistance, float maxDistance, System.Random random)
        {
            _startDistance = startDistance;
            _minDistance = minDistance;
            _maxDistance = maxDistance;
            _random = random;
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.LeftTurnSucceeded, OnTurnSucceeded);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.RightTurnSucceeded, OnTurnSucceeded);
        }

        private void CreateInitialTrack()
        {
            _maxDistance = Mathf.Max(_minDistance, _maxDistance);
            var newTrackSegment = (GetNewDirection(), _startDistance);
            _trackSegments.Enqueue(newTrackSegment);
            EventsPublisherTempleRun.Instance.PublishEvent(KnownEvents.TrackSegmentCreated, this, newTrackSegment);
            for (int i = 1; i < TrackLength; i++)
            {
                AddTrackSegment();
            }
            EventsPublisherTempleRun.Instance.PublishEvent(KnownEvents.ActiveTrackChanged, this, _trackSegments.Peek());
        }

        private void OnTurnSucceeded(object sender, object data)
        {
            AdvanceToNextSegment();
        }

        private void AddTrackSegment()
        {
            float segmentLength = (float)_random.NextDouble() * (_maxDistance - _minDistance) + _minDistance;
            var newTrackSegment = (GetNewDirection(), segmentLength);
            _trackSegments.Enqueue(newTrackSegment);
            EventsPublisherTempleRun.Instance.PublishEvent(KnownEvents.TrackSegmentCreated, this, newTrackSegment);
        }

        private Direction GetNewDirection()
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