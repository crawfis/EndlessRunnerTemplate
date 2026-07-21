using System;
using System.Collections.Generic;

using UnityEngine;

using CrawfisSoftware.TempleRun.Track;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Provides new track distance for each turn. It publishes a new track segment
    ///       when needed (either to create visuals or to determine the currently active track).
    ///    Dependencies: EventsPublisherTempleRun, Blackboard.GameConfig, Blackboard.MasterRandom,
    ///                  Blackboard.TrackLevelDefinition (set by level selection)
    ///    Subscribes to SegmentExited for all segment types (single advancement path)
    ///    Subscribes to SegmentRequested to resume lookahead after an Either (T-junction) segment
    ///    Publishes: TrackSegmentCreated. Useful for creating prefabs. Several of these will be created at the start. Data is a TrackSegmentInfo
    ///    Publishes: ActiveTrackChanging. The track that we are transitioning to. Data is a TrackSegmentInfo
    ///    Publishes: ActiveTrackChanged. The track segment that was just fully exited. Data is a TrackSegmentInfo. Fires before ActiveTrackChanging.
    /// </summary>
    /// <remarks> Obstacle and gap distances should be in a separate class(es).
    /// Random distances (_random) could be replaced with a list of possible distances, but a better / cleaner solution would
    /// be to have another class subscribe to the event, massage the data and publish a new event. This may be needed
    /// for example to map the distance to a number of tiles.</remarks>
    /// <remarks>Used as a base class for integer-based tracks (voxels or tiles) and a fixed set of track lengths.</remarks>
    public class TrackManager : TrackManagerAbstract
    {
        [SerializeField] int _numberOfLookAheadTracks = 12;
        [SerializeField] private TextAsset _trackSegmentLibraryJson;

        protected Queue<TrackSegmentInfo> _trackSegments;
        protected float _startDistance = 10f;
        protected float _minDistance = 3;
        protected float _maxDistance = 9;
        protected System.Random _random;
        private TrackSegmentLibrary _segmentLibrary;
        // The pluggable selection policy. Default reproduces the previous
        // TrackSegmentLibrary.SelectNext behaviour exactly (ungated weighted random).
        private ISegmentSelector _selector = new WeightedDifficultySelector();
        private string _lastSegmentId;
        private TrackSegmentDefinition _lastSegmentDefinition;
        private int _lastSegmentRepeatCount;
        private int _segmentIndex;
        private bool _isInitialized = false;

        // Set when an Either (T-junction) segment is at the tail of the lookahead queue.
        // No further segments are generated until SegmentRequested fires with the chosen direction.
        private bool _awaitingEitherDirection = false;


        protected virtual void Awake()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.TempleRunScenesReady, OnGameStarting);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.TempleRunConfigApplied, OnGameConfigured);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.SegmentRequested, OnSegmentRequested);
        }

        protected virtual void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.TempleRunScenesReady, OnGameStarting);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.TempleRunConfigApplied, OnGameConfigured);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.SegmentExited, OnSegmentCompleted);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.SegmentRequested, OnSegmentRequested);
        }

        private void Start()
        {
            _trackSegments = new(_numberOfLookAheadTracks);
        }

        private void OnGameConfigured(string eventName, object sender, object data)
        {
            Initialize();
        }

        private void Initialize()
        {
            var gameConfig = Blackboard.Instance.GameConfig;
            Initialize(gameConfig.StartRunway, gameConfig.MinTrackLength,
                gameConfig.MaxTrackLength, Blackboard.Instance.MasterRandom);
            _isInitialized = true;
        }

        protected virtual void OnGameStarting(string eventName, object sender, object data)
        {
            if(!_isInitialized)
            {
                Initialize();
            }
            CreateInitialTrack();
        }

        public override void AdvanceToNextSegment()
        {
            _ = _trackSegments.Dequeue();
            if (!_awaitingEitherDirection)
                AddTrackSegment();
            EventsPublisherTempleRun.Instance.PublishEvent(TempleRunEvents.ActiveTrackChanging, this, _trackSegments.Peek());
        }

        protected virtual void Initialize(float startDistance, float minDistance, float maxDistance, System.Random random)
        {
            _startDistance = startDistance;
            _minDistance = minDistance;
            _maxDistance = maxDistance;
            _random = random;
            _awaitingEitherDirection = false;

            // Build runtime library from Blackboard's level definition + registry
            var levelDef = Blackboard.Instance.TrackLevelDefinition;
            if (levelDef != null)
            {
                string registryJson = null;
                if (!string.IsNullOrWhiteSpace(levelDef.SegmentRegistryFile))
                {
                    var registryAsset = Resources.Load<TextAsset>(levelDef.SegmentRegistryFile);
                    registryJson = registryAsset?.text;
                }
                _segmentLibrary = TrackSegmentLibrary.LoadFromDefinition(levelDef, registryJson);
            }
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.SegmentExited, OnSegmentCompleted);
        }

        protected virtual void CreateInitialTrack()
        {
            _maxDistance = Mathf.Max(_minDistance, _maxDistance);
            _awaitingEitherDirection = false;
            var newTrackSegment = CreateTrackSegment(isStartSegment: true);
            _trackSegments.Enqueue(newTrackSegment);
            EventsPublisherTempleRun.Instance.PublishEvent(TempleRunEvents.TrackSegmentCreated, this, newTrackSegment);
            for (int i = 1; i < _numberOfLookAheadTracks; i++)
            {
                AddTrackSegment();
                if (_awaitingEitherDirection) break;
            }
            EventsPublisherTempleRun.Instance.PublishEvent(TempleRunEvents.ActiveTrackChanging, this, _trackSegments.Peek());
        }

        /// <summary>
        /// Handles SegmentExited for ALL segment types (the single advancement path).
        /// Advancement always waits for the player to fully exit the segment.
        /// </summary>
        protected virtual void OnSegmentCompleted(string eventName, object sender, object data)
        {
            EventsPublisherTempleRun.Instance.PublishEvent(TempleRunEvents.ActiveTrackChanged, this, _trackSegments.Peek());
            AdvanceToNextSegment();
        }

        protected virtual void AddTrackSegment()
        {
            var newTrackSegment = CreateTrackSegment(isStartSegment: false);
            _trackSegments.Enqueue(newTrackSegment);
            EventsPublisherTempleRun.Instance.PublishEvent(TempleRunEvents.TrackSegmentCreated, this, newTrackSegment);
            if (newTrackSegment.Direction == Direction.Either)
                _awaitingEitherDirection = true;
        }

        /// <summary>
        /// Fires when the player commits a direction at an Either junction.
        /// Resumes lookahead generation using the normal fill logic.
        /// PathProvider (execution order -10) processes this event first, updating _anchorPoint
        /// before the TrackSegmentCreated events fired here reach PathProvider.
        /// </summary>
        private void OnSegmentRequested(string eventName, object sender, object data)
        {
            _awaitingEitherDirection = false;
            while (!_awaitingEitherDirection && _trackSegments.Count < _numberOfLookAheadTracks)
                AddTrackSegment();
        }

        protected virtual TrackSegmentInfo CreateTrackSegment(bool isStartSegment)
        {
            if (_segmentLibrary != null)
            {
                // The library is the read-only data view; the selector is the policy.
                ISegmentPool pool = _segmentLibrary;

                // Thread the same state the old TrackSegmentLibrary calls used:
                //   Previous            <- _lastSegmentDefinition (Previous?.Id == _lastSegmentId)
                //   PreviousRepeatCount <- _lastSegmentRepeatCount
                //   Random              <- _random (same seeded instance)
                // DistanceTravelled/SegmentIndex are new context only used by
                // distance-/index-aware selectors; the default selector ignores them.
                var distanceTracker = Blackboard.Instance.DistanceTracker;
                var ctx = new SelectionContext(
                    _lastSegmentDefinition,
                    _lastSegmentRepeatCount,
                    distanceTracker != null ? distanceTracker.DistanceTravelled : 0f,
                    _segmentIndex,
                    _random);

                var segmentDefinition = isStartSegment
                    ? _selector.SelectStart(pool, ctx)
                    : _selector.SelectNext(pool, ctx);

                if (segmentDefinition != null)
                {
                    UpdateRepeatTracking(segmentDefinition.Id);
                    _lastSegmentDefinition = segmentDefinition;
                    _segmentIndex++;
                    var direction = segmentDefinition.Direction;
                    return new TrackSegmentInfo(segmentDefinition, direction);
                }
            }

            float segmentLength = isStartSegment ? _startDistance : GetNewSegmentLength();
            var fallbackDirection = GetNewDirection();
            var fallbackDef = new TrackSegmentDefinition
            {
                Id             = "random",
                Direction      = fallbackDirection,
                Length         = segmentLength,
                EntranceDistance = segmentLength  // ensure normalization is correct for inline defs
            };
            return new TrackSegmentInfo(fallbackDef, fallbackDirection);
        }

        private static Direction ParseDirection(string directionValue, Direction fallback)
        {
            if (!string.IsNullOrWhiteSpace(directionValue) && Enum.TryParse(directionValue, true, out Direction parsed))
            {
                return parsed;
            }

            return fallback;
        }

        private void UpdateRepeatTracking(string segmentId)
        {
            if (string.Equals(_lastSegmentId, segmentId, System.StringComparison.Ordinal))
            {
                _lastSegmentRepeatCount++;
            }
            else
            {
                _lastSegmentId = segmentId;
                _lastSegmentRepeatCount = 1;
            }
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
