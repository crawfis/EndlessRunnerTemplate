using System;
using System.Collections.Generic;

using UnityEngine;

using CrawfisSoftware.Events;
using CrawfisSoftware.TempleRun.GameConfig;
using CrawfisSoftware.TempleRun.Track;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Provides new track distance for each turn. It publishes a new track segment
    ///       when needed (either to create visuals or to determine the currently active track).
    ///    Dependencies: EventsFor<TempleRunEvents>, Blackboard.GameConfig, Blackboard.MasterRandom,
    ///                  TrackLevelApplied (Sticky; read at init via TryGetLast), _trackLevels registry
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
        [SerializeField] private TrackLevelRegistrySO _trackLevels;

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

        private static readonly EventId<int> LevelApplied =
            TempleRunBus.Id<int>(TempleRunEvents.TrackLevelApplied);
        private static readonly EventId<TrackSegmentInfo> TrackSegmentCreated =
            TempleRunBus.Id<TrackSegmentInfo>(TempleRunEvents.TrackSegmentCreated);
        private static readonly EventId<TrackSegmentInfo> ActiveTrackChanging =
            TempleRunBus.Id<TrackSegmentInfo>(TempleRunEvents.ActiveTrackChanging);
        private static readonly EventId<TrackSegmentInfo> ActiveTrackChanged =
            TempleRunBus.Id<TrackSegmentInfo>(TempleRunEvents.ActiveTrackChanged);

        // Set when an Either (T-junction) segment is at the tail of the lookahead queue.
        // No further segments are generated until SegmentRequested fires with the chosen direction.
        private bool _awaitingEitherDirection = false;

        // Set when a junction direction is committed; acted on in Update, out of the dispatch.
        private bool _refillRequested = false;


        protected virtual void Awake()
        {
            TempleRunBus.Subscribe(TempleRunEvents.RunInitializeRequested, OnGameStarting);
            TempleRunBus.Subscribe(TempleRunEvents.SegmentRequested, OnSegmentRequested);
        }

        protected virtual void OnDestroy()
        {
            TempleRunBus.Unsubscribe(TempleRunEvents.RunInitializeRequested, OnGameStarting);
            TempleRunBus.Unsubscribe(TempleRunEvents.SegmentExited, OnSegmentCompleted);
            TempleRunBus.Unsubscribe(TempleRunEvents.SegmentRequested, OnSegmentRequested);
        }

        private void Start()
        {
            _trackSegments = new(_numberOfLookAheadTracks);
        }

        private void Initialize()
        {
            var gameConfig = Blackboard.Instance.GameConfig;
            Initialize(gameConfig.StartRunway, gameConfig.MinTrackLength,
                gameConfig.MaxTrackLength, Blackboard.Instance.MasterRandom);
        }

        protected virtual void OnGameStarting(string eventName, object sender, object data)
        {
            Initialize();
            CreateInitialTrack();
        }

        public override void AdvanceToNextSegment()
        {
            _ = _trackSegments.Dequeue();
            if (!_awaitingEitherDirection)
                AddTrackSegment();
            ActiveTrackChanging.Publish(this, _trackSegments.Peek());
        }

        protected virtual void Initialize(float startDistance, float minDistance, float maxDistance, System.Random random)
        {
            _startDistance = startDistance;
            _minDistance = minDistance;
            _maxDistance = maxDistance;
            _random = random;
            _awaitingEitherDirection = false;

            // Resolve the selected level's track. TrackLevelApplied is published (bridged from
            // GameFlow) before this scene and TrackManager exist, so it is Sticky and read here
            // rather than mirrored into a field. Never published means no level was selected, which
            // is level 0. TrackLibraryLoader reads the authoring SOs and builds the runtime library;
            // a null result leaves the procedural fallback in CreateTrackSegment in charge.
            int selectedLevel = LevelApplied.TryGetLast(out _, out int level) ? level : 0;
            _segmentLibrary = TrackLibraryLoader.Load(_trackLevels, selectedLevel);
            TempleRunBus.Subscribe(TempleRunEvents.SegmentExited, OnSegmentCompleted);
        }

        protected virtual void CreateInitialTrack()
        {
            _maxDistance = Mathf.Max(_minDistance, _maxDistance);
            _awaitingEitherDirection = false;
            var newTrackSegment = CreateTrackSegment(isStartSegment: true);
            _trackSegments.Enqueue(newTrackSegment);
            TrackSegmentCreated.Publish(this, newTrackSegment);
            for (int i = 1; i < _numberOfLookAheadTracks; i++)
            {
                AddTrackSegment();
                if (_awaitingEitherDirection) break;
            }
            ActiveTrackChanging.Publish(this, _trackSegments.Peek());
        }

        /// <summary>
        /// Handles SegmentExited for ALL segment types (the single advancement path).
        /// Advancement always waits for the player to fully exit the segment.
        /// </summary>
        protected virtual void OnSegmentCompleted(string eventName, object sender, object data)
        {
            ActiveTrackChanged.Publish(this, _trackSegments.Peek());
            AdvanceToNextSegment();
        }

        protected virtual void AddTrackSegment()
        {
            var newTrackSegment = CreateTrackSegment(isStartSegment: false);
            _trackSegments.Enqueue(newTrackSegment);
            TrackSegmentCreated.Publish(this, newTrackSegment);
            if (newTrackSegment.Direction == Direction.Either)
            {
                _awaitingEitherDirection = true;
            }
        }

        /// <summary>
        /// Fires when the player commits a direction at an Either junction.
        /// Resumes lookahead generation using the normal fill logic.
        /// PathProvider (execution order -10) processes this event first, updating _anchorPoint
        /// before the TrackSegmentCreated events fired here reach PathProvider.
        /// </summary>
        // The junction's direction is committed, so generation can resume - but NOT from inside this
        // dispatch. PathProvider handles the same event before us and publishes while it works;
        // publishing re-enters the shared FIFO drain, which resumes with this handler still queued.
        // Generating here therefore ran mid-way through PathProvider building the junction's exit,
        // and the next segment's SegmentGeometryReady overtook the junction's own - closing
        // SpawnerBase's pending batch onto the wrong segment, so the junction's exit tiles were
        // attributed to the segment after it and destroyed on that segment's schedule.
        // Ordering cannot fix this: TrackManager is in TempleRunTrackPCG and the spawners are in
        // other additively-loaded scenes, where DefaultExecutionOrder does not apply. Deferring one
        // frame takes us out of the dispatch entirely, and costs nothing - the player is at the
        // junction, many segments from the end of the lookahead.
        private void OnSegmentRequested(string eventName, object sender, object data)
        {
            _refillRequested = true;
        }

        private void Update()
        {
            if (!_refillRequested) return;
            _refillRequested = false;

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

            // A turn spends its last stretch running out of the corner; a Straight has no exit
            // section at all. Splitting the length here keeps the total at segmentLength.
            float exitDistance = fallbackDirection == Direction.Straight
                ? 0f
                : TempleRunConstants.MinimumTurnExitDistance;

            var fallbackDef = new TrackSegmentDefinition
            {
                Id              = "random",
                Direction       = fallbackDirection,
                ToPivotDistance = segmentLength - exitDistance,
                ExitDistance    = exitDistance
            };

            // Inline definitions skip the registry, so they must be normalized explicitly —
            // otherwise TurnFailureDistance stays 0 and the player fails the turn immediately.
            TrackSegmentLibrary.Normalize(fallbackDef);
            return new TrackSegmentInfo(fallbackDef, fallbackDef.Direction);
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
