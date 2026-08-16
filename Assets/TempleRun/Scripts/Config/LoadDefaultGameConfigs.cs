using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

namespace CrawfisSoftware.TempleRun.GameConfig
{
    /// <summary>
    /// Loads default difficulty configurations and requests the default difficulty.
    ///    Dependencies: TempleRunGameConfig (ScriptableObject)
    ///    Publishes: TempleRunEvents.TempleRunDifficultySettingsApplied
    ///    Publishes: TempleRunEvents.TempleRunDifficultyChangeRequested
    /// </summary>
    internal class LoadDefaultGameConfigs : MonoBehaviour
    {
        [SerializeField] private TempleRunGameConfig _gameConfig;
        [SerializeField] private string _difficultyLevel = "Easy";

        private void Start()
        {
            TempleRunBus.Publish(TempleRunEvents.TempleRunDifficultySettingsApplied, this, _gameConfig.DifficultyConfigs);
            TempleRunBus.Publish(TempleRunEvents.TempleRunDifficultyChangeRequested, this, _difficultyLevel);
        }
    }
}