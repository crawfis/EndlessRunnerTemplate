using CrawfisSoftware.Events;

using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Toggles pause state from raw input and applies it when the pause lifecycle completes.
    ///    Subscribes: UserInitiatedEvents.UserPauseToggle, TempleRunEvents.PlayerPaused,
    ///                TempleRunEvents.PlayerResumed
    ///    Publishes: TempleRunEvents.PlayerPauseRequested, TempleRunEvents.PlayerResumeRequested
    /// </summary>
    public class PauseController : MonoBehaviour
    {
        private bool _isPaused = false;

        public bool IsPaused { get { return _isPaused; } }

        private void Awake()
        {
            EventsPublisherUserInitiated.Instance.SubscribeToEvent(UserInitiatedEvents.UserPauseToggle, OnPauseToggle);

            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.PlayerPaused, OnPause);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.PlayerResumed, OnResume);
        }

        private void OnDestroy()
        {
            EventsPublisherUserInitiated.Instance.UnsubscribeToEvent(UserInitiatedEvents.UserPauseToggle, OnPauseToggle);

            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.PlayerPaused, OnPause);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.PlayerResumed, OnResume);
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
                EventsPublisherTempleRun.Instance.PublishEvent(TempleRunEvents.PlayerResumeRequested, this, UnityEngine.Time.time);
            else
                EventsPublisherTempleRun.Instance.PublishEvent(TempleRunEvents.PlayerPauseRequested, this, UnityEngine.Time.time);
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
