using CrawfisSoftware.Events;

using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Applies pause/resume to the player (Time.timeScale) when the pause lifecycle completes.
    ///    Subscribes: TempleRunEvents.PlayerPaused, TempleRunEvents.PlayerResumed
    ///    Publishes: TempleRunEvents.PlayerPauseRequested, TempleRunEvents.PlayerResumeRequested
    /// </summary>
    public class PlayerPauseController : MonoBehaviour
    {
        private bool _isPaused = false;

        public bool IsPaused { get { return _isPaused; } }

        private void Awake()
        {
            TempleRunBus.Subscribe(TempleRunEvents.PlayerPaused, OnPause);
            TempleRunBus.Subscribe(TempleRunEvents.PlayerResumed, OnResume);
        }

        private void OnDestroy()
        {
            TempleRunBus.Unsubscribe(TempleRunEvents.PlayerPaused, OnPause);
            TempleRunBus.Unsubscribe(TempleRunEvents.PlayerResumed, OnResume);
        }
        public void Pause()
        {
            _isPaused = true;
            Time.timeScale = 0f;
        }

        public void Resume()
        {
            _isPaused = false;
            Time.timeScale = 1f;

        }

        public void TogglePauseResume()
        {
            // No payload. This used to carry UnityEngine.Time.time, which no subscriber read and
            // any of them could read for itself.
            if (_isPaused)
                TempleRunBus.Publish(TempleRunEvents.PlayerResumeRequested, this, null);
            else
                TempleRunBus.Publish(TempleRunEvents.PlayerPauseRequested, this, null);
        }

        private void OnPause(string eventName, object sender, object data)
        {
            Pause();
        }

        private void OnResume(string eventName, object sender, object data)
        {
            Resume();
        }
    }
}