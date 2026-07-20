using System.Collections;

using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Speed controller that updates a DistanceTracker.
    ///    Dependencies: Blackboard, DistanceTracker and GameConfig (from Blackboard)
    ///    Subscribes: TempleRunStarted
    ///    Subscribes: PlayerDied
    ///    Subscribes: TeleportStarted — pauses movement during cinematic teleport
    ///    Subscribes: TeleportEnded — snaps distance to LandingDistance from event data, resumes movement
    /// </summary>
    internal class DistanceController : MonoBehaviour
    {
        private float _initialSpeed;
        private float _maxSpeed;
        private float _acceleration;
        private float _speed;
        private Coroutine _coroutine;
        private bool _isMoving = true;
        private int _distancePublishIndex = 0;
        private float _nextDistancePublishThreshold = 0f;
        private static readonly float[] DistancePublishThresholds = { 25f, 100f, 200f, 500f, 1000f, 2000f, 5000f };

        private void Awake()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.PlayerFailing, OnResetSpeed);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.TempleRunStarted, OnGameStarted);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.PlayerDied, OnGameOver);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.TeleportStarted, OnTeleportStarted);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.TeleportEnded, OnTeleportEnded);
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.PlayerFailing, OnResetSpeed);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.TempleRunStarted, OnGameStarted);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.PlayerDied, OnGameOver);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.TeleportStarted, OnTeleportStarted);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.TeleportEnded, OnTeleportEnded);
            DeleteCoroutine();
        }

        private void OnResetSpeed(string eventName, object sender, object data)
        {
            _speed = _initialSpeed;
        }

        private void OnGameStarted(string eventName, object sender, object data)
        {
            _initialSpeed = Blackboard.Instance.GameConfig.InitialSpeed;
            _maxSpeed = Blackboard.Instance.GameConfig.MaxSpeed;
            _acceleration = Blackboard.Instance.GameConfig.Acceleration;
            _speed = _initialSpeed;
            _distancePublishIndex = 0;
            _nextDistancePublishThreshold = DistancePublishThresholds[0];
            _coroutine = StartCoroutine(UpdateAfterGameStart());
        }

        private void OnGameOver(string eventName, object sender, object data)
        {
            DeleteCoroutine();
        }

        private void OnTeleportStarted(string eventName, object sender, object data)
        {
            _isMoving = false;
        }

        /// <summary>
        /// Snaps the distance tracker to the LandingDistance provided by
        /// SegmentTransitionController via the event data tuple.
        /// </summary>
        private void OnTeleportEnded(string eventName, object sender, object data)
        {
            _isMoving = true;
            var (_, _, _, landingDistance) = ((Vector3, Vector3, Direction, float))data;
            if (landingDistance > 0f)
            {
                float delta = landingDistance - Blackboard.Instance.DistanceTracker.DistanceTravelled;
                Blackboard.Instance.DistanceTracker.UpdateDistance(delta);
            }
        }

        IEnumerator UpdateAfterGameStart()
        {
            DistanceTracker _distanceTracker = Blackboard.Instance.DistanceTracker;
            while (true)
            {
                if (_isMoving)
                {
                    // Apply both dash and slide speed multipliers (both default to 1.0 when inactive)
                    float effectiveSpeed = _speed * Blackboard.Instance.CurrentDashMultiplier * Blackboard.Instance.CurrentSlideMultiplier;
                    _distanceTracker.UpdateDistance(effectiveSpeed * GameTime.Instance.deltaTime);

                    if (_distanceTracker.DistanceTravelled >= _nextDistancePublishThreshold)
                    {
                        EventsPublisherTempleRun.Instance.PublishEvent(
                            TempleRunEvents.DistanceUpdated, this, _distanceTracker.DistanceTravelled);

                        if (_distancePublishIndex < DistancePublishThresholds.Length - 1)
                        {
                            _distancePublishIndex++;
                            _nextDistancePublishThreshold = DistancePublishThresholds[_distancePublishIndex];
                        }
                        else
                        {
                            _nextDistancePublishThreshold += DistancePublishThresholds[_distancePublishIndex];
                        }
                    }

                    _speed += _acceleration * GameTime.Instance.deltaTime;
                    _speed = Mathf.Clamp(_speed, _initialSpeed, _maxSpeed);
                    Blackboard.Instance.CurrentSpeed = _speed;
                }
                yield return new WaitForEndOfFrame();
            }
        }

        private void DeleteCoroutine()
        {
            if (_coroutine != null) StopCoroutine(_coroutine);
            _coroutine = null;
        }
    }
}
