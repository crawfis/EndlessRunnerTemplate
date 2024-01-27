using System;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Game initialization logic. Sets the number of player lives.
    /// </summary>
    public class GameInitialization : IDisposable
    {
        private PlayerLifeController _playerLifeController;
        public GameInitialization(int numberOfLives)
        {
            _playerLifeController = new PlayerLifeController(numberOfLives, 0);
            var distanceTracker = new DistanceTracker();
            Blackboard.Instance.DistanceTracker = distanceTracker;
        }

        public void Dispose()
        {
            _playerLifeController?.Dispose();
            _playerLifeController = null;
        }
    }
}