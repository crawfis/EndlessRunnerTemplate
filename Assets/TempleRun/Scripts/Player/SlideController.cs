using CrawfisSoftware.Events;
using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Validates slide requests and manages slide state.
    /// Blocks slides while already sliding or on cooldown.
    /// Does NOT publish events - validation pass-through allows auto-chain to fire SlideStarting.
    ///    Dependencies: Blackboard.SlideConfig for cooldown configuration
    ///    Subscribes: TempleRunEvents.SlideRequested (from bridge translating UserInitiated)
    ///    Subscribes: TempleRunEvents.SlideEnded (clear _isSliding, track cooldown)
    ///    Publishes: (none - auto-flow handles event progression)
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
            EventsPublisherTempleRun.Instance.SubscribeToEvent(
                TempleRunEvents.SlideRequested, OnSlideRequested);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(
                TempleRunEvents.SlideEnded, OnSlideEnded);
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(
                TempleRunEvents.SlideRequested, OnSlideRequested);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(
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
            // Event auto-chains will handle SlideRequested -> SlideStarting progression
            _isSliding = true;
            _lastSlideTime = Time.time;

            EventsPublisherTempleRun.Instance.PublishEvent(TempleRunEvents.SlideStarted, this, null);
        }

        private void OnSlideEnded(string eventName, object sender, object data)
        {
            _isSliding = false;
        }
    }
}
