using CrawfisSoftware.Events;

using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;
using UserInputBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.Events.UserInitiatedEvents>;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Validates jump requests and publishes TempleRun jump events.
    /// Blocks jumps while already airborne.
    ///    Dependencies: Blackboard
    ///    Subscribes: UserInitiatedEvents.JumpRequested
    ///    Subscribes: TempleRunEvents.JumpLanded (clear _isJumping)
    ///    Subscribes: TempleRunEvents.TempleRunStarted (reset state)
    ///    Publishes: TempleRunEvents.JumpRequested
    /// </summary>
    internal class JumpController : MonoBehaviour
    {
        private bool _isJumping = false;

        private void Awake()
        {
            UserInputBus.Subscribe(
                UserInitiatedEvents.UserJumpRequested, OnJumpInputReceived);
            TempleRunBus.Subscribe(
                TempleRunEvents.JumpLanded, OnJumpLanded);
        }

        private void OnDestroy()
        {
            UserInputBus.Unsubscribe(
                UserInitiatedEvents.UserJumpRequested, OnJumpInputReceived);
            TempleRunBus.Unsubscribe(
                TempleRunEvents.JumpLanded, OnJumpLanded);
        }

        private void OnJumpInputReceived(string eventName, object sender, object data)
        {
            if (_isJumping) return;

            _isJumping = true;
            TempleRunBus.Publish(
                TempleRunEvents.JumpRequested, this, null);
        }

        private void OnJumpLanded(string eventName, object sender, object data)
        {
            _isJumping = false;
        }
    }
}