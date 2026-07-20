using System.Collections;

using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Start and end the teleportation when the current spline is changing. Allows for a cinematic
    /// teleportation or a smoother teleportation and rotation.
    ///    Dependency: EventsPublisherTempleRun
    ///    Subscribes: CurrentSplineChanging - Publishes a GameOver event
    ///    Publishes: TeleportStarted
    ///    Publishes: TeleportEnded
    /// </summary>
    public class TeleportController : MonoBehaviour
    {
        [SerializeField] private float _teleportDuration = 1.0f;
        private void Awake()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.CurrentSplineChanging, OnActiveSplineChanging);
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.CurrentSplineChanging, OnActiveSplineChanging);
        }

        private void OnActiveSplineChanging(string EventName, object sender, object data)
        {
            // Do not teleport if the new spline is a straight segment.
            var (_, _, direction, _) = ((Vector3, Vector3, Direction, float))data;
            if (direction == Direction.Straight)
                return;
            StartCoroutine(TeleportWithDelay(data));
        }

        private IEnumerator TeleportWithDelay(object data)
        {
            EventsPublisherTempleRun.Instance.PublishEvent(TempleRunEvents.TeleportStarted, this, (_teleportDuration, data));
            yield return new WaitForSecondsRealtime(_teleportDuration);
            EventsPublisherTempleRun.Instance.PublishEvent(TempleRunEvents.TeleportEnded, this, data);
            EventsPublisherTempleRun.Instance.PublishEvent(TempleRunEvents.PlayerResumed, this, data);
        }
    }
}