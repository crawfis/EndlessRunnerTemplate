using CrawfisSoftware.TempleRun.GameConfig;

using System.Collections;

using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Runs the countdown timer and publishes tick/end events.
    /// Extracted from UIPanelController so countdown logic lives in TempleRun domain.
    ///    Dependencies: TempleRunConstants
    ///    Subscribes: TempleRunEvents.CountdownStarting
    ///    Publishes: TempleRunEvents.CountdownTick
    ///    Publishes: TempleRunEvents.CountdownEnding
    ///    Publishes: TempleRunEvents.CountdownEnded
    /// </summary>
    internal class CountdownController : MonoBehaviour
    {
        private Coroutine _countdownCoroutine;

        private void Awake()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(
                TempleRunEvents.CountdownStarting, OnCountdownStarting);
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(
                TempleRunEvents.CountdownStarting, OnCountdownStarting);
        }

        private void OnCountdownStarting(string eventName, object sender, object data)
        {
            if (_countdownCoroutine != null)
                StopCoroutine(_countdownCoroutine);

            _countdownCoroutine = StartCoroutine(CountdownRoutine(TempleRunConstants.CountdownSeconds));
        }

        private IEnumerator CountdownRoutine(float seconds)
        {
            float t = seconds;
            int lastReportedSecond = Mathf.FloorToInt(t);

            while (t > 0f)
            {
                yield return null;
                t -= Time.deltaTime;
                int currentSecond = Mathf.FloorToInt(t);
                if (currentSecond != lastReportedSecond)
                {
                    lastReportedSecond = currentSecond;
                    EventsPublisherTempleRun.Instance.PublishEvent(
                        TempleRunEvents.CountdownTick, this, currentSecond);
                }
            }

            EventsPublisherTempleRun.Instance.PublishEvent(
                TempleRunEvents.CountdownEnding, this, null);

            _countdownCoroutine = null;

            EventsPublisherTempleRun.Instance.PublishEvent(
                TempleRunEvents.CountdownEnded, this, null);
        }
    }
}
