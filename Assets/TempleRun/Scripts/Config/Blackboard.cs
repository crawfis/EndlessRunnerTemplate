using CrawfisSoftware.Config;
using CrawfisSoftware.TempleRun.GameConfig;
using CrawfisSoftware.Utility;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// TempleRun-domain singleton holding gameplay state.
    ///    Dependencies: None
    ///    Subscribes: TempleRunEvents.TempleRunConfigApplied (bridged from GameFlow)
    ///    Subscribes: TempleRunEvents.TempleRunDifficultyChanging (bridged from GameFlow)
    ///    Subscribes: TempleRunEvents.TempleRunLevelApplied (bridged from GameFlow; data: int level number)
    /// </summary>
    public class Blackboard : MonoBehaviour
    {
        [SerializeField] private RandomProviderFromList _randomProvider;
        [SerializeField] private LaneConfig _laneConfig;
        [SerializeField] private JumpConfig _jumpConfig;
        [SerializeField] private SlideConfig _slideConfig;
        [SerializeField] private DashConfig _dashConfig;
        [SerializeField] private CoinConfig _coinConfig;

        private LaneChangeController _laneChangeController;

        public static Blackboard Instance { get; private set; }
        public System.Random MasterRandom { get { return _randomProvider.RandomGenerator; } }
        public DifficultyConfig GameConfig { get; 
            set; }  = new DifficultyConfig();
        public DistanceTracker DistanceTracker { get; set; } = new DistanceTracker();
        public float TrackWidthOffset { get; set; } = 1f;
        public float TileLength { get; set; } = 4f;
        public float CurrentSpeed { get; set; } = 0f;

        // ---------- Lane State ----------
        public LaneConfig LaneConfig { get => _laneConfig; set => _laneConfig = value; }
        public LaneChangeController LaneChangeController { get => _laneChangeController; set => _laneChangeController = value; }

        // ---------- Jump State ----------
        public JumpConfig JumpConfig { get => _jumpConfig; set => _jumpConfig = value; }
        public float JumpHeightOffset { get; set; } = 0f;    // Current Y offset during jump arc

        // ---------- Slide State ----------
        public SlideConfig SlideConfig { get => _slideConfig; set => _slideConfig = value; }
        public float SlideHeightOffset { get; set; } = 0f;   // Current Y offset during slide (negative when sliding)
        public float CurrentSlideMultiplier { get; set; } = 1.0f;  // Speed multiplier during slide (1.0 = normal speed)

        // ---------- Dash State ----------
        public DashConfig DashConfig { get => _dashConfig; set => _dashConfig = value; }
        public float CurrentDashMultiplier { get; set; } = 1.0f;  // Speed multiplier during dash (1.0 = normal speed)

        // ---------- Coin State ----------
        public CoinConfig CoinConfig { get => _coinConfig; set => _coinConfig = value; }
        public int SessionCoinCount { get; set; } = 0;

        // ---------- Track Level ----------
        // The selected level number (input). Persisted here because it arrives (bridged from
        // GameFlow) before the gameplay scene — and TrackManager — exists. TrackManager reads it
        // at init and resolves the track via TrackLibraryLoader; the resolved library is never
        // stored here.
        public int SelectedLevel { get; set; }

        // ---------- Power-Up / Buff State ----------
        public float ActiveSpeedMultiplier { get; set; } = 1.0f;    // Applied by SpeedBoost power-up
        public float ActiveScoreMultiplier { get; set; } = 1.0f;    // Applied by ScoreMultiplier power-up
        public bool CoinMagnetActive { get; set; } = false;         // Applied by CoinMagnet power-up
        public float CoinMagnetRadius { get; set; } = 0f;           // 0 = collider only, >0 = attraction radius
        public bool ShieldActive { get; set; } = false;             // Applied by Shield power-up

        private const float DEFAULT_TRACK_WIDTH_OFFSET = 1f;
        private const float DEFAULT_TILE_LENGTH = 4f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }
        private void Start()
        {
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeToEvents();
            ResetState();
        }

        /// <summary>
        /// Resets all gameplay state to initial defaults. Called on scene unload.
        /// </summary>
        private void ResetState()
        {
            DistanceTracker = new DistanceTracker();
            CurrentSpeed = 0f;
            JumpHeightOffset = 0f;
            SlideHeightOffset = 0f;
            CurrentSlideMultiplier = 1.0f;
            CurrentDashMultiplier = 1.0f;
            SessionCoinCount = 0;
            ActiveSpeedMultiplier = 1.0f;
            ActiveScoreMultiplier = 1.0f;
            CoinMagnetActive = false;
            CoinMagnetRadius = 0f;
            ShieldActive = false;
            SelectedLevel = 0;
        }

        private void OnConfigApplied(string eventName, object sender, object data)
        {
            DifficultyConfig difficulty = data as DifficultyConfig;
            if (difficulty != null)
            {
                GameConfig = difficulty;
                Debug.Log($"Blackboard: GameConfig set to '{difficulty.DifficultyName}'");
                EventsPublisherTempleRun.Instance.PublishEvent(TempleRunEvents.TempleRunDifficultyChanged, this, difficulty);
            }
        }

        private void OnLevelApplied(string eventName, object sender, object data)
        {
            SelectedLevel = (int)data;
            Debug.Log($"Blackboard: SelectedLevel = {SelectedLevel}");
        }

        private void OnGameEnded(string eventName, object sender, object data)
        {
            ResetState();
        }

        private void SubscribeToEvents()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.TempleRunEnded, OnGameEnded);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.TempleRunConfigApplied, OnConfigApplied);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.TempleRunDifficultyChanging, OnConfigApplied);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.TempleRunLevelApplied, OnLevelApplied);
        }

        private void UnsubscribeToEvents()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.TempleRunEnded, OnGameEnded);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.TempleRunConfigApplied, OnConfigApplied);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.TempleRunDifficultyChanging, OnConfigApplied);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.TempleRunLevelApplied, OnLevelApplied);
        }

#if UNITY_EDITOR
        [InitializeOnEnterPlayMode()]
        public static void InitializeOnPlay()
        {
            Instance = null;
        }
#endif
    }
}
