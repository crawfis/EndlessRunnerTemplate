using CrawfisSoftware.TempleRun;
using CrawfisSoftware.Events;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CrawfisSoftware.TempleRun.Input
{
    /// <summary>
    /// Handles device accelerometer input for lane changes via device tilt.
    /// Uses the new Input System to poll the Accelerometer device.
    /// Left tilt (accel.x > threshold) → left lane change
    /// Right tilt (accel.x < -threshold) → right lane change
    ///    Dependencies: Input System Accelerometer device (mobile devices)
    ///    Publishes: UserInitiatedEvents.LeftLaneChangeRequested, UserInitiatedEvents.RightLaneChangeRequested
    /// </summary>
    public class AccelerometerInputActions : MonoBehaviour
    {
        [Header("Accelerometer Settings")]
        [SerializeField] private float _tiltThreshold = 0.3f;  // Deadzone threshold for meaningful tilt
        [SerializeField] private float _cooldownSeconds = 0.2f;  // Minimum time between lane changes
        [SerializeField] private bool _enableAccelerometer = true;  // Can be disabled if not available on device

        private float _lastLaneChangeTime = -1f;
        private const int PlayerNumber = 0;
        private Accelerometer _accelerometer;

        private void OnEnable()
        {
            if (!_enableAccelerometer)
                return;

            // Request accelerometer access from the Input System
            if (Accelerometer.current != null)
            {
                _accelerometer = Accelerometer.current;
                InputSystem.EnableDevice(_accelerometer);
            }
            else
            {
                Debug.LogWarning("Accelerometer device not available on this device");
                _enableAccelerometer = false;
            }
        }

        private void OnDisable()
        {
            if (_accelerometer != null)
            {
                InputSystem.DisableDevice(_accelerometer);
                _accelerometer = null;
            }
        }

        private void Update()
        {
            if (!_enableAccelerometer || _accelerometer == null)
                return;

            if (Time.time - _lastLaneChangeTime < _cooldownSeconds)
                return;

            // Get current accelerometer reading
            Vector3 accel = _accelerometer.acceleration.ReadValue();

            // Left tilt (positive X acceleration)
            if (accel.x > _tiltThreshold)
            {
                LeftLaneChangeAction_performed();
                _lastLaneChangeTime = Time.time;
            }
            // Right tilt (negative X acceleration)
            else if (accel.x < -_tiltThreshold)
            {
                RightLaneChangeAction_performed();
                _lastLaneChangeTime = Time.time;
            }
        }

        private void LeftLaneChangeAction_performed()
        {
            EventsPublisherUserInitiated.Instance.PublishEvent(
                UserInitiatedEvents.UserLeftLaneChangeRequested, this, PlayerNumber);
        }

        private void RightLaneChangeAction_performed()
        {
            EventsPublisherUserInitiated.Instance.PublishEvent(
                UserInitiatedEvents.UserRightLaneChangeRequested, this, PlayerNumber);
        }
    }
}
