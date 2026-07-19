using CrawfisSoftware.Events;
using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Validates dash requests and manages dash state.
    /// Blocks dashes while already dashing or on cooldown.
    /// Does NOT publish events - validation pass-through allows auto-chain to fire DashStarting.
    ///    Dependencies: Blackboard.DashConfig for cooldown configuration
    ///    Subscribes: TempleRunEvents.DashRequested (from bridge translating UserInitiated)
    ///    Subscribes: TempleRunEvents.DashEnded (clear _isDashing, track cooldown)
    ///    Publishes: (none - auto-flow handles event progression)
    /// </summary>
    internal class DashController : MonoBehaviour
    {
        private bool _isDashing = false;
        private float _lastDashTime = -10f;

        private void Awake()
        {
            // Subscribe to TempleRun domain events, not UserInitiated
            // This allows dash to be triggered from any source: player input, AI, replay, network, etc.
            // The bridge translates UserInitiated.DashRequested -> TempleRunEvents.DashRequested
            EventsPublisherTempleRun.Instance.SubscribeToEvent(
                TempleRunEvents.DashRequested, OnDashRequested);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(
                TempleRunEvents.DashEnded, OnDashEnded);
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(
                TempleRunEvents.DashRequested, OnDashRequested);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(
                TempleRunEvents.DashEnded, OnDashEnded);
        }

        private void OnDashRequested(string eventName, object sender, object data)
        {
            // Prevent dashing while already dashing
            if (_isDashing) return;

            // Get DashConfig with safe fallback
            var dashConfig = Blackboard.Instance.DashConfig;
            if (dashConfig == null)
            {
                Debug.LogWarning("DashConfig not assigned to Blackboard. Dash input will be ignored.");
                return;
            }

            // Check cooldown
            float timeSinceLastDash = Time.time - _lastDashTime;
            if (timeSinceLastDash < dashConfig.DashCooldown)
                return;

            // Validation passed - mark as dashing and record time
            // Event auto-chains will handle DashRequested -> DashStarting progression
            _isDashing = true;
            _lastDashTime = Time.time;
        }

        private void OnDashEnded(string eventName, object sender, object data)
        {
            _isDashing = false;
        }
    }
}
