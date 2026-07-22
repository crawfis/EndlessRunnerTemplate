using CrawfisSoftware.GameFlow.Events;

using UnityEngine;
using UnityEngine.UIElements;

namespace CrawfisSoftware.GameFlow.UI
{
    /// <summary>
    /// Shows/hides the Level Selector PanelRenderer based on GameFlow events.
    /// Follows the same pattern as MainMenuPanelController.
    ///    Dependencies: PanelRenderer (level selector panel)
    ///    Subscribes: GameFlowEvents.LevelSelectorShowing (show),
    ///                GameFlowEvents.GameScenesLoading (hide - game starting),
    ///                GameFlowEvents.MainMenuShowing (hide - back to menu)
    ///    Publishes: GameFlowEvents.LevelSelectorShown, GameFlowEvents.LevelSelectorHidden
    /// </summary>
    class LevelSelectorPanelController : MonoBehaviour
    {
        [SerializeField] private PanelRenderer _levelSelectorUI;
        private bool _initialized;

        private void Awake()
        {
            EventsPublisherGameFlow.Instance.SubscribeToEvent(
                GameFlowEvents.LevelSelectorShowing, StartShowPanel);
            EventsPublisherGameFlow.Instance.SubscribeToEvent(
                GameFlowEvents.GameScenesLoading, StartHidePanel);
            EventsPublisherGameFlow.Instance.SubscribeToEvent(
                GameFlowEvents.MainMenuShowing, StartHidePanel);
        }

        private void OnEnable() => _levelSelectorUI.RegisterUIReloadCallback(OnUIReload);

        private void OnDisable() => _levelSelectorUI.UnregisterUIReloadCallback(OnUIReload);

        // Hide only after the panel's FIRST load, never in Awake. Disabling a PanelRenderer in
        // Awake is Unity bug UUM-146174: a later enable no longer fires UIReloaded and the panel
        // stays blank until a manual toggle. Letting it init enabled then hiding here makes the
        // subsequent show (enabled = true) repaint correctly.
        private void OnUIReload(PanelRenderer renderer, VisualElement root)
        {
            if (_initialized) return;
            _initialized = true;
            _levelSelectorUI.enabled = false;
        }

        private void OnDestroy()
        {
            EventsPublisherGameFlow.Instance.UnsubscribeToEvent(
                GameFlowEvents.LevelSelectorShowing, StartShowPanel);
            EventsPublisherGameFlow.Instance.UnsubscribeToEvent(
                GameFlowEvents.GameScenesLoading, StartHidePanel);
            EventsPublisherGameFlow.Instance.UnsubscribeToEvent(
                GameFlowEvents.MainMenuShowing, StartHidePanel);
        }

        private void StartShowPanel(string eventName, object sender, object data)
        {
            ShowPanel();
        }

        private void StartHidePanel(string eventName, object sender, object data)
        {
            HidePanel();
        }

        private void ShowPanel()
        {
            _levelSelectorUI.enabled = true;
            EventsPublisherGameFlow.Instance.PublishEvent(
                GameFlowEvents.LevelSelectorShown, this, null);
        }

        private void HidePanel()
        {
            if (!_levelSelectorUI.enabled) return;
            _levelSelectorUI.enabled = false;
            EventsPublisherGameFlow.Instance.PublishEvent(
                GameFlowEvents.LevelSelectorHidden, this, null);
        }
    }
}
