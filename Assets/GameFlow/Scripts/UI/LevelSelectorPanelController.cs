using CrawfisSoftware.GameFlow.Events;

using UnityEngine;
using UnityEngine.UIElements;

namespace CrawfisSoftware.GameFlow.UI
{
    /// <summary>
    /// Shows/hides the Level Selector UIDocument panel based on GameFlow events.
    /// Follows the same pattern as MainMenuPanelController.
    ///    Subscribes: GameFlowEvents.LevelSelectorShowing (show),
    ///                GameFlowEvents.GameScenesLoading (hide - game starting),
    ///                GameFlowEvents.MainMenuShowing (hide - back to menu)
    ///    Publishes: GameFlowEvents.LevelSelectorShown, GameFlowEvents.LevelSelectorHidden
    /// </summary>
    class LevelSelectorPanelController : MonoBehaviour
    {
        [SerializeField] private UIDocument _levelSelectorUI;

        private void Awake()
        {
            _levelSelectorUI.rootVisualElement.visible = false;

            EventsPublisherGameFlow.Instance.SubscribeToEvent(
                GameFlowEvents.LevelSelectorShowing, StartShowPanel);
            EventsPublisherGameFlow.Instance.SubscribeToEvent(
                GameFlowEvents.GameScenesLoading, StartHidePanel);
            EventsPublisherGameFlow.Instance.SubscribeToEvent(
                GameFlowEvents.MainMenuShowing, StartHidePanel);
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
            _levelSelectorUI.rootVisualElement.visible = true;
            EventsPublisherGameFlow.Instance.PublishEvent(
                GameFlowEvents.LevelSelectorShown, this, null);
        }

        private void HidePanel()
        {
            if (!_levelSelectorUI.rootVisualElement.visible) return;
            _levelSelectorUI.rootVisualElement.visible = false;
            EventsPublisherGameFlow.Instance.PublishEvent(
                GameFlowEvents.LevelSelectorHidden, this, null);
        }
    }
}
