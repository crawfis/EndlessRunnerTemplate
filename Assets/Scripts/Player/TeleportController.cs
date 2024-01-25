using System.Collections;
using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    public class TeleportController : MonoBehaviour
    {
        [SerializeField] private float _teleportDuration = 1.0f;
        private void Awake()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.CurrentSplineChanged, OnActiveSplineChanged);
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.CurrentSplineChanged, OnActiveSplineChanged);
        }

        private void OnActiveSplineChanged(object sender, object data)
        {
            StartCoroutine(TeleportWithDelay(data));
        }

        private IEnumerator TeleportWithDelay(object data)
        {
            EventsPublisherTempleRun.Instance.PublishEvent(KnownEvents.TeleportStarted, this, data);
            yield return new WaitForSeconds(_teleportDuration);
            EventsPublisherTempleRun.Instance.PublishEvent(KnownEvents.TeleportEnded, this, data);
        }
    }
}