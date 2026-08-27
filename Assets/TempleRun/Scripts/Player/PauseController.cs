using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Resolves a pause toggle against current state and applies it when the pause lifecycle
    /// completes. The toggle arrives as a TempleRun event so pause can be driven from any source:
    /// player input, AI, replay, network.
    ///    Subscribes: TempleRunEvents.PlayerPauseToggleRequested (from bridge translating
    ///                UserInitiated), TempleRunEvents.PlayerPaused, TempleRunEvents.PlayerResumed
    ///    Publishes: TempleRunEvents.PlayerPauseRequested, TempleRunEvents.PlayerResumeRequested
    /// </summary>
    public class PauseController : MonoBehaviour
    {
        private bool _isPaused = false;

        public bool IsPaused { get { return _isPaused; } }

        private void Awake()
        {
            TempleRunBus.Subscribe(TempleRunEvents.PlayerPauseToggleRequested, OnPauseToggle);

            TempleRunBus.Subscribe(TempleRunEvents.PlayerPaused, OnPause);
            TempleRunBus.Subscribe(TempleRunEvents.PlayerResumed, OnResume);
        }

        private void OnDestroy()
        {
            TempleRunBus.Unsubscribe(TempleRunEvents.PlayerPauseToggleRequested, OnPauseToggle);

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
            if (_isPaused)
                TempleRunBus.Publish(TempleRunEvents.PlayerResumeRequested, this, UnityEngine.Time.time);
            else
                TempleRunBus.Publish(TempleRunEvents.PlayerPauseRequested, this, UnityEngine.Time.time);
        }

        private void OnPauseToggle(string eventName, object sender, object data)
        {
            TogglePauseResume();
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
