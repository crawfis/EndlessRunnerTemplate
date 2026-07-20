using CrawfisSoftware.TempleRun.Input;
using CrawfisSoftware.Events;

using System.Collections;

using UnityEngine;
using UnityEngine.InputSystem;

namespace CrawfisSoftware.TempleRun
{
    public class SwipeDetectorActions : MonoBehaviour
    {
        [SerializeField] private LeftRightJumpSlide _playerControls;
        const int PlayerNumber = 0;
        private InputAction _swipePressed, _swipePosition;
        private Vector2 _startPosition;
        private bool _isPressing;
        private bool _isCoolingDown = false;

        void Awake()
        {
            _playerControls = new LeftRightJumpSlide();
        }

        void OnEnable()
        {
            _playerControls.PlayerTouch.Enable();
            // Use a named handler so OnDisable can actually remove it. A lambda here
            // would allocate a new delegate instance that -= can never match.
            _playerControls.PlayerTouch.InitialPress.performed += OnInitialPressPerformed;
            _swipePressed = _playerControls.PlayerTouch.InitialPress;
            _swipePressed.Enable();
            _swipePosition = _playerControls.PlayerTouch.EndOfSwipe;
            _swipePosition.Enable();
        }

        void OnDisable()
        {
            _playerControls.PlayerTouch.InitialPress.performed -= OnInitialPressPerformed;
            _swipePressed.Disable();
            _swipePosition.Disable();
            _playerControls.PlayerTouch.Disable();
        }

        private void OnInitialPressPerformed(InputAction.CallbackContext ctx)
        {
            OnPress(true, ctx.ReadValue<float>());
        }

        private void Update()
        {
            var swipeDelta = _swipePosition.ReadValue<Vector2>();
            if(swipeDelta.magnitude > 100 && _isPressing)
            {
                _isPressing = false;
                //Vector2 endPosition = _swipePosition.ReadValue<Vector2>();
                //Vector2 swipeVector = endPosition - _startPosition;
                DetectSwipeDirection(swipeDelta.normalized);
            }
        }
        private void OnPress(bool pressed, float pressure)
        {
            if(_isCoolingDown) return;
            if (pressed)
            {
                _isPressing = true;
                //_startPosition = _playerControls.PlayerTouch.InitialPress.ReadValue<float>();
                //_startPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                _startPosition = Touchscreen.current?.primaryTouch.position.ReadValue() ?? Vector2.zero;
            }
            else if (_isPressing)
            {
                _isPressing = false;
                Vector2 endPosition = _swipePosition.ReadValue<Vector2>();
                Vector2 swipeVector = endPosition - _startPosition;

                if (swipeVector.magnitude > 100)
                {
                    DetectSwipeDirection(swipeVector.normalized);
                }
            }
        }

        private void DetectSwipeDirection(Vector2 swipeDirection)
        {
            // Determine if swipe is primarily horizontal or vertical
            if (Mathf.Abs(swipeDirection.x) > Mathf.Abs(swipeDirection.y))
            {
                // Horizontal swipe - lane changes
                if (swipeDirection.x > 0) RightAction_performed();
                else LeftAction_performed();
            }
            else
            {
                // Vertical swipe - jump or slide
                if (swipeDirection.y > 0) JumpAction_performed();
                else SlideAction_performed();
            }
        }

        // Slightly modified from MovementInputActions.cs
        private void LeftAction_performed()
        {
            _swipePressed.Disable();
            EventsPublisherUserInitiated.Instance.PublishEvent(UserInitiatedEvents.UserLeftTurnRequested, this, PlayerNumber);
            StartCoroutine(EnableAfterDelay(_swipePressed));
        }

        private void RightAction_performed()
        {
            _swipePressed.Disable();
            EventsPublisherUserInitiated.Instance.PublishEvent(UserInitiatedEvents.UserRightTurnRequested, this, PlayerNumber);
            StartCoroutine(EnableAfterDelay(_swipePressed));
        }

        private void JumpAction_performed()
        {
            _swipePressed.Disable();
            EventsPublisherUserInitiated.Instance.PublishEvent(UserInitiatedEvents.UserJumpRequested, this, PlayerNumber);
            StartCoroutine(EnableAfterDelay(_swipePressed));
        }

        private void SlideAction_performed()
        {
            _swipePressed.Disable();
            EventsPublisherUserInitiated.Instance.PublishEvent(UserInitiatedEvents.UserSlideRequested, this, PlayerNumber);
            StartCoroutine(EnableAfterDelay(_swipePressed));
        }

        private IEnumerator EnableAfterDelay(InputAction actionToEnable)
        {
            yield return new WaitForSeconds(Blackboard.Instance.GameConfig.InputCoolDownForTurns);
            actionToEnable.Enable();
        }
    }
}