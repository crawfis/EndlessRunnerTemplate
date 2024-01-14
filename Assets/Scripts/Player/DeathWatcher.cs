using System;
using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Compares the distance from DistanceTracker and compares it to the current track length. Fires the PlayerFailed event if the
    /// distance is greater than (or equal) the active track distance.
    ///    Dependencies: DistanceTracker, EventsPublisherTempleRun
    ///    Subscribes: ActiveTrackChanged - increased the active track length
    ///    Subscribes: GameStarted - useful if there is a delay between when the tracks are sent and the player has control.
    ///    Subscribes: GameEnded - useful if multiple players and we need to stop the checking.
    ///    Publishes: PlayerFailed event. Data is the current player distance.
    /// </summary>
    /// <remarks> For local multi-player we may need a player ID. Would be good to include this in the event data.</remarks>
    internal class DeathWatcher : MonoBehaviour
    {
        [SerializeField] private DistanceTracker _distanceTracker;

        private float _currentSegmentDistance = 0f;
        private bool _isRunning = false;
        private bool _gameStarted = false;

        private void Awake()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.ActiveTrackChanged, OnTrackChanged);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.GameStarted, OnGameStarted);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.GameOver, OnGameEnded);
        }

        private void Update()
        {
            if (_isRunning && _gameStarted && _distanceTracker.DistanceTravelled >= _currentSegmentDistance)
            {
                _isRunning = false;
                Debug.Log(string.Format("Player Died at Distance: {0}", (int)_currentSegmentDistance));
                EventsPublisherTempleRun.Instance.PublishEvent(KnownEvents.PlayerFailed, this, _distanceTracker.DistanceTravelled);
            }
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.ActiveTrackChanged, OnTrackChanged);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.GameStarted, OnGameStarted);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.GameOver, OnGameEnded);
        }

        private void OnTrackChanged(object sender, object data)
        {
            _isRunning = true;
            var tuple = (ValueTuple<Direction, float>)data;
            _currentSegmentDistance += tuple.Item2;
        }

        private void OnGameStarted(object sender, object data)
        {
            _gameStarted = true;
        }

        private void OnGameEnded(object sender, object data)
        {
            _gameStarted = false;
        }
    }
}