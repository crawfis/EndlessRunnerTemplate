using CrawfisSoftware.Events;
using CrawfisSoftware.GameFlow.Config;
using CrawfisSoftware.GameFlow.Events;

using UnityEngine;
using GameFlowBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.GameFlow.Events.GameFlowEvents>;

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

            GameFlowBus.Subscribe(GameFlowEvents.MainMenuShowing, OnMainMenuShowing);
            GameFlowBus.Subscribe(GameFlowEvents.MainMenuHidden, OnMainMenuHidden);
            GameFlowBus.Subscribe(GameFlowEvents.GameStarted, OnGameStarted);
            GameFlowBus.Subscribe(GameFlowEvents.GameEnding, OnGameOver);
            GameFlowBus.Subscribe(GameFlowEvents.GameConfigApplied, OnGameConfigured);
            GameFlowBus.Subscribe(GameFlowEvents.Paused, OnPause);
            GameFlowBus.Subscribe(GameFlowEvents.Resumed, OnResume);
            GameFlowBus.Subscribe(GameFlowEvents.LevelSelectorShowing, OnLevelSelectorShowing);
            GameFlowBus.Subscribe(GameFlowEvents.LevelSelectorHidden, OnLevelSelectorHidden);
        }

        private void OnDestroy()
        {
            GameFlowBus.Unsubscribe(GameFlowEvents.MainMenuShowing, OnMainMenuShowing);
            GameFlowBus.Unsubscribe(GameFlowEvents.MainMenuHidden, OnMainMenuHidden);
            GameFlowBus.Unsubscribe(GameFlowEvents.GameStarted, OnGameStarted);
            GameFlowBus.Unsubscribe(GameFlowEvents.GameEnding, OnGameOver);
            GameFlowBus.Unsubscribe(GameFlowEvents.GameConfigApplied, OnGameConfigured);
            GameFlowBus.Unsubscribe(GameFlowEvents.Paused, OnPause);
            GameFlowBus.Unsubscribe(GameFlowEvents.Resumed, OnResume);
            GameFlowBus.Unsubscribe(GameFlowEvents.LevelSelectorShowing, OnLevelSelectorShowing);
            GameFlowBus.Unsubscribe(GameFlowEvents.LevelSelectorHidden, OnLevelSelectorHidden);
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