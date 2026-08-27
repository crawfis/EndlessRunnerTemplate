using CrawfisSoftware.TempleRun.GameConfig;
using System.Collections;
using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Drives the slide animation by writing to Blackboard.SlideHeightOffset and Blackboard.CurrentSlideMultiplier each frame.
    /// Follows the JumpArcController pattern (coroutine-based lerp with AnimationCurve).
    /// Simultaneously animates:
    ///   - SlideHeightOffset: from 0 to -SlideConfig.SlideHeightOffset (crouching motion)
    ///   - CurrentSlideMultiplier: from 1.0 to SlideConfig.SlideSpeedMultiplier (speed boost)
    ///    Dependencies: Blackboard, SlideConfig
    ///    Subscribes: TempleRunEvents.SlideStarting
    ///    Publishes: TempleRunEvents.SlideStarted (at animation start)
    ///    Publishes: TempleRunEvents.SlideEnded (when animation completes)
    /// </summary>
    internal class SlideArcController : MonoBehaviour
    {
        private Coroutine _slideCoroutine;

        private void Awake()
        {
            TempleRunBus.Subscribe(
                TempleRunEvents.SlideStarting, OnSlideStarting);
        }

        private void OnDestroy()
        {
            TempleRunBus.Unsubscribe(
                TempleRunEvents.SlideStarting, OnSlideStarting);

            if (_slideCoroutine != null)
                StopCoroutine(_slideCoroutine);

            // Reset offsets on destroy so they don't persist across scene loads
            if (Blackboard.Instance != null)
            {
                Blackboard.Instance.SlideHeightOffset = 0f;
                Blackboard.Instance.CurrentSlideMultiplier = 1.0f;
            }
        }

        private void OnSlideStarting(string eventName, object sender, object data)
        {
            // If somehow a slide is already running, stop it first
            if (_slideCoroutine != null)
                StopCoroutine(_slideCoroutine);

            _slideCoroutine = StartCoroutine(RunSlideArc());
        }

        private IEnumerator RunSlideArc()
        {
            SlideConfig config = Blackboard.Instance.SlideConfig;
            if (config == null)
            {
                Debug.LogError("SlideArcController: SlideConfig is null! Animation cannot proceed.");
                _slideCoroutine = null;
                TempleRunBus.Publish(TempleRunEvents.SlideEnded, this, null);
                yield break;
            }

            float heightOffset = config.SlideHeightOffset;
            float speedMultiplier = config.SlideSpeedMultiplier;
            float duration = config.SlideDuration;
            AnimationCurve curve = config.SlideCurve ?? AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

            float elapsed = 0f;
            bool startPublished = false;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curveValue = curve.Evaluate(t);

                // Animate both height and speed multiplier using the same curve
                Blackboard.Instance.SlideHeightOffset = curveValue * heightOffset;
                Blackboard.Instance.CurrentSlideMultiplier = 1.0f + (curveValue * (speedMultiplier - 1.0f));

                // Publish SlideStarted once the animation is actually running
                if (!startPublished)
                {
                    startPublished = true;
                    TempleRunBus.Publish(
                        TempleRunEvents.SlideStarted, this, null);
                }

                yield return null;
            }

            // Snap to normal state
            Blackboard.Instance.SlideHeightOffset = 0f;
            Blackboard.Instance.CurrentSlideMultiplier = 1.0f;
            _slideCoroutine = null;

            TempleRunBus.Publish(
                TempleRunEvents.SlideEnded, this, null);
        }
    }
}
