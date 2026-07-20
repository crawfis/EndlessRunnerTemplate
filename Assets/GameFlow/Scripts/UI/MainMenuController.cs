using CrawfisSoftware.GameFlow.Events;

using UnityEngine;
using UnityEngine.UIElements;

namespace CrawfisSoftware.GameFlow.UI
{
    /// <summary>
    /// Wires the main menu buttons to GameFlow events.
    ///    Subscribes: none
    ///    Publishes: LevelSelectorShowRequested, QuitRequested
    /// </summary>
    class MainMenuController : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private Button _startGameButton;
        [SerializeField] private Button _quitGameButton;

        private void OnEnable()
        {
            var root = _uiDocument.rootVisualElement;
            _startGameButton = root.Q<Button>("BtnPlay");
            _startGameButton.clicked += OnStartGameButtonClicked;
            _quitGameButton = root.Q<Button>("BtnQuit");
            _quitGameButton.clicked += OnQuitButtonClicked;

            // No authentication in the non-UGS template: hide the sign-out button if present.
            var signOutButton = root.Q<Button>("BtnSignOut");
            if (signOutButton != null)
            {
                signOutButton.style.display = DisplayStyle.None;
            }
        }

        private void OnDisable()
        {
            if (_startGameButton != null) _startGameButton.clicked -= OnStartGameButtonClicked;
            if (_quitGameButton != null) _quitGameButton.clicked -= OnQuitButtonClicked;
        }

        private void OnQuitButtonClicked()
        {
            EventsPublisherGameFlow.Instance.PublishEvent(GameFlowEvents.QuitRequested, "Main Menu", null);
        }

        private void OnStartGameButtonClicked()
        {
            EventsPublisherGameFlow.Instance.PublishEvent(GameFlowEvents.LevelSelectorShowRequested, this, null);
        }
    }
}
