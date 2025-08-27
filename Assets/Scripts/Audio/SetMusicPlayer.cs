using GTMY.Audio;

using System;

using UnityEngine;

namespace CrawfisSoftware.TempleRun.Audio
{
    [RequireComponent(typeof(MusicPlayerExplicit))]
    internal class SetMusicPlayer : MonoBehaviour
    {
        [SerializeField] private MusicPlayerExplicit _musicPlayer;
        [SerializeField] private float _initialVolume = 0.5f;
        private void Awake()
        {
            AudioManagerSingleton.Instance.SetMusicPlayer(_musicPlayer);
            _musicPlayer.Volume = _initialVolume;
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.GameStarted, OnGameStarted);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.GameOver, OnGameOver);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.Pause, OnPause);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.Resume, OnResume);
        }

        private void OnPause(string eventName, object sender, object data)
        {
            AudioManagerSingleton.Instance.Music.Pause();
            _musicPlayer.Pause();
        }

        private void OnResume(string eventName, object sender, object data)
        {
            AudioManagerSingleton.Instance.Music.UnPause();
            _musicPlayer.UnPause();
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.GameStarted, OnGameStarted);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.GameOver, OnGameOver);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.Pause, OnPause);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.Resume, OnResume);
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