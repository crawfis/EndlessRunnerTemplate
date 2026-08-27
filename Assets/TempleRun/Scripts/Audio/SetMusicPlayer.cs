using CrawfisSoftware.Events;

using GTMY.Audio;

using System;

using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

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
            TempleRunBus.Subscribe(TempleRunEvents.TempleRunStartRequested, OnTempleRunStartRequested);
            // Stopping the run's music is a run-ended behavior, not a death-specific one:
            // quitting reaches TempleRunEnded without ever publishing PlayerDied.
            TempleRunBus.Subscribe(TempleRunEvents.TempleRunEnded, OnRunEnded);
            TempleRunBus.Subscribe(TempleRunEvents.PlayerPaused, OnPause);
            TempleRunBus.Subscribe(TempleRunEvents.PlayerResumed, OnResume);
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
            TempleRunBus.Unsubscribe(TempleRunEvents.TempleRunStartRequested, OnTempleRunStartRequested);
            TempleRunBus.Unsubscribe(TempleRunEvents.TempleRunEnded, OnRunEnded);
            TempleRunBus.Unsubscribe(TempleRunEvents.PlayerPaused, OnPause);
            TempleRunBus.Unsubscribe(TempleRunEvents.PlayerResumed, OnResume);
        }

        private void OnTempleRunStartRequested(string eventName, object sender, object data)
        {
            _musicPlayer.Play();
        }

        private void OnRunEnded(string eventName, object sender, object data)
        {
            _musicPlayer.Stop();
        }
    }
}