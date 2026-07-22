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

        private VisualElement _root;
        private bool _visible;

        private void Awake()
        {
            _visible = false;

            EventsPublisherGameFlow.Instance.SubscribeToEvent(
                GameFlowEvents.LevelSelectorShowing, StartShowPanel);
            EventsPublisherGameFlow.Instance.SubscribeToEvent(
                GameFlowEvents.GameScenesLoading, StartHidePanel);
            EventsPublisherGameFlow.Instance.SubscribeToEvent(
                GameFlowEvents.MainMenuShowing, StartHidePanel);
        }

        private void OnEnable() => _levelSelectorUI.RegisterUIReloadCallback(OnUIReload);

        private void OnDisable() => _levelSelectorUI.UnregisterUIReloadCallback(OnUIReload);

        private void OnDestroy()
        {
            EventsPublisherGameFlow.Instance.UnsubscribeToEvent(
                GameFlowEvents.LevelSelectorShowing, StartShowPanel);
            EventsPublisherGameFlow.Instance.UnsubscribeToEvent(
                GameFlowEvents.GameScenesLoading, StartHidePanel);
            EventsPublisherGameFlow.Instance.UnsubscribeToEvent(
                GameFlowEvents.MainMenuShowing, StartHidePanel);
        }

        // Show/hide via the root's style.display; the PanelRenderer stays enabled at all times.
        // See the UUM-146174 note in MainMenuPanelController for why we avoid toggling enabled.
        private void OnUIReload(PanelRenderer renderer, VisualElement root)
        {
            _root = root;
            ApplyVisibility();
        }

        private void StartShowPanel(string eventName, object sender, object data) => ShowPanel();

        private void StartHidePanel(string eventName, object sender, object data) => HidePanel();

        private void ShowPanel()
        {
            _visible = true;
            ApplyVisibility();
            EventsPublisherGameFlow.Instance.PublishEvent(
                GameFlowEvents.LevelSelectorShown, this, null);
        }

        private void HidePanel()
        {
            if (!_visible) return;
            _visible = false;
            ApplyVisibility();
            EventsPublisherGameFlow.Instance.PublishEvent(
                GameFlowEvents.LevelSelectorHidden, this, null);
        }

        private void ApplyVisibility()
        {
            if (_root != null)
                _root.style.display = _visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
