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
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.CurrentSplineChanging, OnActiveSplineChanging);
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.CurrentSplineChanging, OnActiveSplineChanging);
        }

        private void OnActiveSplineChanging(object sender, object data)
        {
            StartCoroutine(TeleportWithDelay(data));
        }

        private IEnumerator TeleportWithDelay(object data)
        {
            EventsPublisherTempleRun.Instance.PublishEvent(KnownEvents.TeleportStarted, this, (_teleportDuration, data));
            yield return new WaitForSeconds(_teleportDuration);
            EventsPublisherTempleRun.Instance.PublishEvent(KnownEvents.TeleportEnded, this, data);
        }
    }
}