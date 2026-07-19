using CrawfisSoftware.TempleRun.Input;
using CrawfisSoftware.Events;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Handles double-tap input detection for dash activation.
    /// Two taps within the window (0.3s) triggers a dash.
    ///    Dependencies: LeftRightJumpSlide input action map
    ///    Publishes: UserInitiatedEvents.DashRequested
    /// </summary>
    public class DashInputActions : MonoBehaviour
    {
        [SerializeField] private LeftRightJumpSlide _playerControls;
        [SerializeField] private float _doubleTapWindow = 0.3f;  // Time window for double-tap detection
        [SerializeField] private float _dashCooldown = 1.0f;  // Cooldown between dash activations

        private InputAction _tapAction;
        private float _lastTapTime = -10f;
        private float _lastDashTime = -10f;
        private const int PlayerNumber = 0;

        private void Awake()
        {
            _playerControls = new LeftRightJumpSlide();
        }

        private void OnEnable()
        {
            _playerControls.PlayerTouch.Enable();
            _tapAction = _playerControls.PlayerTouch.InitialPress;
            _tapAction.performed += OnTap;
            _tapAction.Enable();
        }

        private void OnDisable()
        {
            _tapAction.performed -= OnTap;
            _tapAction.Disable();
            _playerControls.PlayerTouch.Disable();
        }

        private void OnTap(InputAction.CallbackContext context)
        {
            // Check if this is a double-tap (second tap within window)
            float timeSinceLastTap = Time.time - _lastTapTime;

            if (timeSinceLastTap < _doubleTapWindow && timeSinceLastTap > 0.05f)
            {
                // Double-tap detected!
                if (Time.time - _lastDashTime >= _dashCooldown)
                {
                    DashAction_performed();
                    _lastDashTime = Time.time;
                }
                // Reset tap timer to prevent triple-tap registering as another double-tap
                _lastTapTime = -10f;
            }
            else
            {
                // First tap or too slow
                _lastTapTime = Time.time;
            }
        }

        private void DashAction_performed()
        {
            EventsPublisherUserInitiated.Instance.PublishEvent(
                UserInitiatedEvents.UserDashRequested, this, PlayerNumber);
        }
    }
}
