using CrawfisSoftware.GameFlow.Config;
using CrawfisSoftware.GameFlow.Events;

using UnityEngine;
using UnityEngine.UIElements;

namespace CrawfisSoftware.GameFlow.UI
{
    /// <summary>
    /// Handles level selector button logic. Creates level cards dynamically
    /// from a LevelRegistry, checks unlock status, and publishes selection events.
    ///    Dependencies: PanelRenderer (level selector panel), LevelRegistry, LevelProgressManager
    ///    Subscribes: GameFlowEvents.LevelSelectorShowing
    ///    Publishes: GameFlowEvents.LevelSelected (data: LevelConfig),
    ///               GameFlowEvents.MainMenuShowRequested (back button)
    /// </summary>
    class LevelSelectorController : MonoBehaviour
    {
        [SerializeField] private PanelRenderer _panel;
        [SerializeField] private LevelRegistry _levelRegistry;

        private VisualElement _root;
        private VisualElement _levelContainer;
        private Button _backButton;

        private void OnEnable()
        {
            _panel.RegisterUIReloadCallback(OnUIReload);

            EventsPublisherGameFlow.Instance.SubscribeToEvent(
                GameFlowEvents.LevelSelectorShowing, OnShowing);
        }

        private void OnDisable()
        {
            _panel.UnregisterUIReloadCallback(OnUIReload);
        }

        private void OnDestroy()
        {
            if (_backButton != null)
                _backButton.clicked -= OnBackClicked;

            EventsPublisherGameFlow.Instance.UnsubscribeToEvent(
                GameFlowEvents.LevelSelectorShowing, OnShowing);
        }

        // The PanelRenderer surfaces its visual tree only through this callback (it has no
        // root-tree property). It can fire again on LiveReload, so wiring is idempotent.
        // We (re)populate here as well so cards exist even if the tree arrives after a
        // LevelSelectorShowing event (callback-timing safety).
        private void OnUIReload(PanelRenderer renderer, VisualElement root)
        {
            _root = root;
            _levelContainer = root.Q<VisualElement>("LevelContainer");

            if (_backButton != null)
                _backButton.clicked -= OnBackClicked;
            _backButton = root.Q<Button>("BtnBack");
            if (_backButton != null)
                _backButton.clicked += OnBackClicked;

            PopulateLevels();
        }

        private void OnShowing(string eventName, object sender, object data)
        {
            PopulateLevels();
        }

        private void PopulateLevels()
        {
            if (_levelContainer == null || _levelRegistry == null) return;

            _levelContainer.Clear();
            var sortedLevels = _levelRegistry.GetSortedLevels();
            foreach (var level in sortedLevels)
            {
                bool unlocked = LevelProgressManager.Instance != null
                    ? LevelProgressManager.Instance.IsLevelUnlocked(level)
                    : level.UnlockScoreThreshold <= 0f;

                float bestScore = LevelProgressManager.Instance != null
                    ? LevelProgressManager.Instance.ProgressData.GetBestScore(level.LevelName)
                    : 0f;

                var card = CreateLevelCard(level, unlocked, bestScore);
                _levelContainer.Add(card);
            }
        }

        private VisualElement CreateLevelCard(LevelConfig level, bool unlocked, float bestScore)
        {
            var card = new VisualElement();
            card.AddToClassList("level-card");
            if (!unlocked)
                card.AddToClassList("level-card--locked");

            // Title
            var title = new Label(level.LevelName);
            title.AddToClassList("level-card__title");
            card.Add(title);

            // Description
            if (!string.IsNullOrEmpty(level.Description))
            {
                var description = new Label(level.Description);
                description.AddToClassList("level-card__description");
                card.Add(description);
            }

            if (unlocked)
            {
                // Best score
                if (bestScore > 0)
                {
                    var scoreLabel = new Label($"Best: {bestScore:F0}");
                    scoreLabel.AddToClassList("level-card__score");
                    card.Add(scoreLabel);
                }

                // Play button
                var playBtn = new Button(() => OnLevelSelected(level));
                playBtn.text = "Play";
                playBtn.AddToClassList("level-card__play-btn");
                card.Add(playBtn);
            }
            else
            {
                // Lock info
                var lockLabel = new Label(level.UnlockRequirementDescription);
                lockLabel.AddToClassList("level-card__lock-info");
                card.Add(lockLabel);
            }

            return card;
        }

        private void OnLevelSelected(LevelConfig level)
        {
            GameState.SelectedLevel = level;
            EventsPublisherGameFlow.Instance.PublishEvent(
                GameFlowEvents.LevelSelected, this, level);
        }

        private void OnBackClicked()
        {
            EventsPublisherGameFlow.Instance.PublishEvent(
                GameFlowEvents.MainMenuShowRequested, this, null);
        }
    }
}
