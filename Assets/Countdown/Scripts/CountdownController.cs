using CrawfisSoftware.Countdown.Events;

using System.Threading;

using UnityEngine;
using CountdownBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.Countdown.Events.CountdownEvents>;

namespace CrawfisSoftware.Countdown
{
    /// <summary>
    /// Runs the countdown timer and publishes tick/end events.
    /// The ceremony is its own domain: nothing here knows what the countdown is counting down to.
    ///    Subscribes: CountdownEvents.CountdownStarting
    ///    Publishes: CountdownEvents.CountdownStarted
    ///    Publishes: CountdownEvents.CountdownTick
    ///    Publishes: CountdownEvents.CountdownEnding (CountdownEnded follows by auto-chain)
    /// </summary>
    internal class CountdownController : MonoBehaviour
    {
        // Was TempleRunConstants.CountdownSeconds; the ceremony's length belongs to the
        // ceremony, and a cross-assembly internal is unreachable under RUGS's asmdefs anyway.
        [SerializeField] private float _countdownSeconds = 3f;

        // One token source covers both restart (a second CountdownStarting cancels the
        // countdown in flight) and destroy.
        private CancellationTokenSource _cts;

        private void Awake()
        {
            CountdownBus.Subscribe(
                CountdownEvents.CountdownStarting, OnCountdownStarting);
        }

        private void OnDestroy()
        {
            CountdownBus.Unsubscribe(
                CountdownEvents.CountdownStarting, OnCountdownStarting);

            _cts?.Cancel();
        }

        private void OnCountdownStarting(string eventName, object sender, object data)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            _ = CountdownRoutine(_countdownSeconds, _cts.Token);
        }

        private async Awaitable CountdownRoutine(float seconds, CancellationToken token)
        {
            // CountdownStarting says the countdown was asked for; CountdownStarted says the
            // clock is actually running. Anything that must not fire on a cancelled start
            // (music, the first tick's SFX) hangs off this rung, not the one above it.
            CountdownBus.Publish(
                CountdownEvents.CountdownStarted, this, seconds);

            float t = seconds;
            int lastReportedSecond = Mathf.FloorToInt(t);

            while (t > 0f)
            {
                await Awaitable.NextFrameAsync(token);
                t -= Time.deltaTime;
                int currentSecond = Mathf.FloorToInt(t);
                if (currentSecond != lastReportedSecond)
                {
                    lastReportedSecond = currentSecond;
                    CountdownBus.Publish(
                        CountdownEvents.CountdownTick, this, currentSecond);
                }
            }

            // Only CountdownEnding is published here - CountdownEnding -> CountdownEnded is
            // auto-chained. That link is where a "GO!" flash or a start-line delay goes, and
            // adding one must not require touching this controller.
            CountdownBus.Publish(
                CountdownEvents.CountdownEnding, this, null);
        }
    }
}
