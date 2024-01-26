namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Manages the number of lives a player has, converting the PlayerFailed event to a PlayerDied event when 
    /// all of the lives run out.
    ///    Dependencies: none.
    ///    Subscribes: PlayerFailed event
    ///    Publishes: PlayerDied event. Data can be a player id.
    /// </summary>
    internal class PlayerLifeController
    {
        private int _numberOfLives;
        private readonly int _playerID;

        public PlayerLifeController(int numberOfLives, int playerID = 0)
        {
            _numberOfLives = numberOfLives;
            _playerID = playerID;
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.PlayerFailed, OnPlayerFailed);
        }

        private void OnPlayerFailed(object sender, object data)
        {
            // Todo: Check playerID
            _numberOfLives--;
            if (_numberOfLives <= 0)
            {
                EventsPublisherTempleRun.Instance.PublishEvent(KnownEvents.PlayerDied, this, _playerID);
            }
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.PlayerFailed, OnPlayerFailed);
        }
    }
}