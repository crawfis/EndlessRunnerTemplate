using CrawfisSoftware.Events;

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
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.TempleRunStartRequested, OnTempleRunStartRequested);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.PlayerDied, OnPlayerDied);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.PlayerPaused, OnPause);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.PlayerResumed, OnResume);
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
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.TempleRunStartRequested, OnTempleRunStartRequested);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.PlayerDied, OnPlayerDied);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.PlayerPaused, OnPause);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.PlayerResumed, OnResume);
        }

        private void OnTempleRunStartRequested(string eventName, object sender, object data)
        {
            _musicPlayer.Play();
        }

        private void OnPlayerDied(string eventName, object sender, object data)
        {
            _musicPlayer.Stop();
        }
    }
}