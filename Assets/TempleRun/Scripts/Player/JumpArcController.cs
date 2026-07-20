using CrawfisSoftware.TempleRun.GameConfig;

using System.Collections;

using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Drives the jump arc by writing to Blackboard.JumpHeightOffset each frame.
    /// Follows the LaneOffsetController pattern (coroutine-based lerp with AnimationCurve).
    ///    Dependencies: Blackboard, JumpConfig
    ///    Subscribes: TempleRunEvents.JumpStarting
    ///    Publishes: TempleRunEvents.JumpStarted (at arc apex)
    ///    Publishes: TempleRunEvents.JumpLanded (when arc completes)
    /// </summary>
    internal class JumpArcController : MonoBehaviour
    {
        private Coroutine _jumpCoroutine;

        private void Awake()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(
                TempleRunEvents.JumpStarting, OnJumpStarting);
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(
                TempleRunEvents.JumpStarting, OnJumpStarting);

            if (_jumpCoroutine != null)
                StopCoroutine(_jumpCoroutine);

            // Reset offset on destroy so it doesn't persist across scene loads
            if (Blackboard.Instance != null)
                Blackboard.Instance.JumpHeightOffset = 0f;
        }

        private void OnJumpStarting(string eventName, object sender, object data)
        {
            // If somehow a jump is already running, stop it first
            if (_jumpCoroutine != null)
                StopCoroutine(_jumpCoroutine);

            _jumpCoroutine = StartCoroutine(RunJumpArc());
        }

        private IEnumerator RunJumpArc()
        {
            JumpConfig config = Blackboard.Instance.JumpConfig;
            float height = config != null ? config.JumpHeight : 3f;
            float duration = config != null ? config.JumpDuration : 0.6f;
            AnimationCurve curve = config != null ? config.JumpCurve : AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

            float elapsed = 0f;
            bool apexPublished = false;
            float halfDuration = duration * 0.5f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curveValue = curve.Evaluate(t);
                Blackboard.Instance.JumpHeightOffset = curveValue * height;

                // Publish JumpStarted at the apex (halfway point)
                if (!apexPublished && elapsed >= halfDuration)
                {
                    apexPublished = true;
                    EventsPublisherTempleRun.Instance.PublishEvent(
                        TempleRunEvents.JumpStarted, this, null);
                }

                yield return null;
            }

            // Snap to ground
            Blackboard.Instance.JumpHeightOffset = 0f;
            _jumpCoroutine = null;

            EventsPublisherTempleRun.Instance.PublishEvent(
                TempleRunEvents.JumpLanded, this, null);
        }
    }
}
