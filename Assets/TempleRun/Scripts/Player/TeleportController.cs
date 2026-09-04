using System.Collections;

using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Start and end the teleportation when the current spline is changing. Allows for a cinematic
    /// teleportation or a smoother teleportation and rotation.
    ///    Dependency: EventsFor<TempleRunEvents>
    ///    Subscribes: TempleRunEvents.CurrentSplineChanging
    ///    Publishes: TeleportStarting — TeleportStarted follows by auto-chain (DistanceController halts movement)
    ///    Publishes: TeleportEnding — TeleportEnded follows by auto-chain (DistanceController resumes)
    ///    Publishes: TurnLeftEnding / TurnRightEnding — the teleport IS the turn's motion, so the
    ///               turn ends when it does. Turn*Ended follows by auto-chain.
    /// </summary>
    public class TeleportController : MonoBehaviour
    {
        [SerializeField] private float _teleportDuration = 1.0f;
        private void Awake()
        {
            TempleRunBus.Subscribe(TempleRunEvents.CurrentSplineChanging, OnActiveSplineChanging);
        }

        private void OnDestroy()
        {
            TempleRunBus.Unsubscribe(TempleRunEvents.CurrentSplineChanging, OnActiveSplineChanging);
        }

        private void OnActiveSplineChanging(string EventName, object sender, object data)
        {
            // Do not teleport if the new spline is a straight segment.
            var (_, _, direction, _) = ((Vector3, Vector3, Direction, float))data;
            if (direction == Direction.Straight)
                return;
            StartCoroutine(TeleportWithDelay(data, direction));
        }

        private IEnumerator TeleportWithDelay(object data, Direction direction)
        {
            // This teleport has no warm-up and no wind-down of its own, so it publishes only the
            // *ing rungs and lets the chain carry each to its *ed. Both links stay open: a VFX
            // wind-up belongs in Starting -> Started, an arrival sting in Ending -> Ended, and
            // either can be added without this controller or its subscribers changing.
            TempleRunBus.Publish(TempleRunEvents.TeleportStarting, this, (_teleportDuration, data));
            yield return new WaitForSecondsRealtime(_teleportDuration);
            TempleRunBus.Publish(TempleRunEvents.TeleportEnding, this, data);

            // The turn ends here, not where it was committed. This teleport only runs for a
            // non-Straight spline change, which is a turn's exit, so the motion finishing IS the
            // turn finishing. Publishing it earlier declared the turn over while the player had
            // not moved yet - the teleport was hanging off the terminal rung instead of filling
            // the gap between Started and Ending, which is where an action's duration belongs.
            TempleRunBus.Publish(
                direction == Direction.Right
                    ? TempleRunEvents.TurnRightEnding
                    : TempleRunEvents.TurnLeftEnding,
                this, data);
            // No resume published here. A teleport never paused: the freeze during a teleport
            // is DistanceController._isMoving, toggled by TeleportStarted/TeleportEnded above.
            // Publishing a resume released a pause this class never took - and if the player
            // had paused mid-teleport, it un-paused them.
        }
    }
}