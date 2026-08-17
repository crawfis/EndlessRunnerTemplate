using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

namespace CrawfisSoftware.TempleRun.GameConfig
{
    /// <summary>
    /// Supplies a difficulty table when no level has supplied one - i.e. entering play mode
    /// straight into gameplay, without going through level selection.
    ///    Dependencies: TempleRunGameConfig (ScriptableObject)
    ///    Publishes: TempleRunEvents.TempleRunDifficultySettingsApplied (fallback only)
    ///    Publishes: TempleRunEvents.TempleRunDifficultyChangeRequested (fallback only)
    /// </summary>
    /// <remarks>
    /// Selecting a level publishes that level's difficulty variants, which is the normal path and
    /// arrives before this scene loads. TempleRunDifficultySettingsApplied is Sticky, so the level's
    /// table is already retained by the time Start runs and this stands down. Without that check
    /// the global table would clobber the level's - PopulateDifficulties clears before it fills.
    /// </remarks>
    internal class LoadDefaultGameConfigs : MonoBehaviour
    {
        [SerializeField] private TempleRunGameConfig _gameConfig;
        [SerializeField] private string _difficultyLevel = "Easy";

        private void Start()
        {
            if (TempleRunBus.TryGetLast(TempleRunEvents.TempleRunDifficultySettingsApplied, out _, out _))
                return;

            TempleRunBus.Publish(TempleRunEvents.TempleRunDifficultySettingsApplied, this, _gameConfig.DifficultyConfigs);
            TempleRunBus.Publish(TempleRunEvents.TempleRunDifficultyChangeRequested, this, _difficultyLevel);
        }
    }
}