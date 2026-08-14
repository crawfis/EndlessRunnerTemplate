using GTMY.Audio;

using System.Collections;

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

        private Coroutine _metronomeCoroutine;
        private void Awake()
        {
            var leftClipProvider = new AudioClipProvider(new System.Random());
            leftClipProvider.AddClip(_tickSound);
            var leftFactory = new AudioFactoryPooled(this, this.gameObject);
            //AudioFactoryRegistry.Instance.RegisterAudioFactory("TurnLeftPooledAudio", leftFactory);
            ISfxAudioPlayer sfxAudioPlayer = SfxAudioPlayerFactory.Instance.CreateSfxAudioPlayer("Metronome", leftFactory, leftClipProvider);

            TempleRunBus.Subscribe(TempleRunEvents.TempleRunStarted, StartMetronome);
            TempleRunBus.Subscribe(TempleRunEvents.PlayerDied, StopMetronome);
        }

        private void OnDestroy()
        {
            TempleRunBus.Unsubscribe(TempleRunEvents.TempleRunStarted, StartMetronome);
            TempleRunBus.Unsubscribe(TempleRunEvents.PlayerDied, StopMetronome);
        }

        private void StartMetronome(string eventName, object sender, object eventData)
        {
            // Start the metronome ticking
            _metronomeCoroutine = StartCoroutine(MetronomeTick());
        }

        private void StopMetronome(string eventName, object sender, object eventData)
        {
            StopCoroutine(_metronomeCoroutine);
        }

        private IEnumerator MetronomeTick()
        {
            while (true)
            {
                // Play the tick sound
                AudioManagerSingleton.Instance.PlaySfx("Metronome", 1);
                float timeBetweenTicks = _speedTimeScale / Blackboard.Instance.CurrentSpeed; // Calculate time between ticks
                yield return new WaitForSeconds(timeBetweenTicks);
            }
        }
    }
}