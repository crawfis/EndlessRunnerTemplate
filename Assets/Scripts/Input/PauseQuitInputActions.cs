using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.InputSystem;

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
        private void TEMP_GameQuit(InputAction.CallbackContext obj)
        {
            EventsPublisherTempleRun.Instance.PublishEvent(KnownEvents.GameOver, this, null);
        }

        private void PauseResumeToggle_performed(InputAction.CallbackContext obj)
        {
            EventsPublisherTempleRun.Instance.PublishEvent(KnownEvents.PauseToggle, this, null);
        }
    }
}