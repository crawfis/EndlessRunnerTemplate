using GTMY.Audio;

using UnityEngine;

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
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.TurnLeftCompleted, PlayLeftTurnSound);

            var rightClipProvider = new AudioClipProvider(new System.Random());
            rightClipProvider.AddClip(_turnRightAudioClips);
            var rightFactory = new AudioFactoryPooled(this, this.gameObject);
            //AudioFactoryRegistry.Instance.RegisterAudioFactory("TurnRightPooledAudio", rightFactory);
            ISfxAudioPlayer sfxRightAudioPlayer = SfxAudioPlayerFactory.Instance.CreateSfxAudioPlayer("rightTurnFeedback", rightFactory, rightClipProvider);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.TurnRightCompleted, PlayRightTurnSound);
        }
        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.TurnLeftCompleted, PlayLeftTurnSound);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.TurnRightCompleted, PlayRightTurnSound);
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