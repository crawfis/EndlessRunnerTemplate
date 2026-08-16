using CrawfisSoftware.Config;
using CrawfisSoftware.TempleRun.Events;

using System;
using System.Collections.Generic;

using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

namespace CrawfisSoftware.TempleRun.GameConfig
{
    /// <summary>
    /// Manages difficulty configurations for TempleRun gameplay.
    /// Subscribes to TempleRunEvents (via bridge from GameFlow domain).
    ///    Dependencies: DifficultyConfig (shared in _Common)
    ///    Subscribes: TempleRunEvents.DifficultyChanging, TempleRunEvents.DifficultySettingsApplied
    ///    Publishes: TempleRunEvents.DifficultyChanged, TempleRunEvents.DifficultyChangeFailed
    /// </summary>
    public class GameDifficultyManager : MonoBehaviour
    {
        public string CurrentDifficulty { get; private set; } = "Easy";
        public DifficultyConfig CurrentDifficultyConfig
        {
            get
            {
                if (_difficultyConfigs.ContainsKey(CurrentDifficulty))
                {
                    return _difficultyConfigs[CurrentDifficulty];
                }
                else
                {
                    Debug.LogWarning($"Current difficulty '{CurrentDifficulty}' not found. Returning null.");
                    return null;
                }
            }
        }
        public IEnumerable<string> AvailableDifficulties => _difficultyConfigs.Keys;
        public IEnumerable<DifficultyConfig> AvailableDifficultyConfigs => _difficultyConfigs.Values;

        private readonly Dictionary<string, DifficultyConfig> _difficultyConfigs = new Dictionary<string, DifficultyConfig>();

        public void Awake()
        {
            TempleRunBus.Subscribe(TempleRunEvents.TempleRunDifficultyChangeRequested, OnDifficultyChanging);
            TempleRunBus.Subscribe(TempleRunEvents.TempleRunDifficultySettingsApplied, OnDifficultySettingsChanged);
        }

        private void OnDestroy()
        {
            TempleRunBus.Unsubscribe(TempleRunEvents.TempleRunDifficultyChangeRequested, OnDifficultyChanging);
            TempleRunBus.Unsubscribe(TempleRunEvents.TempleRunDifficultySettingsApplied, OnDifficultySettingsChanged);
        }

        public void SetDifficulty(string difficultyName)
        {
            Debug.Log($"Attempting to set game difficulty from {CurrentDifficulty} to {difficultyName}");
            if (_difficultyConfigs.ContainsKey(difficultyName))
            {
                CurrentDifficulty = difficultyName;
                TempleRunBus.Publish(TempleRunEvents.TempleRunDifficultyChanging, this, _difficultyConfigs[CurrentDifficulty]);
            }
            else
            {
                Debug.LogWarning($"SetDifficulty failed: difficulty '{difficultyName}' not found in available configurations.");
            }
        }

        public void PopulateDifficulties(IList<DifficultyConfig> difficulties)
        {
            Clear();
            foreach (var config in difficulties)
            {
                AddConfig(config);
            }
        }

        public void Clear()
        {
            _difficultyConfigs?.Clear();
        }

        public void AddConfig(DifficultyConfig difficultyConfig)
        {
            _difficultyConfigs[difficultyConfig.DifficultyName] = difficultyConfig;
        }

        public void OnDifficultyChanging(string eventName, object sender, object data)
        {
            string newDifficulty = data as string;
            if (string.IsNullOrEmpty(newDifficulty))
            {
                TempleRunBus.Publish(TempleRunEvents.DifficultyChangeFailed, this, CurrentDifficultyConfig);
                return;
            }
            SetDifficulty(newDifficulty);
        }

        public void OnDifficultySettingsChanged(string eventName, object sender, object data)
        {
            var difficultyConfigs = data as IList<DifficultyConfig>;
            if (difficultyConfigs == null)
            {
                throw new ArgumentException("OnDifficultySettingsChanged event data must be of type IList<DifficultyConfig>");
            }
            PopulateDifficulties(difficultyConfigs);
        }
    }
}
