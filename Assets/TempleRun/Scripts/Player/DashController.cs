using CrawfisSoftware.Events;
using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Validates dash requests and manages dash state.
    /// Blocks dashes while already dashing or on cooldown.
    ///    Dependencies: Blackboard.DashConfig for cooldown configuration
    ///    Subscribes: TempleRunEvents.DashRequested (from bridge translating UserInitiated)
    ///    Subscribes: TempleRunEvents.DashEnded (clear _isDashing, track cooldown)
    ///    Publishes: TempleRunEvents.DashStarting (only once validation passes)
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
            TempleRunBus.Subscribe(
                TempleRunEvents.DashRequested, OnDashRequested);
            TempleRunBus.Subscribe(
                TempleRunEvents.DashEnded, OnDashEnded);
        }

        private void OnDestroy()
        {
            TempleRunBus.Unsubscribe(
                TempleRunEvents.DashRequested, OnDashRequested);
            TempleRunBus.Unsubscribe(
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
            _isDashing = true;
            _lastDashTime = Time.time;

            // Published here rather than auto-chained from DashRequested: DashRequested is the
            // bridge's raw translation of the input, so the old auto-chain fired DashStarting even
            // when the checks above rejected the request, defeating the cooldown entirely.
            // DashSpeedController takes it from here.
            TempleRunBus.Publish(
                TempleRunEvents.DashStarting, this, null);
        }

        private void OnDashEnded(string eventName, object sender, object data)
        {
            _isDashing = false;
        }
    }
}
