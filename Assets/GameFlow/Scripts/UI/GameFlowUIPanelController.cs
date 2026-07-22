using CrawfisSoftware.Events;
using CrawfisSoftware.GameFlow.Events;
using CrawfisSoftware.GameFlow.GameConfig;

using System.Collections;

using UnityEngine;
using UnityEngine.UIElements;

namespace CrawfisSoftware.GameFlow.UI
{
    /// <summary>
    /// GameFlow-domain UI panel controller. Manages loading screen and game over overlay.
    /// Countdown and HUD are now managed by TempleRun-domain CountdownUIController.
    ///    Dependencies: PanelRenderer (gameOver + loading panels), GameConstants
    ///    Subscribes: GameFlowEvents.GameStarting, GameFlowEvents.GameStarted, GameFlowEvents.GameEnding
    ///    Publishes: GameFlowEvents.LoadingScreenShown, GameFlowEvents.LoadingScreenHidden, GameFlowEvents.GameEnded
    /// </summary>
    public class GameFlowUIPanelController : MonoBehaviour
    {
        [Header("Panels (drag from scene)")]
        public PanelRenderer gameOverUI;
        public PanelRenderer loadingUI;

        private bool _isSignedIn = true;

        void Awake()
        {
            if (gameOverUI) gameOverUI.enabled = false;

            Go(UIState.Loading);

            EventsPublisherGameFlow.Instance.SubscribeToEvent(GameFlowEvents.GameStarting, OnGameStarting);
            EventsPublisherGameFlow.Instance.SubscribeToEvent(GameFlowEvents.GameStarted, OnGameStarted);
            EventsPublisherGameFlow.Instance.SubscribeToEvent(GameFlowEvents.GameEnding, OnGameEnding);

            StartCoroutine(ShowLoadingRoutine(GameConstants.DefaultLoadingDisplayTime));
        }

        private void OnDestroy()
        {
            EventsPublisherGameFlow.Instance.UnsubscribeToEvent(GameFlowEvents.GameStarting, OnGameStarting);
            EventsPublisherGameFlow.Instance.UnsubscribeToEvent(GameFlowEvents.GameStarted, OnGameStarted);
            EventsPublisherGameFlow.Instance.UnsubscribeToEvent(GameFlowEvents.GameEnding, OnGameEnding);
        }

        private void OnGameStarting(string eventName, object sender, object data)
        {
            // Hide loading; countdown UI is now managed by TempleRun CountdownUIController
            Go(UIState.None);
        }

        private void OnGameStarted(string eventName, object sender, object data)
        {
            Go(UIState.None);
        }

        private void OnGameEnding(string eventName, object sender, object data)
        {
            ShowGameOver();
        }

        private IEnumerator ShowLoadingRoutine(float seconds)
        {
            EventsPublisherGameFlow.Instance.PublishEvent(GameFlowEvents.LoadingScreenShown, this, null);
            yield return new WaitForSecondsRealtime(seconds);
            if (_isSignedIn)
                Go(UIState.Menu);
            else
                Go(UIState.None);
            EventsPublisherGameFlow.Instance.PublishEvent(GameFlowEvents.LoadingScreenHidden, this, null);
        }

        void SetActive(PanelRenderer panel, bool on)
        {
            if (!panel) return;
            // PanelRenderer preserves its content when disabled, so enabled is the show/hide
            // toggle. No gameObject.SetActive / style.display needed.
            panel.enabled = on;
        }

        public void Go(UIState s)
        {
            SetActive(gameOverUI, s == UIState.GameOverOverlay);
            SetActive(loadingUI, s == UIState.Loading);
        }

        public void ShowGameOver()
        {
            if (!gameOverUI) { Debug.LogWarning("GameOver UXML not set"); return; }
            SetActive(gameOverUI, true);
            StartCoroutine(ShowGameOverRoutine());
        }

        private IEnumerator ShowGameOverRoutine()
        {
            yield return new WaitForSecondsRealtime(GameConstants.GameOverDisplayTime);
            EventsPublisherGameFlow.Instance.PublishEvent(GameFlowEvents.GameEnded, this, null);
            Go(UIState.None);
        }

        public enum UIState { None, Menu, Loading, GameOverOverlay }
    }
}
