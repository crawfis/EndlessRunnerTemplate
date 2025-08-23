using GTMY.Audio;

using System.Collections;

using UnityEngine;

namespace CrawfisSoftware.TempleRun.Audio
{
    internal class Metronome : MonoBehaviour
    {
        [SerializeField] private AudioClip _tickSound;
        [SerializeField] private float _speedTimeScale = 6f;

        private Coroutine _metronomeCoroutine;
        private void Awake()
        {
            var leftClipProvider = new AudioClipProvider(new System.Random());
            leftClipProvider.AddClip(_tickSound);
            var leftFactory = new AudioFactoryPooled(this, null);
            //AudioFactoryRegistry.Instance.RegisterAudioFactory("TurnLeftPooledAudio", leftFactory);
            ISfxAudioPlayer sfxAudioPlayer = SfxAudioPlayerFactory.Instance.CreateSfxAudioPlayer("Metronome", leftFactory, leftClipProvider);

            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.GameStarted, StartMetronome);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.GameOver, StopMetronome);
        }

        private void StopMetronome(string eventName, object sender, object eventData)
        {
            StopCoroutine(_metronomeCoroutine);
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.GameStarted, StartMetronome);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.GameOver, StopMetronome);
        }

        private void StartMetronome(string eventName, object sender, object eventData)
        {
            // Start the metronome ticking
            _metronomeCoroutine = StartCoroutine(MetronomeTick());
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