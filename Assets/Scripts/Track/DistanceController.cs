using System.Collections;
using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Speed controller that updates a DistanceTracker.
    ///    Dependencies: Blackboard, DistanceTracker and GameConfig (from Blackboard)
    ///    Subscribes: GameStarted
    ///    Subscribes: GameOver
    ///    Subscribes: TeleportStarted
    ///    Subscribes: TeleportEnded
    /// </summary>
    internal class DistanceController : MonoBehaviour
    {
        private float _initialSpeed;
        private float _maxSpeed;
        private float _acceleration;
        private float _speed;
        private Coroutine _coroutine;
        private bool _isMoving = true;
        private float _trackDistance = 0;

        private void Awake()
        {
            _initialSpeed = Blackboard.Instance.GameConfig.InitialSpeed;
            _maxSpeed = Blackboard.Instance.GameConfig.MaxSpeed;
            _acceleration = Blackboard.Instance.GameConfig.Acceleration;
            _speed = _initialSpeed;
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.PlayerFailed, OnResetSpeed);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.GameStarted, OnGameStarted);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.GameOver, OnGameOver);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.TeleportStarted, OnTeleportStarted);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.TeleportEnded, OnTeleportEnded);
            //EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.ActiveTrackChanging, OnTrackChanging);
        }

        private void OnResetSpeed(object arg1, object arg2)
        {
            _speed = _initialSpeed;
        }

        private void OnGameStarted(object sender, object data)
        {
            float initialDistance = Blackboard.Instance.TrackWidthOffset;
            //Blackboard.Instance.DistanceTracker.UpdateDistance(initialDistance);
            _coroutine = StartCoroutine(UpdateAfterGameStart());
        }

        private void OnGameOver(object sender, object data)
        {
            DeleteCoroutine();
        }

        //private void OnTrackChanging(object sender, object data)
        //{
        //    var (_, segmentDistance) = ((Direction direction, float segmentDistance))data;
        //    _trackDistance += segmentDistance;
        //}

        private void OnTeleportStarted(object sender, object data)
        {
            _isMoving = false;
        }

        private void OnTeleportEnded(object sender, object data)
        {
            _isMoving = true;
            Blackboard.Instance.DistanceTracker.UpdateDistance(_trackDistance - Blackboard.Instance.DistanceTracker.DistanceTravelled);
            var (point1, point2, _) = ((Vector3 point1, Vector3 point2, Direction direction))data;
            _trackDistance += Vector3.Magnitude(point1 - point2);
        }

        IEnumerator UpdateAfterGameStart()
        {
            DistanceTracker _distanceTracker = Blackboard.Instance.DistanceTracker;
            while (true)
            {
                if (_isMoving)
                {
                    _distanceTracker.UpdateDistance(_speed * Time.deltaTime);
                    _speed += _acceleration * Time.deltaTime;
                    _speed = Mathf.Clamp(_speed, _initialSpeed, _maxSpeed);
                }
                yield return new WaitForEndOfFrame();
            }
        }

        // Typically called when a player loses a life.
        public void Reset()
        {
            _speed = _initialSpeed;
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.PlayerFailed, OnResetSpeed);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.GameStarted, OnGameStarted);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.GameOver, OnGameOver);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.TeleportStarted, OnTeleportStarted);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.TeleportEnded, OnTeleportEnded);
            DeleteCoroutine();
        }

        private void DeleteCoroutine()
        {
            if (_coroutine != null) StopCoroutine(_coroutine);
            _coroutine = null;
        }
    }
}