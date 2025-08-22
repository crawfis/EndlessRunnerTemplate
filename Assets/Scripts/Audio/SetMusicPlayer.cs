using GTMY.Audio;

using UnityEngine;

namespace CrawfisSoftware.TempleRun.Audio
{
    [RequireComponent(typeof(MusicPlayerExplicit))]
    internal class SetMusicPlayer : MonoBehaviour
    {
        [SerializeField]
        private MusicPlayerExplicit _musicPlayer;
        [SerializeField] private float _initialVolume = 0.5f;
        private void Awake()
        {
            AudioManagerSingleton.Instance.SetMusicPlayer(_musicPlayer);
            _musicPlayer.Volume = _initialVolume;
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.GameStarted, OnGameStarted);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.GameOver, OnGameOver);
        }
        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.GameStarted, OnGameStarted);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.GameOver, OnGameOver);
        }

        private void OnGameStarted(string eventName, object sender, object data)
        {
            _musicPlayer.Play();
        }

        private void OnGameOver(string eventName, object sender, object data)
        {
            _musicPlayer.Stop();
        }
    }
}