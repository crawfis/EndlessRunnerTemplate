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

        private VisualElement _gameOverRoot;
        private VisualElement _loadingRoot;
        private bool _gameOverVisible;
        private bool _loadingVisible;

        void Awake()
        {
            // Loading is shown immediately; gameOver starts hidden. Visibility is applied to each
            // panel's root style.display once its UIReload callback delivers the tree. The
            // PanelRenderers stay enabled at all times (we never toggle enabled - see the
            // UUM-146174 note in MainMenuPanelController).
            _loadingVisible = true;
            _gameOverVisible = false;

            EventsPublisherGameFlow.Instance.SubscribeToEvent(GameFlowEvents.GameStarting, OnGameStarting);
            EventsPublisherGameFlow.Instance.SubscribeToEvent(GameFlowEvents.GameStarted, OnGameStarted);
            EventsPublisherGameFlow.Instance.SubscribeToEvent(GameFlowEvents.GameEnding, OnGameEnding);

            StartCoroutine(ShowLoadingRoutine(GameConstants.DefaultLoadingDisplayTime));
        }

        private void OnEnable()
        {
            // Visibility is driven by style.display, so both PanelRenderers must stay enabled for
            // their trees to build. The scene may author gameOver (or loading) disabled, so force
            // them on here - after registering, to catch the resulting reload.
            if (loadingUI)
            {
                loadingUI.RegisterUIReloadCallback(OnLoadingUIReload);
                loadingUI.enabled = true;
            }
            if (gameOverUI)
            {
                gameOverUI.RegisterUIReloadCallback(OnGameOverUIReload);
                gameOverUI.enabled = true;
            }
        }

        private void OnDisable()
        {
            if (loadingUI) loadingUI.UnregisterUIReloadCallback(OnLoadingUIReload);
            if (gameOverUI) gameOverUI.UnregisterUIReloadCallback(OnGameOverUIReload);
        }

        private void OnLoadingUIReload(PanelRenderer renderer, VisualElement root)
        {
            _loadingRoot = root;
            ApplyLoading();
        }

        private void OnGameOverUIReload(PanelRenderer renderer, VisualElement root)
        {
            _gameOverRoot = root;
            ApplyGameOver();
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

        public void Go(UIState s)
        {
            SetGameOverVisible(s == UIState.GameOverOverlay);
            SetLoadingVisible(s == UIState.Loading);
        }

        public void ShowGameOver()
        {
            if (!gameOverUI) { Debug.LogWarning("GameOver UXML not set"); return; }
            SetGameOverVisible(true);
            StartCoroutine(ShowGameOverRoutine());
        }

        private IEnumerator ShowGameOverRoutine()
        {
            yield return new WaitForSecondsRealtime(GameConstants.GameOverDisplayTime);
            EventsPublisherGameFlow.Instance.PublishEvent(GameFlowEvents.GameEnded, this, null);
            Go(UIState.None);
        }

        private void SetGameOverVisible(bool on)
        {
            _gameOverVisible = on;
            ApplyGameOver();
        }

        private void SetLoadingVisible(bool on)
        {
            _loadingVisible = on;
            ApplyLoading();
        }

        private void ApplyGameOver()
        {
            if (_gameOverRoot != null)
                _gameOverRoot.style.display = _gameOverVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ApplyLoading()
        {
            if (_loadingRoot != null)
                _loadingRoot.style.display = _loadingVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public enum UIState { None, Menu, Loading, GameOverOverlay }
    }
}
