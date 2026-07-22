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
        private bool _initialized;

        private void Awake()
        {
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

        // Apply the initial visibility only after the panel's FIRST load, never in Awake.
        // Disabling a PanelRenderer in Awake is Unity bug UUM-146174: a later enable no longer
        // fires UIReloaded, so the tree never rebuilds and the panel stays blank until a manual
        // toggle. Letting it init enabled (so UIReloaded fires) then hiding here behaves like the
        // editor toggle, so subsequent show/hide via enabled repaints correctly.
        private void OnUIReload(PanelRenderer renderer, VisualElement root)
        {
            if (_initialized) return;
            _initialized = true;
            menuUI.enabled = GameState.IsMainMenuActive;
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
            menuUI.enabled = true;
            EventsPublisherGameFlow.Instance.PublishEvent(GameFlowEvents.MainMenuShown, this, null);
        }

        private void HidePanel()
        {
            menuUI.enabled = false;
            EventsPublisherGameFlow.Instance.PublishEvent(GameFlowEvents.MainMenuHidden, this, null);
        }
    }
}
