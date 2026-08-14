using CrawfisSoftware.GameFlow.Events;

using UnityEngine;
using UnityEngine.UIElements;
using GameFlowBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.GameFlow.Events.GameFlowEvents>;

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
            GameFlowBus.Subscribe(GameFlowEvents.GameplayNotReady, StartHidePanel);
            GameFlowBus.Subscribe(GameFlowEvents.GameScenesLoading, StartHidePanel);
            GameFlowBus.Subscribe(GameFlowEvents.LevelSelectorShowing, StartHidePanel);
            GameFlowBus.Subscribe(GameFlowEvents.MainMenuShowing, StartShowPanel);
        }

        private void OnEnable()
        {
            menuUI.RegisterUIReloadCallback(OnUIReload);
            // Visibility is driven by style.display, so the PanelRenderer must stay enabled for its
            // tree to build and fire UIReload. The scene may author it disabled (panels that "start
            // hidden"), so force it on here - after registering, to catch the resulting reload.
            menuUI.enabled = true;
        }

        private void OnDisable() => menuUI.UnregisterUIReloadCallback(OnUIReload);

        private void OnDestroy()
        {
            GameFlowBus.Unsubscribe(GameFlowEvents.GameplayNotReady, StartHidePanel);
            GameFlowBus.Unsubscribe(GameFlowEvents.GameScenesLoading, StartHidePanel);
            GameFlowBus.Unsubscribe(GameFlowEvents.LevelSelectorShowing, StartHidePanel);
            GameFlowBus.Unsubscribe(GameFlowEvents.MainMenuShowing, StartShowPanel);
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
            GameFlowBus.Publish(GameFlowEvents.MainMenuShown, this, null);
        }

        private void HidePanel()
        {
            _visible = false;
            ApplyVisibility();
            GameFlowBus.Publish(GameFlowEvents.MainMenuHidden, this, null);
        }

        private void ApplyVisibility()
        {
            if (_root != null)
                _root.style.display = _visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
