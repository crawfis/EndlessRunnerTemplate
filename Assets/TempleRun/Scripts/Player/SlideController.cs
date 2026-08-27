using CrawfisSoftware.Events;
using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Validates slide requests and manages slide state.
    /// Blocks slides while already sliding or on cooldown.
    ///    Dependencies: Blackboard.SlideConfig for cooldown configuration
    ///    Subscribes: TempleRunEvents.SlideRequested (from bridge translating UserInitiated)
    ///    Subscribes: TempleRunEvents.SlideEnded (clear _isSliding, track cooldown)
    ///    Publishes: TempleRunEvents.SlideStarting (only once validation passes)
    /// </summary>
    internal class SlideController : MonoBehaviour
    {
        private bool _isSliding = false;
        private float _lastSlideTime = -10f;

        private void Awake()
        {
            // Subscribe to TempleRun domain events, not UserInitiated
            // This allows slide to be triggered from any source: player input, AI, replay, network, etc.
            // The bridge translates UserInitiated.SlideRequested -> TempleRunEvents.SlideRequested
            TempleRunBus.Subscribe(
                TempleRunEvents.SlideRequested, OnSlideRequested);
            TempleRunBus.Subscribe(
                TempleRunEvents.SlideEnded, OnSlideEnded);
        }

        private void OnDestroy()
        {
            TempleRunBus.Unsubscribe(
                TempleRunEvents.SlideRequested, OnSlideRequested);
            TempleRunBus.Unsubscribe(
                TempleRunEvents.SlideEnded, OnSlideEnded);
        }

        private void OnSlideRequested(string eventName, object sender, object data)
        {
            // Prevent sliding while already sliding
            if (_isSliding) return;

            // Get SlideConfig with safe fallback
            var slideConfig = Blackboard.Instance.SlideConfig;
            if (slideConfig == null)
            {
                Debug.LogWarning("SlideConfig not assigned to Blackboard. Slide input will be ignored.");
                return;
            }

            // Check cooldown
            float timeSinceLastSlide = Time.time - _lastSlideTime;
            if (timeSinceLastSlide < slideConfig.SlideCooldown)
                return;

            // Validation passed - mark as sliding and record time
            _isSliding = true;
            _lastSlideTime = Time.time;

            // Published here rather than auto-chained from SlideRequested: SlideRequested is the
            // bridge's raw translation of the input, so an auto-chain would fire even when the
            // checks above reject the request. SlideArcController takes it from here.
            TempleRunBus.Publish(TempleRunEvents.SlideStarting, this, null);
        }

        private void OnSlideEnded(string eventName, object sender, object data)
        {
            _isSliding = false;
        }
    }
}
