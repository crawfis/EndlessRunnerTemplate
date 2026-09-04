using CrawfisSoftware.Config;
using CrawfisSoftware.TempleRun.Events;

using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

namespace CrawfisSoftware.TempleRun.GameConfig
{
    /// <summary>
    /// Manages difficulty configurations for TempleRun gameplay.
    /// Subscribes to TempleRunEvents (via bridge from GameFlow domain).
    ///    Dependencies: DifficultyConfig (shared in _Common)
    ///    Subscribes: TempleRunEvents.TempleRunDifficultyChangeRequested, TempleRunEvents.TempleRunDifficultySettingsApplied
    ///    Publishes: TempleRunEvents.TempleRunDifficultyChanging, TempleRunEvents.DifficultyChangeFailed
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
            TempleRunBus.Subscribe(TempleRunEvents.TempleRunDifficultyChangeRequested, OnDifficultyChangeRequested);
            TempleRunBus.Subscribe(TempleRunEvents.TempleRunDifficultySettingsApplied, OnDifficultySettingsChanged);
        }

        private void OnDestroy()
        {
            TempleRunBus.Unsubscribe(TempleRunEvents.TempleRunDifficultyChangeRequested, OnDifficultyChangeRequested);
            TempleRunBus.Unsubscribe(TempleRunEvents.TempleRunDifficultySettingsApplied, OnDifficultySettingsChanged);
        }

        // The table is the selected level's difficulty variants, so a level decides which
        // difficulties it offers. A preference the level does not offer resolves to the level's
        // first variant rather than leaving GameConfig unset - the player asked for a level, and
        // playing it at its own difficulty beats not playing it.
        public void SetDifficulty(string difficultyName)
        {
            Debug.Log($"Attempting to set game difficulty from {CurrentDifficulty} to {difficultyName}");
            if (!_difficultyConfigs.ContainsKey(difficultyName))
            {
                if (_difficultyConfigs.Count == 0)
                {
                    Debug.LogWarning($"SetDifficulty failed: no difficulty configurations have been applied.");
                    return;
                }
                string fallback = _difficultyConfigs.Keys.First();
                Debug.LogWarning($"This level does not offer difficulty '{difficultyName}'; using '{fallback}'.");
                difficultyName = fallback;
            }
            CurrentDifficulty = difficultyName;
            TempleRunBus.Publish(TempleRunEvents.TempleRunDifficultyChanging, this, _difficultyConfigs[CurrentDifficulty]);
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

        // An empty name is still worth reporting: a payload of the wrong type throws on the cast,
        // but a caller can legitimately publish "" and there is no difficulty by that name.
        public void OnDifficultyChangeRequested(string eventName, object sender, object data)
        {
            var newDifficulty = (string)data;
            if (string.IsNullOrEmpty(newDifficulty))
            {
                TempleRunBus.Publish(TempleRunEvents.DifficultyChangeFailed, this, CurrentDifficultyConfig);
                return;
            }
            SetDifficulty(newDifficulty);
        }

        public void OnDifficultySettingsChanged(string eventName, object sender, object data)
        {
            var difficultyConfigs = (IList<DifficultyConfig>)data;
            PopulateDifficulties(difficultyConfigs);
        }
    }
}
