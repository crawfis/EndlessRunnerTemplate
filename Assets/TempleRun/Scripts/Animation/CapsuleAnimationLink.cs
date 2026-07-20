using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    public class CapsuleAnimationLink : MonoBehaviour
    {
        private Animator animator;

        private void Start()
        {
            animator = GetComponent<Animator>();
            EventsPublisherTempleRun.Instance.SubscribeToEvent(
                TempleRunEvents.LaneChangingLeft, TriggerLeanLeftAnimation);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(
                TempleRunEvents.LaneChangingRight, TriggerLeanRightAnimation);
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(
                TempleRunEvents.LaneChangingLeft, TriggerLeanLeftAnimation);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.LaneChangingRight, TriggerLeanRightAnimation);
        }

        private void TriggerLeanLeftAnimation(string eventName, object sender, object data)
        {
            if (animator != null)
            {
                animator.SetTrigger("LeanLeft");
            }
        }

        private void TriggerLeanRightAnimation(string eventName, object sender, object data)
        {
            if (animator != null)
            {
                animator.SetTrigger("LeanRight");
            }
        }
    }
}