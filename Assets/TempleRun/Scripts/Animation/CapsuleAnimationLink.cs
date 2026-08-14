using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

namespace CrawfisSoftware.TempleRun
{
    public class CapsuleAnimationLink : MonoBehaviour
    {
        private Animator animator;

        private void Start()
        {
            animator = GetComponent<Animator>();
            TempleRunBus.Subscribe(
                TempleRunEvents.LaneChangingLeft, TriggerLeanLeftAnimation);
            TempleRunBus.Subscribe(
                TempleRunEvents.LaneChangingRight, TriggerLeanRightAnimation);
        }

        private void OnDestroy()
        {
            TempleRunBus.Unsubscribe(
                TempleRunEvents.LaneChangingLeft, TriggerLeanLeftAnimation);
            TempleRunBus.Unsubscribe(TempleRunEvents.LaneChangingRight, TriggerLeanRightAnimation);
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