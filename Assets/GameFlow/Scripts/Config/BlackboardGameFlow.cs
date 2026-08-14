//using CrawfisSoftware.Config;

//using UnityEngine;
//using CrawfisSoftware.GameFlow.Events;
//using GameFlowBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.GameFlow.Events.GameFlowEvents>;


//#if UNITY_EDITOR
//using UnityEditor;
//#endif

//namespace CrawfisSoftware.GameFlow
//{
//    /// <summary>
//    /// GameFlow-domain singleton holding session-level configuration.
//    /// Subscribes to GameFlowEvents.DifficultyChanged and publishes
//    /// GameFlowEvents.GameConfigApplying / GameConfigApplied.
//    ///    Dependencies: None
//    ///    Subscribes: GameFlowEvents.DifficultyChanged
//    ///    Publishes: GameFlowEvents.GameConfigApplying, GameFlowEvents.GameConfigApplied
//    /// </summary>
//    public class BlackboardGameFlow : MonoBehaviour
//    {
//        public static BlackboardGameFlow Instance { get; private set; }

//        public DifficultyConfig GameConfig { get; set; } = new DifficultyConfig();

//        private void Awake()
//        {
//            if (Instance != null && Instance != this)
//            {
//                Destroy(this);
//                return;
//            }
//            Instance = this;
//        }

//        private void Start()
//        {
//            SubscribeToEvents();
//        }

//        private void OnDestroy()
//        {
//            UnsubscribeToEvents();
//        }

//        private void OnGameDifficultyChanged(string eventName, object sender, object data)
//        {
//            DifficultyConfig difficulty = data as DifficultyConfig;
//            if (difficulty != null)
//            {
//                GameFlowBus.Publish(GameFlowEvents.GameConfigApplying, this, difficulty);
//                GameConfig = difficulty;
//                Debug.Log($"Successfully set game difficulty to '{difficulty.DifficultyName}'");
//                GameFlowBus.Publish(GameFlowEvents.GameConfigApplied, this, GameConfig);
//            }
//        }

//        private void OnDifficultySettingsApplied(string eventName, object sender, object data)
//        {
//            // When RemoteConfig difficulty settings are fetched, apply the first one as the default config
//            var difficultyConfigs = data as System.Collections.Generic.IList<DifficultyConfig>;
//            if (difficultyConfigs != null && difficultyConfigs.Count > 0)
//            {
//                OnGameDifficultyChanged(eventName, sender, difficultyConfigs[0]);
//            }
//        }

//        private void SubscribeToEvents()
//        {
//            GameFlowBus.Subscribe(GameFlowEvents.DifficultyChanged, OnGameDifficultyChanged);
//            GameFlowBus.Subscribe(GameFlowEvents.DifficultySettingsApplied, OnDifficultySettingsApplied);
//        }

//        private void UnsubscribeToEvents()
//        {
//            GameFlowBus.Unsubscribe(GameFlowEvents.DifficultyChanged, OnGameDifficultyChanged);
//            GameFlowBus.Unsubscribe(GameFlowEvents.DifficultySettingsApplied, OnDifficultySettingsApplied);
//        }

//#if UNITY_EDITOR
//        [InitializeOnEnterPlayMode()]
//        public static void InitializeOnPlay()
//        {
//            Instance = null;
//        }
//#endif
//    }
//}
