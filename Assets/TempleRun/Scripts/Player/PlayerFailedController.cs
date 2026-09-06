using CrawfisSoftware.TempleRun.GameConfig;

using System.Collections;

using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Owns the length of the post-failure hitch: the brief freeze after the player stumbles,
    /// before control returns. PlayerFailing (auto-chained from whichever specific failure
    /// occurred) begins the hitch; this class ends it by publishing PlayerFailed.
    ///
    /// The hitch has its own events rather than reusing the pause events. Sharing them meant a
    /// stumble set PauseController's state, so pressing pause during a stumble resumed instead
    /// of pausing - and it sent the hitch across the bridge into GameFlow as a session pause.
    ///    Dependencies: TempleRunConstants, EventsFor&lt;TempleRunEvents&gt;
    ///    Subscribes: TempleRunEvents.PlayerFailing
    ///    Publishes: TempleRunEvents.PlayerFailed (the hitch is over)
    /// </summary>
    internal class PlayerFailedController : MonoBehaviour
    {
        private Coroutine _hitchCoroutine;

        private void Awake()
        {
            TempleRunBus.Subscribe(TempleRunEvents.PlayerFailing, OnPlayerFailing);
        }

        private void OnPlayerFailing(string eventName, object sender, object data)
        {
            // Guard: ignore if a hitch is already running (e.g. turn failure + obstacle hit in
            // the same frame). The first one owns the timing.
            if (_hitchCoroutine != null) return;
            _hitchCoroutine = StartCoroutine(FailureHitch());
        }

        private IEnumerator FailureHitch()
        {
            // Real time, because GameTime is frozen for the duration of the hitch.
            yield return new WaitForSecondsRealtime(TempleRunConstants.ResumeDelay);
            _hitchCoroutine = null;
            // No payload: the clock it used to carry was read by nobody and readable by anyone.
            TempleRunBus.Publish(TempleRunEvents.PlayerFailed, this, null);
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            TempleRunBus.Unsubscribe(TempleRunEvents.PlayerFailing, OnPlayerFailing);
        }
    }
}
