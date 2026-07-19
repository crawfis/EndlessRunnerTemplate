using CrawfisSoftware.GameFlow.Config;
using CrawfisSoftware.GameFlow.Events;

using UnityEngine;
using UnityEngine.UIElements;

namespace CrawfisSoftware.GameFlow.UI
{
    /// <summary>
    /// Handles level selector button logic. Creates level cards dynamically
    /// from a LevelRegistry, checks unlock status, and publishes selection events.
    ///    Dependencies: LevelRegistry, LevelProgressManager
    ///    Subscribes: GameFlowEvents.LevelSelectorShowing
    ///    Publishes: GameFlowEvents.LevelSelected (data: LevelConfig),
    ///               GameFlowEvents.MainMenuShowRequested (back button)
    /// </summary>
    class LevelSelectorController : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private LevelRegistry _levelRegistry;

        private VisualElement _levelContainer;
        private Button _backButton;

        private void OnEnable()
        {
            var root = _uiDocument.rootVisualElement;
            _levelContainer = root.Q<VisualElement>("LevelContainer");
            _backButton = root.Q<Button>("BtnBack");
            if (_backButton != null)
                _backButton.clicked += OnBackClicked;

            EventsPublisherGameFlow.Instance.SubscribeToEvent(
                GameFlowEvents.LevelSelectorShowing, OnShowing);
        }

        private void OnDestroy()
        {
            if (_backButton != null)
                _backButton.clicked -= OnBackClicked;

            EventsPublisherGameFlow.Instance.UnsubscribeToEvent(
                GameFlowEvents.LevelSelectorShowing, OnShowing);
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
