using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    public class GameTime : MonoBehaviour
    {
        public static GameTime Instance { get; private set; }

        private float _timeScale = 1f;
        public float deltaTime
        {
            get
            {
                return _timeScale * UnityEngine.Time.deltaTime;
            }
        }

        private void Awake()
        {
            if(Instance != null && Instance != this)
            {
                Destroy(Instance);
            }
            Instance = this;

            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.PlayerPaused, OnPlayerPause);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.PlayerResumed, OnPlayerResume);
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.PlayerPaused, OnPlayerPause);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.PlayerResumed, OnPlayerResume);
        }

        private void OnPlayerPause(string eventName, object sender, object data)
        {
            _timeScale = 0f;
        }

        private void OnPlayerResume(string eventName, object sender, object data)
        {
            _timeScale = 1f;
        }
    }
}