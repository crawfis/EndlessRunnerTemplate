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
    ///    Subscribes: TempleRunEvents.TempleRunEnding - a run that ends while paused ends the
    ///                pause too (see OnRunEnding)
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
            TempleRunBus.Subscribe(TempleRunEvents.TempleRunEnding, OnRunEnding);
        }

        private void OnDestroy()
        {
            TempleRunBus.Unsubscribe(TempleRunEvents.PlayerPauseToggleRequested, OnPauseToggle);

            TempleRunBus.Unsubscribe(TempleRunEvents.PlayerPaused, OnPause);
            TempleRunBus.Unsubscribe(TempleRunEvents.PlayerResumed, OnResume);
            TempleRunBus.Unsubscribe(TempleRunEvents.TempleRunEnding, OnRunEnding);
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

        private void OnPauseToggle(string eventName, object sender, object data)
        {
            TogglePauseResume();
        }

        private void OnRunEnding(string eventName, object sender, object data)
        {
            // Quitting from the pause menu ends the run without anyone pressing resume, and the
            // pause used to outlive it: Time.timeScale stayed at 0, so the next run's countdown
            // (a scaled wait) sat at 3 until pause was toggled twice. This controller took the
            // pause, so it releases it - through the same ladder, so every mirror of the state
            // (GameTime's freeze, the music, GameFlow's Paused flag) is released with it. On
            // Ending rather than Ended so the resume completes before the run is declared over.
            if (_isPaused)
                TempleRunBus.Publish(TempleRunEvents.PlayerResumeRequested, this, null);
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
