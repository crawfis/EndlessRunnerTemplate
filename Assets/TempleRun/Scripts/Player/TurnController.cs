using CrawfisSoftware.Events;

using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// The turn gate, and only the gate. It answers one question — may the player turn that way,
    /// here, right now? — and announces the answer by publishing <c>Turn*Starting</c>.
    ///
    /// A request is legal when the active segment bends the way the player asked (or is an Either
    /// junction, which accepts both) and the player has reached the turn window. Everything that
    /// happens *because* a turn started — committing an Either junction to a direction, and
    /// announcing the turn's progress up the rest of the ladder — belongs to
    /// <see cref="SegmentCommitController"/>. This class publishes one rung and stops.
    ///
    /// It owns the turn window because it is the thing that tests against it; AIController reads
    /// <see cref="TurnFailedDistance"/> and <see cref="TurnDirection"/> to decide when to ask.
    ///    Dependencies: Blackboard, DistanceTracker
    ///    Subscribes: TempleRunEvents.TurnLeftRequested, TurnRightRequested — from the input
    ///                bridge, AIController, or any future replay/netcode source
    ///    Subscribes: TempleRunEvents.ActiveTrackChanging — moves the window to the new segment
    ///    Publishes: TempleRunEvents.TurnLeftStarting, TurnRightStarting
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

        private static readonly EventId<TrackSegmentInfo> TrackChanging =
            TempleRunBus.Id<TrackSegmentInfo>(TempleRunEvents.ActiveTrackChanging);

        /// <summary>
        /// Turns without waiting for a request — the auto-turn after the player has already failed
        /// a turn. The direction comes from the segment rather than from input, but the window check
        /// still applies, exactly as it did when this lived inside the request path.
        /// </summary>
        public void ForceTurn()
        {
            TryTurn(_nextTrackDirection == Direction.Right ? Direction.Right : Direction.Left);
        }

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

        private void OnDestroy()
        {
            TempleRunBus.Unsubscribe(TempleRunEvents.TurnLeftRequested, OnLeftTurnRequested);
            TempleRunBus.Unsubscribe(TempleRunEvents.TurnRightRequested, OnRightTurnRequested);
            TrackChanging.Unsubscribe(OnTrackChanging);
        }

        private void OnLeftTurnRequested(string eventName, object sender, object data)
        {
            if (_nextTrackDirection == Direction.Left || _nextTrackDirection == Direction.Either)
                TryTurn(Direction.Left);
        }

        private void OnRightTurnRequested(string eventName, object sender, object data)
        {
            if (_nextTrackDirection == Direction.Right || _nextTrackDirection == Direction.Either)
                TryTurn(Direction.Right);
        }

        /// <summary>The gate. Publishes the Starting rung if the player is inside the turn window.</summary>
        private void TryTurn(Direction direction)
        {
            float distance = Blackboard.Instance.DistanceTracker.DistanceTravelled;
            if (distance <= _turnAvailableDistance) return;

            TempleRunBus.Publish(
                direction == Direction.Right
                    ? TempleRunEvents.TurnRightStarting
                    : TempleRunEvents.TurnLeftStarting,
                this, distance);
        }

        private void OnTrackChanging(string eventName, object sender, TrackSegmentInfo trackSegment)
        {
            _nextTrackDirection = trackSegment.Direction;
            // Anchor to this segment's start, not to the running sum of turn points. Summing
            // TurnPointDistance loses (Length - TurnPointDistance) per segment, which walked the
            // turn window earlier and earlier; for a Straight (TurnPointDistance == float.MaxValue)
            // it saturated _trackDistance to Infinity and disabled every later turn.
            _segmentStartDistance += _previousSegmentLength;
            _previousSegmentLength = trackSegment.Length;
            _trackDistance = _segmentStartDistance + trackSegment.TurnPointDistance;
            _turnAvailableDistance = _trackDistance - _safeTurnDistance;
        }
    }
}
