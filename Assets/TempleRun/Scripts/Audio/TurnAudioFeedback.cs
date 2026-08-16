using GTMY.Audio;

using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

namespace CrawfisSoftware.TempleRun.Audio
{
    [RequireComponent(typeof(AudioSource))]
    internal class TurnAudioFeedback : MonoBehaviour
    {
        [SerializeField] private AudioClip _turnLeftAudioClips;
        [SerializeField] private AudioClip _turnRightAudioClips;

        private void Awake()
        {
            var leftClipProvider = new AudioClipProvider(new System.Random());
            leftClipProvider.AddClip(_turnLeftAudioClips);
            var leftFactory = new AudioFactoryPooled(this, this.gameObject);
            //AudioFactoryRegistry.Instance.RegisterAudioFactory("TurnLeftPooledAudio", leftFactory);
            ISfxAudioPlayer sfxAudioPlayer = SfxAudioPlayerFactory.Instance.CreateSfxAudioPlayer("leftTurnFeedback", leftFactory, leftClipProvider);
            TempleRunBus.Subscribe(TempleRunEvents.TurnLeftCompleted, PlayLeftTurnSound);

            var rightClipProvider = new AudioClipProvider(new System.Random());
            rightClipProvider.AddClip(_turnRightAudioClips);
            var rightFactory = new AudioFactoryPooled(this, this.gameObject);
            //AudioFactoryRegistry.Instance.RegisterAudioFactory("TurnRightPooledAudio", rightFactory);
            ISfxAudioPlayer sfxRightAudioPlayer = SfxAudioPlayerFactory.Instance.CreateSfxAudioPlayer("rightTurnFeedback", rightFactory, rightClipProvider);
            TempleRunBus.Subscribe(TempleRunEvents.TurnRightCompleted, PlayRightTurnSound);
        }
        private void OnDestroy()
        {
            TempleRunBus.Unsubscribe(TempleRunEvents.TurnLeftCompleted, PlayLeftTurnSound);
            TempleRunBus.Unsubscribe(TempleRunEvents.TurnRightCompleted, PlayRightTurnSound);
        }

        private static void PlayLeftTurnSound(string eventName, object sender, object data)
        {
            AudioManagerSingleton.Instance.PlaySfx("leftTurnFeedback", 1);
        }

        private static void PlayRightTurnSound(string eventName, object sender, object data)
        {
            AudioManagerSingleton.Instance.PlaySfx("rightTurnFeedback", 1);
        }
    }
}