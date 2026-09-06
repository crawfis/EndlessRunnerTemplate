using System.Collections;

using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Speed controller that updates a DistanceTracker.
    ///    Dependencies: Blackboard, DistanceTracker and GameConfig (from Blackboard)
    ///    Subscribes: PlayerActivated — distance starts when the player is released, not when the
    ///                run's systems come up (the countdown ceremony sits between the two)
    ///    Subscribes: PlayerFailing — resets speed to initial after a non-fatal failure
    ///    Subscribes: TempleRunEnded — stops the run, however the run ended
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
            TempleRunBus.Subscribe(TempleRunEvents.PlayerFailing, OnResetSpeed);
            TempleRunBus.Subscribe(TempleRunEvents.PlayerActivated, OnPlayerActivated);
            TempleRunBus.Subscribe(TempleRunEvents.TempleRunEnded, OnGameOver);
            TempleRunBus.Subscribe(TempleRunEvents.TeleportStarted, OnTeleportStarted);
            TempleRunBus.Subscribe(TempleRunEvents.TeleportEnded, OnTeleportEnded);
        }

        private void OnDestroy()
        {
            TempleRunBus.Unsubscribe(TempleRunEvents.PlayerFailing, OnResetSpeed);
            TempleRunBus.Unsubscribe(TempleRunEvents.PlayerActivated, OnPlayerActivated);
            TempleRunBus.Unsubscribe(TempleRunEvents.TempleRunEnded, OnGameOver);
            TempleRunBus.Unsubscribe(TempleRunEvents.TeleportStarted, OnTeleportStarted);
            TempleRunBus.Unsubscribe(TempleRunEvents.TeleportEnded, OnTeleportEnded);
            DeleteCoroutine();
        }

        private void OnResetSpeed(string eventName, object sender, object data)
        {
            _speed = _initialSpeed;
        }

        private void OnPlayerActivated(string eventName, object sender, object data)
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
        /// Snaps the distance tracker to the section's LandingDistance, stamped by
        /// SegmentTransitionController when it published the exit section.
        /// </summary>
        private void OnTeleportEnded(string eventName, object sender, object data)
        {
            _isMoving = true;
            // A teleport only ever runs onto a section that owns the transform, and every such
            // section carries a real landing. The old `> 0f` test read a sentinel the approach
            // section used to park in an unnamed fourth tuple slot; approaches never reached
            // here, and no longer carry one.
            var section = (SplineSection)data;
            float delta = section.LandingDistance - Blackboard.Instance.DistanceTracker.DistanceTravelled;
            Blackboard.Instance.DistanceTracker.UpdateDistance(delta);
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
                        TempleRunBus.Publish(
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
