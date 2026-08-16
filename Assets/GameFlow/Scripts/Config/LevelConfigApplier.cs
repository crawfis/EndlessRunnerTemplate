using System.Collections.Generic;

using CrawfisSoftware.Config;
using CrawfisSoftware.Events;
using CrawfisSoftware.GameFlow.Events;

using UnityEngine;
using GameFlowBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.GameFlow.Events.GameFlowEvents>;

namespace CrawfisSoftware.GameFlow.Config
{
    /// <summary>
    /// Applies the selected level's configuration to the game config pipeline.
    /// Bridges level selection to both the difficulty system and track generation.
    ///    Subscribes: GameFlowEvents.LevelSelected
    ///    Publishes: GameFlowEvents.DifficultySettingsApplied (data: IList&lt;DifficultyConfig&gt;)
    ///    Publishes: GameFlowEvents.LevelApplied (data: int level number)
    /// </summary>
    /// <remarks>
    /// The level publishes its whole difficulty table rather than one resolved config. The
    /// difficulty system owns the choice between variants, and it is the single writer of
    /// Blackboard.GameConfig - so the level's tuning and the player's preference compose
    /// instead of racing to overwrite each other.
    /// </remarks>
    internal class LevelConfigApplier : MonoBehaviour
    {
        private static readonly EventId<LevelConfig> LevelSelected =
            GameFlowBus.Id<LevelConfig>(GameFlowEvents.LevelSelected);
        private static readonly EventId<IList<DifficultyConfig>> DifficultySettingsApplied =
            GameFlowBus.Id<IList<DifficultyConfig>>(GameFlowEvents.DifficultySettingsApplied);
        private static readonly EventId<int> LevelApplied =
            GameFlowBus.Id<int>(GameFlowEvents.LevelApplied);

        private void Awake()
        {
            LevelSelected.Subscribe(OnLevelSelected);
        }

        private void OnDestroy()
        {
            LevelSelected.Unsubscribe(OnLevelSelected);
        }

        private void OnLevelSelected(string eventName, object sender, LevelConfig levelConfig)
        {
            if (levelConfig.Difficulties.Length > 0)
            {
                DifficultySettingsApplied.Publish(this, levelConfig.Difficulties);
            }

            LevelApplied.Publish(this, levelConfig.LevelNumber);
        }
    }
}
