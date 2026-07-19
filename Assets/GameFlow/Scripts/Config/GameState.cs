using CrawfisSoftware.Events;
using CrawfisSoftware.GameFlow.Config;
using CrawfisSoftware.GameFlow.Events;

using UnityEngine;

namespace CrawfisSoftware.GameFlow
{
    public class GameState : MonoBehaviour
    {
        public static GameState Instance { get; private set; }

        public static bool IsMainMenuActive { get; set; } = false;
        public static bool IsGameStarted { get; set; } = false;
        public static bool IsGameOver { get; internal set; } = false;
        public static bool IsGamePaused { get; internal set; } = false;
        public static bool IsGameConfigured { get; internal set; } = false;
        public static bool IsLevelSelectorActive { get; set; } = false;
        public static LevelConfig SelectedLevel { get; set; }

        public  void Reset()
        {
            IsMainMenuActive = false;
            IsGameStarted = false;
            IsGameOver = false;
            IsGamePaused = false;
            IsGameConfigured = false;
            IsLevelSelectorActive = false;
            SelectedLevel = null;
        }
        private void Awake()
        {
            if (Instance != null)
            {
                DestroyImmediate(Instance);
            }
            Instance = this;
            Reset();

            EventsPublisherGameFlow.Instance.SubscribeToEvent(GameFlowEvents.MainMenuShowing, OnMainMenuShowing);
            EventsPublisherGameFlow.Instance.SubscribeToEvent(GameFlowEvents.MainMenuHidden, OnMainMenuHidden);
            EventsPublisherGameFlow.Instance.SubscribeToEvent(GameFlowEvents.GameStarted, OnGameStarted);
            EventsPublisherGameFlow.Instance.SubscribeToEvent(GameFlowEvents.GameEnding, OnGameOver);
            EventsPublisherGameFlow.Instance.SubscribeToEvent(GameFlowEvents.GameConfigApplied, OnGameConfigured);
            EventsPublisherGameFlow.Instance.SubscribeToEvent(GameFlowEvents.Paused, OnPause);
            EventsPublisherGameFlow.Instance.SubscribeToEvent(GameFlowEvents.Resumed, OnResume);
            EventsPublisherGameFlow.Instance.SubscribeToEvent(GameFlowEvents.LevelSelectorShowing, OnLevelSelectorShowing);
            EventsPublisherGameFlow.Instance.SubscribeToEvent(GameFlowEvents.LevelSelectorHidden, OnLevelSelectorHidden);
        }

        private void OnDestroy()
        {
            EventsPublisherGameFlow.Instance.UnsubscribeToEvent(GameFlowEvents.MainMenuShowing, OnMainMenuShowing);
            EventsPublisherGameFlow.Instance.UnsubscribeToEvent(GameFlowEvents.MainMenuHidden, OnMainMenuHidden);
            EventsPublisherGameFlow.Instance.UnsubscribeToEvent(GameFlowEvents.GameStarted, OnGameStarted);
            EventsPublisherGameFlow.Instance.UnsubscribeToEvent(GameFlowEvents.GameEnding, OnGameOver);
            EventsPublisherGameFlow.Instance.UnsubscribeToEvent(GameFlowEvents.GameConfigApplied, OnGameConfigured);
            EventsPublisherGameFlow.Instance.UnsubscribeToEvent(GameFlowEvents.Paused, OnPause);
            EventsPublisherGameFlow.Instance.UnsubscribeToEvent(GameFlowEvents.Resumed, OnResume);
            EventsPublisherGameFlow.Instance.UnsubscribeToEvent(GameFlowEvents.LevelSelectorShowing, OnLevelSelectorShowing);
            EventsPublisherGameFlow.Instance.UnsubscribeToEvent(GameFlowEvents.LevelSelectorHidden, OnLevelSelectorHidden);
        }

        private void OnMainMenuShowing(string eventName, object sender, object data)
        {
            IsMainMenuActive = true;
        }
        private void OnMainMenuHidden(string eventName, object sender, object data)
        {
            IsMainMenuActive = false;
        }
        private void OnGameStarted(string eventName, object sender, object data)
        {
            GameState.IsGameStarted = true;
            GameState.IsGameOver = false;
        }

        private void OnGameOver(string eventName, object sender, object data)
        {
            GameState.IsGameOver = true;
            GameState.IsGameStarted = false;
        }

        private void OnGameConfigured(string eventName, object sender, object data)
        {
            GameState.IsGameConfigured = true;
        }

        private void OnPause(string eventName, object sender, object data)
        {
            GameState.IsGamePaused = true;
        }

        private void OnResume(string eventName, object sender, object data)
        {
            GameState.IsGamePaused = false;
        }

        private void OnLevelSelectorShowing(string eventName, object sender, object data)
        {
            GameState.IsLevelSelectorActive = true;
        }

        private void OnLevelSelectorHidden(string eventName, object sender, object data)
        {
            GameState.IsLevelSelectorActive = false;
        }
    }
}