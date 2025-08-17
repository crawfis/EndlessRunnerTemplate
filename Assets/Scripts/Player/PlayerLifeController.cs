using System;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Manages the number of lives a player has, converting the PlayerFailed event to a PlayerDied event when 
    /// all of the lives run out.
    ///    Dependencies: EventsPublisherTempleRun
    ///    Subscribes: PlayerFailed event
    ///    Publishes: PlayerDied event. Data can be a player id.
    /// </summary>
    internal class PlayerLifeController : IDisposable
    {
        private int _numberOfLives;
        private readonly int _playerID;

        public PlayerLifeController(int numberOfLives, int playerID = 0)
        {
            _numberOfLives = numberOfLives;
            _playerID = playerID;
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.PlayerFailed, OnPlayerFailed);
        }

        private void OnPlayerFailed(string eventName, object sender, object data)
        {
            // Todo: Check playerID
            _numberOfLives--;
            if (_numberOfLives <= 0)
            {
                EventsPublisherTempleRun.Instance.PublishEvent(KnownEvents.PlayerDied, this, _playerID);
            }
        }

        // Not used unless we change this to a MonoBehaviour. Left here to make sure we do this in that case.
        private void OnDestroy()
        {
            Dispose();
        }

        public void Dispose()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.PlayerFailed, OnPlayerFailed);
        }
    }
}