using UnityEngine;

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
            EventsPublisherTempleRun.Instance.PublishEvent(TempleRunEvents.TempleRunDifficultySettingsApplied, this, _gameConfig.DifficultyConfigs);
            EventsPublisherTempleRun.Instance.PublishEvent(TempleRunEvents.TempleRunDifficultyChangeRequested, this, _difficultyLevel);
        }
    }
}