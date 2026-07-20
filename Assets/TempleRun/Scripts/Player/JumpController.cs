using CrawfisSoftware.Events;

using UnityEngine;

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
            EventsPublisherUserInitiated.Instance.SubscribeToEvent(
                UserInitiatedEvents.UserJumpRequested, OnJumpInputReceived);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(
                TempleRunEvents.JumpLanded, OnJumpLanded);
        }

        private void OnDestroy()
        {
            EventsPublisherUserInitiated.Instance.UnsubscribeToEvent(
                UserInitiatedEvents.UserJumpRequested, OnJumpInputReceived);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(
                TempleRunEvents.JumpLanded, OnJumpLanded);
        }

        private void OnJumpInputReceived(string eventName, object sender, object data)
        {
            if (_isJumping) return;

            _isJumping = true;
            EventsPublisherTempleRun.Instance.PublishEvent(
                TempleRunEvents.JumpRequested, this, null);
        }

        private void OnJumpLanded(string eventName, object sender, object data)
        {
            _isJumping = false;
        }
    }
}