using CrawfisSoftware.Config;

using System;
using System.Collections.Generic;

namespace CrawfisSoftware.TempleRun.GameConfig
{
    /// <summary>
    /// Serializable data container for difficulty configurations.
    ///    Publishes: TempleRunEvents.TempleRunDifficultySettingsApplied (when Configs setter is invoked)
    /// </summary>
    [Serializable]
    public class DifficultySettings
    {
        private List<DifficultyConfig> _configs;

        public List<DifficultyConfig> Configs
        {
            get
            {
                return _configs;
            }
            set {
                _configs = value;
                EventsPublisherTempleRun.Instance.PublishEvent(
                    TempleRunEvents.TempleRunDifficultySettingsApplied, this, _configs);
            }
        }
    }
}