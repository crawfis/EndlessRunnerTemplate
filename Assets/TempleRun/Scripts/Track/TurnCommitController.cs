using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Takes a turn from the moment it is permitted to the moment it is done, and commits the
    /// track to it. <see cref="TurnController"/> decides only whether a turn may happen; this
    /// class is what makes it happen on the track.
    ///
    /// Once <c>Turn*Starting</c> says the turn is legal it commits an Either junction to the
    /// chosen direction, if the active segment is one, and publishes <c>Turn*Started</c>. It does
    /// not end the turn: the exit spline is published from <c>Turn*Started</c> and the teleport
    /// onto it is the turn's duration, so <c>TeleportController</c> publishes <c>Turn*Ending</c>
    /// when that motion lands.
    ///
    /// <para><b>The order is load-bearing.</b> <c>SegmentRequested</c> must be delivered before
    /// <c>Turn*Started</c>: PathProvider resolves the junction's exit geometry from it, and
    /// SegmentTransitionController publishes that geometry as the exit spline when it sees
    /// Started. Publishing returns only once the event has been delivered, so the geometry is in
    /// place in time.</para>
    ///
    /// <para>This is also why <c>Turn*Starting → Turn*Started</c> is not auto-chained. A chain
    /// target and this subscriber both hang off <c>Turn*Starting</c>, and their relative order is
    /// not defined — Started could land before the junction had been committed. Publishing it here
    /// makes the order deterministic.</para>
    ///    Dependencies: EventsFor&lt;TempleRunEvents&gt;
    ///    Subscribes: TempleRunEvents.TurnLeftStarting, TurnRightStarting
    ///    Subscribes: TempleRunEvents.ActiveTrackChanging — is the active segment an Either junction?
    ///    Publishes: TempleRunEvents.TurnLeftStarted, TurnRightStarted
    ///    Publishes: TempleRunEvents.SegmentRequested (data: Direction) — only at an Either junction
    /// </summary>
    internal class TurnCommitController : MonoBehaviour
    {
        // Only an Either junction has an uncommitted exit. A Left or Right segment's single exit
        // was built when the segment was created, so there is nothing to commit — and committing
        // one anyway is destructive: TrackManager would clear _awaitingEitherDirection and generate
        // straight past a junction still waiting for its direction, while PathProvider would
        // resolve that junction's exit using the direction of an unrelated turn elsewhere.
        private bool _awaitingCommit;

        private void Awake()
        {
            TempleRunBus.Subscribe(TempleRunEvents.TurnLeftStarting, OnTurnLeftStarting);
            TempleRunBus.Subscribe(TempleRunEvents.TurnRightStarting, OnTurnRightStarting);
            TempleRunBus.Subscribe(TempleRunEvents.ActiveTrackChanging, OnTrackChanging);
        }

        private void OnDestroy()
        {
            TempleRunBus.Unsubscribe(TempleRunEvents.TurnLeftStarting, OnTurnLeftStarting);
            TempleRunBus.Unsubscribe(TempleRunEvents.TurnRightStarting, OnTurnRightStarting);
            TempleRunBus.Unsubscribe(TempleRunEvents.ActiveTrackChanging, OnTrackChanging);
        }

        private void OnTurnLeftStarting(string eventName, object sender, object data)
            => TakeTurn(Direction.Left, TempleRunEvents.TurnLeftStarted, data);

        private void OnTurnRightStarting(string eventName, object sender, object data)
            => TakeTurn(Direction.Right, TempleRunEvents.TurnRightStarted, data);

        /// <summary>
        /// The distance carried by the Starting rung is forwarded unchanged to Started, so both
        /// rungs name the same run-absolute point for one turn. The terminal rungs carry nothing:
        /// TeleportController publishes Ending with a null payload.
        /// </summary>
        private void TakeTurn(Direction direction, TempleRunEvents startedEvent, object distance)
        {
            // Commit before announcing. PathProvider builds the junction's exit geometry from
            // SegmentRequested, and SegmentTransitionController publishes that exit spline when it
            // sees Started - so the geometry has to exist by then. Publish returns only once the
            // queue has drained, so it does.
            if (_awaitingCommit)
            {
                _awaitingCommit = false;
                TempleRunBus.Publish(TempleRunEvents.SegmentRequested, this, direction);
            }

            // Started is where the turn becomes visible: the exit spline is published from it, and
            // the teleport onto that spline is the turn's duration. TeleportController publishes
            // Turn*Ending when that motion finishes - this class does not end the turn.
            TempleRunBus.Publish(startedEvent, this, distance);
        }

        private void OnTrackChanging(string eventName, object sender, object data)
        {
            var trackSegment = (TrackSegmentInfo)data;
            _awaitingCommit = trackSegment.Direction == Direction.Either;
        }
    }
}
