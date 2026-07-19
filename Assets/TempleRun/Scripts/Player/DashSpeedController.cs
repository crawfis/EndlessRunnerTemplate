using CrawfisSoftware.TempleRun.GameConfig;
using System.Collections;
using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Drives the dash animation by writing to Blackboard.CurrentDashMultiplier each frame.
    /// Follows the JumpArcController pattern (coroutine-based lerp with AnimationCurve).
    /// Animates CurrentDashMultiplier from 1.0 to DashConfig.DashSpeedMultiplier using the curve.
    ///    Dependencies: Blackboard, DashConfig
    ///    Subscribes: TempleRunEvents.DashStarting
    ///    Publishes: TempleRunEvents.DashStarted (at animation start)
    ///    Publishes: TempleRunEvents.DashEnded (when animation completes)
    /// </summary>
    internal class DashSpeedController : MonoBehaviour
    {
        private Coroutine _dashCoroutine;

        private void Awake()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(
                TempleRunEvents.DashStarting, OnDashStarting);
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(
                TempleRunEvents.DashStarting, OnDashStarting);

            if (_dashCoroutine != null)
                StopCoroutine(_dashCoroutine);

            // Reset multiplier on destroy so it doesn't persist across scene loads
            if (Blackboard.Instance != null)
                Blackboard.Instance.CurrentDashMultiplier = 1.0f;
        }

        private void OnDashStarting(string eventName, object sender, object data)
        {
            // If somehow a dash is already running, stop it first
            if (_dashCoroutine != null)
                StopCoroutine(_dashCoroutine);

            _dashCoroutine = StartCoroutine(RunDashArc());
        }

        private IEnumerator RunDashArc()
        {
            DashConfig config = Blackboard.Instance.DashConfig;
            if (config == null)
            {
                Debug.LogError("DashSpeedController: DashConfig is null! Animation cannot proceed.");
                _dashCoroutine = null;
                yield break;
            }

            float speedMultiplier = config.DashSpeedMultiplier;
            float duration = config.DashDuration;
            AnimationCurve curve = config.DashCurve ?? AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

            float elapsed = 0f;
            bool startPublished = false;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curveValue = curve.Evaluate(t);

                // Animate speed multiplier: from 1.0 to speedMultiplier and back to 1.0
                Blackboard.Instance.CurrentDashMultiplier = 1.0f + (curveValue * (speedMultiplier - 1.0f));

                // Publish DashStarted at the very beginning (defer to next frame to avoid event circular dependency)
                if (!startPublished)
                {
                    startPublished = true;
                    // Defer event publishing to next frame
                    yield return null;
                    EventsPublisherTempleRun.Instance.PublishEvent(
                        TempleRunEvents.DashStarted, this, null);
                    continue;
                }

                yield return null;
            }

            // Snap to normal state
            Blackboard.Instance.CurrentDashMultiplier = 1.0f;
            _dashCoroutine = null;

            EventsPublisherTempleRun.Instance.PublishEvent(
                TempleRunEvents.DashEnded, this, null);
        }
    }
}
