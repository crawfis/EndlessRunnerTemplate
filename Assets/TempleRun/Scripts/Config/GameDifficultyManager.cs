using CrawfisSoftware.Config;
using CrawfisSoftware.Events;
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

        private static readonly EventId<string> DifficultyChangeRequested =
            TempleRunBus.Id<string>(TempleRunEvents.TempleRunDifficultyChangeRequested);
        private static readonly EventId<IList<DifficultyConfig>> DifficultySettingsApplied =
            TempleRunBus.Id<IList<DifficultyConfig>>(TempleRunEvents.TempleRunDifficultySettingsApplied);
        private static readonly EventId<DifficultyConfig> DifficultyChanging =
            TempleRunBus.Id<DifficultyConfig>(TempleRunEvents.TempleRunDifficultyChanging);
        private static readonly EventId<DifficultyConfig> DifficultyChangeFailed =
            TempleRunBus.Id<DifficultyConfig>(TempleRunEvents.DifficultyChangeFailed);

        public void Awake()
        {
            DifficultyChangeRequested.Subscribe(OnDifficultyChangeRequested);
            DifficultySettingsApplied.Subscribe(OnDifficultySettingsChanged);
        }

        private void OnDestroy()
        {
            DifficultyChangeRequested.Unsubscribe(OnDifficultyChangeRequested);
            DifficultySettingsApplied.Unsubscribe(OnDifficultySettingsChanged);
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
            DifficultyChanging.Publish(this, _difficultyConfigs[CurrentDifficulty]);
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

        // An empty name is still worth reporting: the payload type is now guaranteed, but a
        // caller can legitimately publish "" and there is no difficulty by that name.
        public void OnDifficultyChangeRequested(string eventName, object sender, string newDifficulty)
        {
            if (string.IsNullOrEmpty(newDifficulty))
            {
                DifficultyChangeFailed.Publish(this, CurrentDifficultyConfig);
                return;
            }
            SetDifficulty(newDifficulty);
        }

        public void OnDifficultySettingsChanged(string eventName, object sender, IList<DifficultyConfig> difficultyConfigs)
        {
            PopulateDifficulties(difficultyConfigs);
        }
    }
}
