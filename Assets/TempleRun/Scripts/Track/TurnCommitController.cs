using CrawfisSoftware.Events;

using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Takes a turn from the moment it is permitted to the moment it is done, and commits the
    /// track to it. <see cref="TurnController"/> decides only whether a turn may happen; this
    /// class is what makes it happen on the track.
    ///
    /// Once <c>Turn*Starting</c> says the turn is legal it publishes <c>Turn*Started</c>, commits
    /// an Either junction to the chosen direction if the active segment is one, and then publishes
    /// <c>Turn*Ending</c>. <c>Turn*Ended</c> follows by auto-chain.
    ///
    /// <para><b>The order is load-bearing.</b> <c>SegmentRequested</c> must be delivered before
    /// <c>Turn*Ending</c>: PathProvider resolves the junction's exit geometry from it, and
    /// SegmentTransitionController consumes that geometry when it sees the ending rung. Publishing
    /// returns only once the event has been delivered, so the geometry is in place in time.</para>
    ///
    /// <para>This is also why <c>Turn*Starting → Turn*Started</c> is not auto-chained. A chain
    /// target and this subscriber both hang off <c>Turn*Starting</c>, and their relative order is
    /// not defined — the Started rung could land after the commit, or after Ending. Publishing it
    /// here makes the ladder's order deterministic.</para>
    ///    Dependencies: EventsFor&lt;TempleRunEvents&gt;
    ///    Subscribes: TempleRunEvents.TurnLeftStarting, TurnRightStarting
    ///    Subscribes: TempleRunEvents.ActiveTrackChanging — is the active segment an Either junction?
    ///    Publishes: TempleRunEvents.TurnLeftStarted, TurnRightStarted
    ///    Publishes: TempleRunEvents.SegmentRequested (data: Direction) — only at an Either junction
    ///    Publishes: TempleRunEvents.TurnLeftEnding, TurnRightEnding (Ended follows by auto-chain)
    /// </summary>
    internal class TurnCommitController : MonoBehaviour
    {
        private static readonly EventId<TrackSegmentInfo> TrackChanging =
            TempleRunBus.Id<TrackSegmentInfo>(TempleRunEvents.ActiveTrackChanging);
        private static readonly EventId<Direction> SegmentRequested =
            TempleRunBus.Id<Direction>(TempleRunEvents.SegmentRequested);

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
            TrackChanging.Subscribe(OnTrackChanging);
        }

        private void OnDestroy()
        {
            TempleRunBus.Unsubscribe(TempleRunEvents.TurnLeftStarting, OnTurnLeftStarting);
            TempleRunBus.Unsubscribe(TempleRunEvents.TurnRightStarting, OnTurnRightStarting);
            TrackChanging.Unsubscribe(OnTrackChanging);
        }

        private void OnTurnLeftStarting(string eventName, object sender, object data)
            => TakeTurn(Direction.Left, TempleRunEvents.TurnLeftStarted, TempleRunEvents.TurnLeftEnding, data);

        private void OnTurnRightStarting(string eventName, object sender, object data)
            => TakeTurn(Direction.Right, TempleRunEvents.TurnRightStarted, TempleRunEvents.TurnRightEnding, data);

        /// <summary>
        /// The distance carried by the Starting rung is forwarded unchanged to Started and Ending,
        /// so every subscriber along the ladder sees the same value for one turn.
        /// </summary>
        private void TakeTurn(Direction direction, TempleRunEvents startedEvent,
                              TempleRunEvents endingEvent, object distance)
        {
            TempleRunBus.Publish(startedEvent, this, distance);

            if (_awaitingCommit)
            {
                _awaitingCommit = false;
                SegmentRequested.Publish(this, direction);
            }

            TempleRunBus.Publish(endingEvent, this, distance);
        }

        private void OnTrackChanging(string eventName, object sender, TrackSegmentInfo trackSegment)
        {
            _awaitingCommit = trackSegment.Direction == Direction.Either;
        }
    }
}
