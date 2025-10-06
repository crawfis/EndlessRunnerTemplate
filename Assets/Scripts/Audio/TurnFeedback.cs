using GTMY.Audio;

using UnityEngine;

namespace CrawfisSoftware.TempleRun.Audio
{
    [RequireComponent(typeof(AudioSource))]
    internal class TurnFeedback : MonoBehaviour
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
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.LeftTurnSucceeded, PlayLeftTurnSound);

            var rightClipProvider = new AudioClipProvider(new System.Random());
            rightClipProvider.AddClip(_turnRightAudioClips);
            var rightFactory = new AudioFactoryPooled(this, this.gameObject);
            //AudioFactoryRegistry.Instance.RegisterAudioFactory("TurnRightPooledAudio", rightFactory);
            ISfxAudioPlayer sfxRightAudioPlayer = SfxAudioPlayerFactory.Instance.CreateSfxAudioPlayer("rightTurnFeedback", rightFactory, rightClipProvider);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.RightTurnSucceeded, PlayRightTurnSound);
        }

        private static void PlayLeftTurnSound(string eventName, object sender, object data)
        {
            AudioManagerSingleton.Instance.PlaySfx("leftTurnFeedback", 1);
        }

        private static void PlayRightTurnSound(string eventName, object sender, object data)
        {
            AudioManagerSingleton.Instance.PlaySfx("rightTurnFeedback", 1);
        }
        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.LeftTurnSucceeded, PlayLeftTurnSound);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.RightTurnSucceeded, PlayRightTurnSound);
        }
    }
}
