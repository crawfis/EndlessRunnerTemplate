using CrawfisSoftware.GameFlow.Events;

using UnityEngine;
using UnityEngine.UIElements;

namespace CrawfisSoftware.GameFlow.UI
{
    /// <summary>
    /// Wires the main menu buttons to GameFlow events.
    ///    Dependencies: PanelRenderer (main menu panel)
    ///    Subscribes: none
    ///    Publishes: LevelSelectorShowRequested, QuitRequested
    /// </summary>
    class MainMenuController : MonoBehaviour
    {
        [SerializeField] private PanelRenderer _panel;
        [SerializeField] private Button _startGameButton;
        [SerializeField] private Button _quitGameButton;

        private VisualElement _root;

        private void OnEnable()
        {
            _panel.RegisterUIReloadCallback(OnUIReload);
        }

        private void OnDisable()
        {
            _panel.UnregisterUIReloadCallback(OnUIReload);
            if (_startGameButton != null) _startGameButton.clicked -= OnStartGameButtonClicked;
            if (_quitGameButton != null) _quitGameButton.clicked -= OnQuitButtonClicked;
        }

        // The PanelRenderer surfaces its visual tree only through this callback (there is no
        // rootVisualElement). It can fire again on LiveReload, so wiring is idempotent:
        // unhook before re-hooking.
        private void OnUIReload(PanelRenderer renderer, VisualElement root)
        {
            _root = root;

            if (_startGameButton != null) _startGameButton.clicked -= OnStartGameButtonClicked;
            _startGameButton = root.Q<Button>("BtnPlay");
            if (_startGameButton != null) _startGameButton.clicked += OnStartGameButtonClicked;

            if (_quitGameButton != null) _quitGameButton.clicked -= OnQuitButtonClicked;
            _quitGameButton = root.Q<Button>("BtnQuit");
            if (_quitGameButton != null) _quitGameButton.clicked += OnQuitButtonClicked;

            // No authentication in the non-UGS template: hide the sign-out button if present.
            var signOutButton = root.Q<Button>("BtnSignOut");
            if (signOutButton != null)
            {
                signOutButton.style.display = DisplayStyle.None;
            }
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
