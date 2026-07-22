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
        private void Awake()
        {
            // enabled is a plain component property (no visual tree needed), safe in Awake.
            menuUI.enabled = GameState.IsMainMenuActive;
            EventsPublisherGameFlow.Instance.SubscribeToEvent(GameFlowEvents.GameplayNotReady, StartHidePanel);
            EventsPublisherGameFlow.Instance.SubscribeToEvent(GameFlowEvents.GameScenesLoading, StartHidePanel);
            EventsPublisherGameFlow.Instance.SubscribeToEvent(GameFlowEvents.LevelSelectorShowing, StartHidePanel);
            EventsPublisherGameFlow.Instance.SubscribeToEvent(GameFlowEvents.MainMenuShowing, StartShowPanel);
        }

        private void OnDestroy()
        {
            EventsPublisherGameFlow.Instance.UnsubscribeToEvent(GameFlowEvents.GameplayNotReady, StartHidePanel);
            EventsPublisherGameFlow.Instance.UnsubscribeToEvent(GameFlowEvents.GameScenesLoading, StartHidePanel);
            EventsPublisherGameFlow.Instance.UnsubscribeToEvent(GameFlowEvents.LevelSelectorShowing, StartHidePanel);
            EventsPublisherGameFlow.Instance.UnsubscribeToEvent(GameFlowEvents.MainMenuShowing, StartShowPanel);
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