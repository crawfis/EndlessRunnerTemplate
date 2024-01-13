using System.Collections;
using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Overall game control handling pausing, resuming and player failing.
    ///    Dependency: PlayerLifeController, EventsPublisherTempleRun
    ///    Subscribes: PlayerDied - Publishes a GameOver event
    ///    Subscribes: Pause - pauses be setting time scale to zero
    ///    Subscribes: Resume - resets the time scale to one
    ///    Publishes: GameStarted
    ///    Publishes: GameOver
    /// </summary>
    internal class GameController : MonoBehaviour
    {
        private PlayerLifeController _playerLifeController; // Example of using a non-MonoBehaviour class

        private void Awake()
        {
            //Time.timeScale = 0.0f;
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.PlayerDied, OnPlayerDied);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.Pause, OnPause);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.Resume, OnResume);
        }

        private void Start()
        {
            //_playerLifeController = new PlayerLifeController(int.MaxValue, 0);
            _playerLifeController = new PlayerLifeController(3, 0);
            _ = StartCoroutine(StartGame());
        }

        private IEnumerator StartGame()
        {
            //Time.timeScale = 0.0f;
            yield return new WaitForSecondsRealtime(GameConstants.StartDelay);
            EventsPublisherTempleRun.Instance.PublishEvent(KnownEvents.GameStarted, this, null);
            //Time.timeScale = 1.0f;
        }

        private void OnPlayerDied(object sender, object data)
        {
            EventsPublisherTempleRun.Instance.PublishEvent(KnownEvents.GameOver, this, null);
        }

        private void OnPause(object sender, object data)
        {
            Time.timeScale = 0.0f;
        }

        private void OnResume(object sender, object data)
        {
            Time.timeScale = 1.0f;
        }


        private void OnDestroy()
        {
            UnsubscribeToEvents();
        }

        private void UnsubscribeToEvents()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.PlayerDied, OnPlayerDied);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.Pause, OnPause);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.Resume, OnResume);
        }
    }
}