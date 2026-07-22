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
        private bool _gameOverInitialized;

        void Awake()
        {
            // Show loading immediately. Do NOT disable gameOverUI here: disabling a PanelRenderer
            // in Awake is Unity bug UUM-146174 (a later enable stops firing UIReloaded, leaving the
            // panel blank until a manual toggle). gameOverUI is hidden in its first UIReload instead
            // (its brief on-load frame is masked by loadingUI, which has a higher sort order).
            if (loadingUI) loadingUI.enabled = true;

            EventsPublisherGameFlow.Instance.SubscribeToEvent(GameFlowEvents.GameStarting, OnGameStarting);
            EventsPublisherGameFlow.Instance.SubscribeToEvent(GameFlowEvents.GameStarted, OnGameStarted);
            EventsPublisherGameFlow.Instance.SubscribeToEvent(GameFlowEvents.GameEnding, OnGameEnding);

            StartCoroutine(ShowLoadingRoutine(GameConstants.DefaultLoadingDisplayTime));
        }

        private void OnEnable()
        {
            if (gameOverUI) gameOverUI.RegisterUIReloadCallback(OnGameOverUIReload);
        }

        private void OnDisable()
        {
            if (gameOverUI) gameOverUI.UnregisterUIReloadCallback(OnGameOverUIReload);
        }

        // Hide gameOverUI only after its first load, never in Awake (see UUM-146174 above).
        private void OnGameOverUIReload(PanelRenderer renderer, VisualElement root)
        {
            if (_gameOverInitialized) return;
            _gameOverInitialized = true;
            gameOverUI.enabled = false;
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
            // enabled is the show/hide toggle: disabling tears the visual tree down, enabling
            // rebuilds it (and re-fires UIReloaded). Safe here because each panel completed its
            // first init enabled before we ever disable it (see UUM-146174 note in Awake).
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
