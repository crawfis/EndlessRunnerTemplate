using System.Collections.Generic;
using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Pure geometry builder. Converts abstract TrackSegments into 3D world positions
    /// and publishes SplineSegmentCreated (for spawners) and SegmentGeometryReady
    /// (for SegmentTransitionController).
    ///    Dependencies: Blackboard.TrackWidthOffset, EventsPublisherTempleRun
    ///    Subscribes: TrackSegmentCreated — builds 3-point geometry
    ///    Subscribes: SegmentRequested — completes Either junction exit geometry
    ///    Publishes: SplineSegmentCreated (data: SplineSegmentData) — per sub-spline, for spawners
    ///    Publishes: SegmentGeometryReady (data: SegmentGeometryData) — full segment geometry
    /// </summary>
    /// <remarks>
    /// Execution order -10 ensures SegmentRequested is processed here (updating anchor/direction)
    /// before TrackManager (default order 0) publishes new TrackSegmentCreated events.
    /// </remarks>
    [DefaultExecutionOrder(-10)]
    public class PathProvider : MonoBehaviour
    {
        [Tooltip("The initial position of the starting track and the character")]
        [SerializeField] private Vector3 _anchorPoint = Vector3.zero;

        private readonly Vector3[] _directionAxes = { new(0, 0, 1), new(1, 0, 0), new(0, 0, -1), new(-1, 0, 0) };
        private int _directionIndex = 0;
        private int _sequenceIndex = 0;

        // Holds the definition of an active Either segment while waiting for
        // SegmentRequested to provide the chosen exit direction.
        private TrackSegmentDefinition _pendingEitherDefinition;
        private int _pendingEitherSequenceIndex;

        private void Awake()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.TrackSegmentCreated, OnTrackCreated);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.SegmentRequested, OnSegmentRequested);
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.TrackSegmentCreated, OnTrackCreated);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.SegmentRequested, OnSegmentRequested);
        }

        private void TurnLeft()
        {
            _directionIndex = (_directionIndex == 0) ? 3 : _directionIndex - 1;
        }

        private void TurnRight()
        {
            _directionIndex = (_directionIndex + 1) % 4;
        }

        /// <summary>
        /// Applies track-width centering offset, rotates the direction index for the given turn,
        /// then re-applies the offset in the new direction.
        /// </summary>
        private void ApplyTurn(Direction direction)
        {
            _anchorPoint -= Blackboard.Instance.TrackWidthOffset * _directionAxes[_directionIndex];
            switch (direction)
            {
                case Direction.Left:  TurnLeft();  break;
                case Direction.Right: TurnRight(); break;
            }
            _anchorPoint += Blackboard.Instance.TrackWidthOffset * _directionAxes[_directionIndex];
        }

        /// <summary>
        /// Publishes a SplineSegmentCreated event and advances the anchor point.
        /// </summary>
        private void PublishSplineSegment(Vector3 start, Vector3 end, Direction direction,
                                          TrackSegmentDefinition definition)
        {
            var splineData = new SplineSegmentData(start, end, direction, definition);
            EventsPublisherTempleRun.Instance.PublishEvent(
                TempleRunEvents.SplineSegmentCreated, this, splineData);
            _anchorPoint = end;
        }

        private void OnTrackCreated(string eventName, object sender, object data)
        {
            var segment = (TrackSegmentInfo)data;
            var definition = segment.Definition;
            int seqIdx = _sequenceIndex++;

            switch (segment.Direction)
            {
                case Direction.Either:
                {
                    // Approach only — exit direction unknown until player swipes.
                    Vector3 entrance = _anchorPoint;
                    Vector3 pivot = entrance + definition.EntranceDistance * _directionAxes[_directionIndex];
                    PublishSplineSegment(entrance, pivot, Direction.Either, definition);
                    _pendingEitherDefinition = definition;
                    _pendingEitherSequenceIndex = seqIdx;

                    PublishGeometry(new SegmentGeometryData
                    {
                        ApproachStart = entrance,
                        Pivot         = pivot,
                        ExitEnd       = pivot,          // placeholder
                        Direction     = Direction.Either,
                        Definition    = definition,
                        SequenceIndex = seqIdx,
                        ExitResolved  = false
                    });
                    break;
                }

                case Direction.Straight:
                {
                    Vector3 entrance = _anchorPoint;
                    Vector3 exit = entrance + definition.EntranceDistance * _directionAxes[_directionIndex];
                    PublishSplineSegment(entrance, exit, Direction.Straight, definition);

                    PublishGeometry(new SegmentGeometryData
                    {
                        ApproachStart = entrance,
                        Pivot         = exit,
                        ExitEnd       = exit,
                        Direction     = Direction.Straight,
                        Definition    = definition,
                        SequenceIndex = seqIdx,
                        ExitResolved  = true
                    });
                    break;
                }

                case Direction.Left:
                case Direction.Right:
                {
                    Vector3 entrance = _anchorPoint;
                    Vector3 approachEnd = entrance + definition.EntranceDistance * _directionAxes[_directionIndex];

                    // Approach sub-spline: entrance -> approachEnd (straight to turn point).
                    PublishSplineSegment(entrance, approachEnd, Direction.Straight, definition);

                    // Rotate direction and apply track-width centering offset.
                    ApplyTurn(segment.Direction);

                    Vector3 adjustedPivot = approachEnd; //  _anchorPoint;
                    Vector3 exit = adjustedPivot + definition.ExitDistance * _directionAxes[_directionIndex];

                    // Exit sub-spline: adjustedPivot -> exit (new direction after teleport).
                    PublishSplineSegment(adjustedPivot, exit, segment.Direction, definition);

                    PublishGeometry(new SegmentGeometryData
                    {
                        ApproachStart = entrance,
                        Pivot         = adjustedPivot,
                        ExitEnd       = exit,
                        Direction     = segment.Direction,
                        Definition    = definition,
                        SequenceIndex = seqIdx,
                        ExitResolved  = true
                    });
                    break;
                }
            }
        }

        /// <summary>
        /// Fires when the player commits a turn direction at an Either junction.
        /// Completes the exit geometry and publishes the exit SplineSegmentCreated.
        /// </summary>
        private void OnSegmentRequested(string eventName, object sender, object data)
        {
            if (_pendingEitherDefinition == null) return;

            var chosen = (Direction)data;
            ApplyTurn(chosen);

            Vector3 adjustedPivot = _anchorPoint;
            Vector3 exit = adjustedPivot + _pendingEitherDefinition.ExitDistance * _directionAxes[_directionIndex];

            if (_pendingEitherDefinition.ExitDistance > 0f)
            {
                PublishSplineSegment(adjustedPivot, exit, chosen, _pendingEitherDefinition);
            }

            // Publish updated geometry with resolved exit.
            PublishGeometry(new SegmentGeometryData
            {
                ApproachStart = adjustedPivot - _pendingEitherDefinition.EntranceDistance * _directionAxes[_directionIndex], // approximate
                Pivot         = adjustedPivot,
                ExitEnd       = exit,
                Direction     = chosen,
                Definition    = _pendingEitherDefinition,
                SequenceIndex = _pendingEitherSequenceIndex,
                ExitResolved  = true
            });

            _pendingEitherDefinition = null;
        }

        private void PublishGeometry(SegmentGeometryData geometry)
        {
            EventsPublisherTempleRun.Instance.PublishEvent(
                TempleRunEvents.SegmentGeometryReady, this, geometry);
        }
    }
}
