using CrawfisSoftware.TempleRun.GameConfig;
using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Drives the slide animation by writing to Blackboard.SlideHeightOffset and Blackboard.CurrentSlideMultiplier each frame.
    /// Follows the JumpArcController pattern (a per-frame Awaitable lerp with AnimationCurve).
    /// Simultaneously animates:
    ///   - SlideHeightOffset: from 0 to -SlideConfig.SlideHeightOffset (crouching motion)
    ///   - CurrentSlideMultiplier: from 1.0 to SlideConfig.SlideSpeedMultiplier (speed boost)
    ///    Dependencies: Blackboard, SlideConfig
    ///    Subscribes: TempleRunEvents.SlideStarting
    ///    Publishes: TempleRunEvents.SlideStarted (at animation start)
    ///    Publishes: TempleRunEvents.SlideEnding (animation complete; SlideEnded follows by auto-chain)
    /// </summary>
    internal class SlideArcController : MonoBehaviour
    {
        private void Awake()
        {
            TempleRunBus.Subscribe(
                TempleRunEvents.SlideStarting, OnSlideStarting);
        }

        private void OnDestroy()
        {
            TempleRunBus.Unsubscribe(
                TempleRunEvents.SlideStarting, OnSlideStarting);

            // Reset offsets on destroy so they don't persist across scene loads
            if (Blackboard.Instance != null)
            {
                Blackboard.Instance.SlideHeightOffset = 0f;
                Blackboard.Instance.CurrentSlideMultiplier = 1.0f;
            }
        }

        private void OnSlideStarting(string eventName, object sender, object data)
        {
            // Fire and forget. Reentrancy is SlideController's job: its gate is what makes
            // SlideStarting non-reentrant, so nothing here checks for a slide already running.
            _ = RunSlideArc();
        }

        private async Awaitable RunSlideArc()
        {
            SlideConfig config = Blackboard.Instance.SlideConfig;
            if (config == null)
            {
                Debug.LogError("SlideArcController: SlideConfig is null! Animation cannot proceed.");
                TempleRunBus.Publish(TempleRunEvents.SlideEnding, this, null);
                return;
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

                await Awaitable.NextFrameAsync(destroyCancellationToken);
            }

            // Snap to normal state
            Blackboard.Instance.SlideHeightOffset = 0f;
            Blackboard.Instance.CurrentSlideMultiplier = 1.0f;

            // Only SlideEnding is published here - SlideEnding -> SlideEnded is auto-chained.
            // That link is left open on purpose: a stand-up animation or a brief recovery window
            // belongs there, and inserting it must not require touching this controller.
            TempleRunBus.Publish(
                TempleRunEvents.SlideEnding, this, null);
        }
    }
}
