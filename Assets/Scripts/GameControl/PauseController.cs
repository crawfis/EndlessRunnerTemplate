using CrawfisSoftware.TempleRun;

using UnityEngine;

namespace CrawfisSoftware.GameControl
{
    internal class PauseController : MonoBehaviour
    {
        private bool _isPaused = false;

        public bool IsPaused { get { return _isPaused; } }

        private void Awake()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.PauseToggle, OnPauseToggle);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.Pause, OnPause);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.Resume, OnResume);
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.PauseToggle, OnPauseToggle);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.Pause, OnPause);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.Resume, OnResume);
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
                EventsPublisherTempleRun.Instance.PublishEvent(KnownEvents.Resume, this, null);
            else
                EventsPublisherTempleRun.Instance.PublishEvent(KnownEvents.Pause, this, null);
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
