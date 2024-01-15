using System;
using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    internal class AIController : MonoBehaviour
    {
        [SerializeField] private TurnController _turnController;
        [SerializeField] private float _turnDistance = .1f;

        private bool _gameStarted = false;

        private void Awake()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.GameStarted, OnGameStarted);
        }

        private void OnGameStarted(object arg1, object arg2)
        {
            _gameStarted = true;
        }

        private void Update()
        {
            float distance = Blackboard.Instance.DistanceTracker.DistanceTravelled;
            if (!_gameStarted || _turnController.TurnFailedDistance - _turnDistance > distance) return;
            switch(_turnController.TurnDirection)
            {
                case Direction.Left:
                    EventsPublisherTempleRun.Instance.PublishEvent(KnownEvents.LeftTurnRequested, this, distance);
                    break;
                default:
                    EventsPublisherTempleRun.Instance.PublishEvent(KnownEvents.RightTurnRequested, this, distance);
                    break;
            }
        }
    }
}