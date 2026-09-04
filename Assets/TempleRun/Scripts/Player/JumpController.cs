using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Validates jump requests and publishes TempleRun jump events.
    /// Blocks jumps while already airborne.
    ///    Dependencies: Blackboard
    ///    Subscribes: TempleRunEvents.JumpRequested (from bridge translating UserInitiated)
    ///    Subscribes: TempleRunEvents.JumpEnded (clear _isJumping)
    ///    Publishes: TempleRunEvents.JumpStarting (only once validation passes)
    /// </summary>
    internal class JumpController : MonoBehaviour
    {
        private bool _isJumping = false;

        private void Awake()
        {
            // Subscribe to TempleRun domain events, not UserInitiated.
            // This allows jump to be triggered from any source: player input, AI, replay, network.
            // The bridge translates UserInitiated.UserJumpRequested -> TempleRunEvents.JumpRequested
            TempleRunBus.Subscribe(
                TempleRunEvents.JumpRequested, OnJumpRequested);
            TempleRunBus.Subscribe(
                TempleRunEvents.JumpEnded, OnJumpEnded);
        }

        private void OnDestroy()
        {
            TempleRunBus.Unsubscribe(
                TempleRunEvents.JumpRequested, OnJumpRequested);
            TempleRunBus.Unsubscribe(
                TempleRunEvents.JumpEnded, OnJumpEnded);
        }

        private void OnJumpRequested(string eventName, object sender, object data)
        {
            if (_isJumping) return;

            _isJumping = true;

            // Published here rather than auto-chained from JumpRequested: JumpRequested is the
            // bridge's raw translation of the input, so an auto-chain would launch a second jump
            // while one is already in the air. JumpArcController takes it from here.
            TempleRunBus.Publish(
                TempleRunEvents.JumpStarting, this, null);
        }

        private void OnJumpEnded(string eventName, object sender, object data)
        {
            _isJumping = false;
        }
    }
}