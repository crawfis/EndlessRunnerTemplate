using CrawfisSoftware.GameFlow.Events;

using UnityEngine;

namespace CrawfisSoftware.GameFlow.Config
{
    /// <summary>
    /// Manages level progress persistence and unlock checks.
    /// Saves best scores per level and unlocks levels based on cumulative score thresholds.
    ///    Dependencies: LevelRegistry (ScriptableObject via SerializeField)
    ///    Subscribes: GameFlowEvents.GameEnding
    ///    Publishes: GameFlowEvents.LevelUnlocked, GameFlowEvents.LevelProgressSaved
    /// </summary>
    public class LevelProgressManager : MonoBehaviour
    {
        [SerializeField] private LevelRegistry _levelRegistry;

        private LevelProgressData _progressData;
        private const string PROGRESS_KEY = "LevelProgress";

        public static LevelProgressManager Instance { get; private set; }

        public LevelProgressData ProgressData => _progressData;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadProgress();
            InitializeDefaultUnlocks();

            EventsPublisherGameFlow.Instance.SubscribeToEvent(
                GameFlowEvents.GameEnding, OnGameEnding);
        }

        private void OnDestroy()
        {
            EventsPublisherGameFlow.Instance.UnsubscribeToEvent(
                GameFlowEvents.GameEnding, OnGameEnding);

            if (Instance == this)
                Instance = null;
        }

        private void LoadProgress()
        {
            string json = PlayerPrefs.GetString(PROGRESS_KEY, "");
            _progressData = string.IsNullOrEmpty(json)
                ? new LevelProgressData()
                : JsonUtility.FromJson<LevelProgressData>(json);
        }

        private void SaveProgress()
        {
            string json = JsonUtility.ToJson(_progressData);
            PlayerPrefs.SetString(PROGRESS_KEY, json);
            PlayerPrefs.Save();
            EventsPublisherGameFlow.Instance.PublishEvent(
                GameFlowEvents.LevelProgressSaved, this, null);
        }

        private void InitializeDefaultUnlocks()
        {
            if (_levelRegistry == null || _levelRegistry.Levels == null) return;
            foreach (var level in _levelRegistry.Levels)
            {
                if (level.UnlockScoreThreshold <= 0f)
                    _progressData.UnlockLevel(level.LevelName);
            }
        }

        /// <summary>
        /// Checks if a level is unlocked based on progress or threshold.
        /// </summary>
        public bool IsLevelUnlocked(LevelConfig level)
        {
            if (level.UnlockScoreThreshold <= 0f) return true;
            return _progressData.IsLevelUnlocked(level.LevelName);
        }

        private void OnGameEnding(string eventName, object sender, object data)
        {
            float score = data is float s ? s : 0f;
            var selectedLevel = GameState.SelectedLevel;
            if (selectedLevel == null) return;

            _progressData.SetBestScore(selectedLevel.LevelName, score);
            CheckAndUnlockLevels();
            SaveProgress();
        }

        private void CheckAndUnlockLevels()
        {
            if (_levelRegistry == null || _levelRegistry.Levels == null) return;

            float totalBestScore = GetTotalBestScore();
            foreach (var level in _levelRegistry.Levels)
            {
                if (IsLevelUnlocked(level)) continue;
                if (totalBestScore >= level.UnlockScoreThreshold)
                {
                    _progressData.UnlockLevel(level.LevelName);
                    EventsPublisherGameFlow.Instance.PublishEvent(
                        GameFlowEvents.LevelUnlocked, this, level);
                }
            }
        }

        private float GetTotalBestScore()
        {
            float total = 0f;
            foreach (var entry in _progressData.LevelScores)
                total += entry.BestScore;
            return total;
        }
    }
}
