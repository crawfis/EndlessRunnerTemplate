using GTMY.Audio;

using UnityEngine;

namespace CrawfisSoftware.TempleRun.Audio
{
    internal class TurnFeedback : MonoBehaviour
    {
        [SerializeField] private AudioClip _turnLeftAudioClips;
        [SerializeField] private AudioClip _turnRightAudioClips;

        private void Awake()
        {
            var leftClipProvider = new AudioClipProvider(new System.Random());
            leftClipProvider.AddClip(_turnLeftAudioClips);
            var leftFactory = new AudioFactoryPooled(this, null);
            //AudioFactoryRegistry.Instance.RegisterAudioFactory("TurnLeftPooledAudio", leftFactory);
            ISfxAudioPlayer sfxAudioPlayer = SfxAudioPlayerFactory.Instance.CreateSfxAudioPlayer("leftTurnFeedback", leftFactory, leftClipProvider);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.LeftTurnSucceeded, (_, _, _) =>
            {
                AudioManagerSingleton.Instance.PlaySfx("leftTurnFeedback", 1);
            });

            var rightClipProvider = new AudioClipProvider(new System.Random());
            rightClipProvider.AddClip(_turnRightAudioClips);
            var rightFactory = new AudioFactoryPooled(this, null);
            //AudioFactoryRegistry.Instance.RegisterAudioFactory("TurnRightPooledAudio", rightFactory);
            ISfxAudioPlayer sfxRightAudioPlayer = SfxAudioPlayerFactory.Instance.CreateSfxAudioPlayer("rightTurnFeedback", rightFactory, rightClipProvider);

            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.RightTurnSucceeded, (_, _, _) =>
            {
                AudioManagerSingleton.Instance.PlaySfx("rightTurnFeedback", 1);
            });
        }
    }
}
