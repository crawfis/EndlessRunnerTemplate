using CrawfisSoftware.Events;

using UnityEngine;
using UnityEngine.InputSystem;
using UserInputBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.Events.UserInitiatedEvents>;

// Note: CrawfisSoftware.Events import needed for EventsFor<UserInitiatedEvents>

namespace CrawfisSoftware.TempleRun.Input
{
    internal class PauseQuitInputActions : MonoBehaviour
    {
        [SerializeField] private InputActionAsset _inputAsset;

        const int PlayerNumber = 0;
        private GameControls _inputActions;
        private InputAction _quitAction;
        private InputAction _pauseToggleAction;

        private void Start()
        {
            _inputActions = new GameControls();
            _quitAction = _inputActions.GameControl.Quit;
            _quitAction.Enable();
            _quitAction.performed += TEMP_GameQuit;
            _pauseToggleAction = _inputActions.GameControl.PauseAndResume;
            _pauseToggleAction.Enable();
            _pauseToggleAction.performed += PauseResumeToggle_performed;
        }
        private void OnDestroy()
        {
            if (_quitAction != null) _quitAction.performed -= TEMP_GameQuit;
            if (_pauseToggleAction != null) _pauseToggleAction.performed -= PauseResumeToggle_performed;
            _inputActions?.Dispose();
        }

        private void TEMP_GameQuit(InputAction.CallbackContext obj)
        {
            UserInputBus.Publish(UserInitiatedEvents.UserQuitRequested, this, UnityEngine.Time.time);
        }

        private void PauseResumeToggle_performed(InputAction.CallbackContext obj)
        {
            UserInputBus.Publish(UserInitiatedEvents.UserPauseToggle, this, UnityEngine.Time.time);
        }
    }
}