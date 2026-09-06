using GTMY.Audio;

using System.Threading;

using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

namespace CrawfisSoftware.TempleRun.Audio
{
    [RequireComponent(typeof(AudioSource))]
    internal class Metronome : MonoBehaviour
    {
        [SerializeField] private AudioClip _tickSound;
        [SerializeField] private float _speedTimeScale = 6f;
        [SerializeField] private AudioSource _audioSource;

        // One token source covers both the run ending and destroy.
        private CancellationTokenSource _cts;
        private void Awake()
        {
            var leftClipProvider = new AudioClipProvider(new System.Random());
            leftClipProvider.AddClip(_tickSound);
            var leftFactory = new AudioFactoryPooled(this, this.gameObject);
            //AudioFactoryRegistry.Instance.RegisterAudioFactory("TurnLeftPooledAudio", leftFactory);
            ISfxAudioPlayer sfxAudioPlayer = SfxAudioPlayerFactory.Instance.CreateSfxAudioPlayer("Metronome", leftFactory, leftClipProvider);

            // PlayerActivated, not TempleRunStarted: the beat belongs to the run, not the
            // ceremony - and pre-activation CurrentSpeed is 0, so a tick scheduled off
            // TempleRunStarted divides by it and waits forever after the first click.
            TempleRunBus.Subscribe(TempleRunEvents.PlayerActivated, StartMetronome);
            // The run can end without a death - quitting reaches TempleRunEnded without ever
            // publishing PlayerDied - so listen for the state, not one particular cause of it.
            TempleRunBus.Subscribe(TempleRunEvents.TempleRunEnded, StopMetronome);
        }

        private void OnDestroy()
        {
            TempleRunBus.Unsubscribe(TempleRunEvents.PlayerActivated, StartMetronome);
            TempleRunBus.Unsubscribe(TempleRunEvents.TempleRunEnded, StopMetronome);
            _cts?.Cancel();
        }

        private void StartMetronome(string eventName, object sender, object eventData)
        {
            // Start the metronome ticking
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            _ = MetronomeTick(_cts.Token);
        }

        private void StopMetronome(string eventName, object sender, object eventData)
        {
            _cts?.Cancel();
        }

        private async Awaitable MetronomeTick(CancellationToken token)
        {
            while (true)
            {
                // Play the tick sound
                AudioManagerSingleton.Instance.PlaySfx("Metronome", 1);
                float timeBetweenTicks = _speedTimeScale / Blackboard.Instance.CurrentSpeed; // Calculate time between ticks
                await Awaitable.WaitForSecondsAsync(timeBetweenTicks, token);
            }
        }
    }
}