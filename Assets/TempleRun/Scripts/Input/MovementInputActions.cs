using CrawfisSoftware.TempleRun.Input;
using CrawfisSoftware.Events;

using System.Collections;

using UnityEngine;
using UnityEngine.InputSystem;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Maps Unity Input System actions to UserInitiatedEvents.
    ///    Dependencies: Blackboard, LaneConfig, JumpConfig
    ///    Publishes: UserInitiatedEvents.LeftTurnRequested, RightTurnRequested,
    ///               LeftLaneChangeRequested, RightLaneChangeRequested, JumpRequested
    /// </summary>
    public class MovementInputActions : MonoBehaviour
    {
        [SerializeField] private LeftRightJumpSlide _inputAsset;

        const int PlayerNumber = 0;
        private LeftRightJumpSlide _inputActions;
        private InputAction _leftAction;
        private InputAction _rightAction;
        private InputAction _laneLeftAction;
        private InputAction _laneRightAction;
        private InputAction _jumpAction;
        private InputAction _slideAction;
        private InputAction _dashAction;

        private void OnEnable()
        {
            if (_inputActions == null)
                _inputActions = new LeftRightJumpSlide();
            _inputActions.Enable();
            _leftAction = _inputActions.Player.Left;
            _leftAction.Enable();
            _leftAction.performed += LeftAction_performed;
            _rightAction = _inputActions.Player.Right;
            _rightAction.Enable();
            _rightAction.performed += RightAction_performed;

            _laneLeftAction = _inputActions.Player.LaneLeft;
            _laneLeftAction.Enable();
            _laneLeftAction.performed += LaneLeftAction_performed;
            _laneRightAction = _inputActions.Player.LaneRight;
            _laneRightAction.Enable();
            _laneRightAction.performed += LaneRightAction_performed;

            _jumpAction = _inputActions.Player.Jump;
            _jumpAction.Enable();
            _jumpAction.performed += JumpAction_performed;

            _slideAction = _inputActions.Player.Slide;
            _slideAction.Enable();
            _slideAction.performed += SlideAction_performed;

            _dashAction = _inputActions.Player.Dash;
            _dashAction.Enable();
            _dashAction.performed += DashAction_performed;
        }

        private void OnDisable()
        {
            _inputActions.Disable();
            _leftAction.Disable();
            _leftAction.performed -= LeftAction_performed;
            _rightAction.Disable();
            _rightAction.performed -= RightAction_performed;

            if (_laneLeftAction != null)
            {
                _laneLeftAction.Disable();
                _laneLeftAction.performed -= LaneLeftAction_performed;
            }
            if (_laneRightAction != null)
            {
                _laneRightAction.Disable();
                _laneRightAction.performed -= LaneRightAction_performed;
            }
            if (_jumpAction != null)
            {
                _jumpAction.Disable();
                _jumpAction.performed -= JumpAction_performed;
            }
            _slideAction?.Disable();
            _slideAction.performed -= SlideAction_performed;
            _dashAction?.Disable();
            _dashAction.performed -= DashAction_performed;
        }

        private void OnDestroy()
        {
            if (_inputActions != null)
            {
                _inputActions.Dispose();
            }
        }

        private void LeftAction_performed(InputAction.CallbackContext obj)
        {
            _leftAction.Disable();
            EventsPublisherUserInitiated.Instance.PublishEvent(UserInitiatedEvents.UserLeftTurnRequested, this, PlayerNumber);
            StartCoroutine(EnableAfterDelay(_leftAction));
        }

        private void RightAction_performed(InputAction.CallbackContext obj)
        {
            _rightAction.Disable();
            EventsPublisherUserInitiated.Instance.PublishEvent(UserInitiatedEvents.UserRightTurnRequested, this, PlayerNumber);
            StartCoroutine(EnableAfterDelay(_rightAction));
        }

        private void LaneLeftAction_performed(InputAction.CallbackContext obj)
        {
            _laneLeftAction.Disable();
            EventsPublisherUserInitiated.Instance.PublishEvent(UserInitiatedEvents.UserLeftLaneChangeRequested, this, PlayerNumber);
            StartCoroutine(EnableLaneAfterDelay(_laneLeftAction));
        }

        private void LaneRightAction_performed(InputAction.CallbackContext obj)
        {
            _laneRightAction.Disable();
            EventsPublisherUserInitiated.Instance.PublishEvent(UserInitiatedEvents.UserRightLaneChangeRequested, this, PlayerNumber);
            StartCoroutine(EnableLaneAfterDelay(_laneRightAction));
        }

        private void JumpAction_performed(InputAction.CallbackContext obj)
        {
            _jumpAction.Disable();
            EventsPublisherUserInitiated.Instance.PublishEvent(UserInitiatedEvents.UserJumpRequested, this, PlayerNumber);
            StartCoroutine(EnableJumpAfterDelay(_jumpAction));
        }

        private void SlideAction_performed(InputAction.CallbackContext obj)
        {
            _slideAction.Disable();
            EventsPublisherUserInitiated.Instance.PublishEvent(UserInitiatedEvents.UserSlideRequested, this, PlayerNumber);
            StartCoroutine(EnableAfterDelay(_slideAction));
        }

        private void DashAction_performed(InputAction.CallbackContext obj)
        {
            _dashAction.Disable();
            EventsPublisherUserInitiated.Instance.PublishEvent(UserInitiatedEvents.UserDashRequested, this, PlayerNumber);
            StartCoroutine(EnableAfterDelay(_dashAction));
        }

        private IEnumerator EnableAfterDelay(InputAction actionToEnable)
        {
            yield return new WaitForSeconds(Blackboard.Instance.GameConfig.InputCoolDownForTurns);
            actionToEnable.Enable();
        }

        private IEnumerator EnableLaneAfterDelay(InputAction actionToEnable)
        {
            float cooldown = Blackboard.Instance.LaneConfig != null
                ? Blackboard.Instance.LaneConfig.LaneChangeCooldown
                : 0.3f;
            yield return new WaitForSeconds(cooldown);
            actionToEnable.Enable();
        }

        private IEnumerator EnableJumpAfterDelay(InputAction actionToEnable)
        {
            float cooldown = Blackboard.Instance.JumpConfig != null
                ? Blackboard.Instance.JumpConfig.JumpCooldown
                : 0.6f;
            yield return new WaitForSeconds(cooldown);
            actionToEnable.Enable();
        }
    }
}
