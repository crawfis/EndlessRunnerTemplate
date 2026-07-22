using CrawfisSoftware.GameFlow.Events;

using UnityEngine;
using UnityEngine.UIElements;

namespace CrawfisSoftware.GameFlow.UI
{
    /// <summary>
    /// Shows/hides the Main Menu PanelRenderer based on GameFlow events.
    ///    Dependencies: PanelRenderer (main menu panel)
    ///    Subscribes: GameplayNotReady, GameScenesLoading, LevelSelectorShowing (hide),
    ///                MainMenuShowing (show)
    ///    Publishes: MainMenuShown, MainMenuHidden
    /// </summary>
    class MainMenuPanelController : MonoBehaviour
    {
        public PanelRenderer menuUI;

        private VisualElement _root;
        private bool _visible;

        private void Awake()
        {
            _visible = GameState.IsMainMenuActive;
            EventsPublisherGameFlow.Instance.SubscribeToEvent(GameFlowEvents.GameplayNotReady, StartHidePanel);
            EventsPublisherGameFlow.Instance.SubscribeToEvent(GameFlowEvents.GameScenesLoading, StartHidePanel);
            EventsPublisherGameFlow.Instance.SubscribeToEvent(GameFlowEvents.LevelSelectorShowing, StartHidePanel);
            EventsPublisherGameFlow.Instance.SubscribeToEvent(GameFlowEvents.MainMenuShowing, StartShowPanel);
        }

        private void OnEnable() => menuUI.RegisterUIReloadCallback(OnUIReload);

        private void OnDisable() => menuUI.UnregisterUIReloadCallback(OnUIReload);

        private void OnDestroy()
        {
            EventsPublisherGameFlow.Instance.UnsubscribeToEvent(GameFlowEvents.GameplayNotReady, StartHidePanel);
            EventsPublisherGameFlow.Instance.UnsubscribeToEvent(GameFlowEvents.GameScenesLoading, StartHidePanel);
            EventsPublisherGameFlow.Instance.UnsubscribeToEvent(GameFlowEvents.LevelSelectorShowing, StartHidePanel);
            EventsPublisherGameFlow.Instance.UnsubscribeToEvent(GameFlowEvents.MainMenuShowing, StartShowPanel);
        }

        // Show/hide is driven by the root's style.display while the PanelRenderer stays ENABLED at
        // all times. We deliberately do NOT toggle PanelRenderer.enabled: disabling tears the visual
        // tree down, and Unity bug UUM-146174 means a later enable may not re-fire UIReloaded (blank
        // panel). Re-applying our own _visible state on every reload also avoids any race with when
        // the tree first arrives relative to a show/hide event.
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
            EventsPublisherGameFlow.Instance.PublishEvent(GameFlowEvents.MainMenuShown, this, null);
        }

        private void HidePanel()
        {
            _visible = false;
            ApplyVisibility();
            EventsPublisherGameFlow.Instance.PublishEvent(GameFlowEvents.MainMenuHidden, this, null);
        }

        private void ApplyVisibility()
        {
            if (_root != null)
                _root.style.display = _visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
