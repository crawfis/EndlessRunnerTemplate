using CrawfisSoftware.GameFlow.Events;

using UnityEngine;
using UnityEngine.UIElements;

namespace CrawfisSoftware.GameFlow.UI
{
    class MainMenuPanelController : MonoBehaviour
    {
        public UIDocument menuUI;
        private void Awake()
        {
            menuUI.rootVisualElement.visible = GameState.IsMainMenuActive;
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
            menuUI.rootVisualElement.visible = true;
            EventsPublisherGameFlow.Instance.PublishEvent(GameFlowEvents.MainMenuShown, this, null);
        }

        private void HidePanel()
        {
            menuUI.rootVisualElement.visible = false;
            EventsPublisherGameFlow.Instance.PublishEvent(GameFlowEvents.MainMenuHidden, this, null);
        }
    }
}