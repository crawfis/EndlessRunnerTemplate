using CrawfisSoftware.GameFlow.Events;

using UnityEngine;
using GameFlowBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.GameFlow.Events.GameFlowEvents>;

namespace CrawfisSoftware.GameFlow.Config
{
    /// <summary>
    /// Applies the selected level's configuration to the game config pipeline.
    /// Bridges level selection to both the difficulty system and track generation.
    ///    Subscribes: GameFlowEvents.LevelSelected
    ///    Publishes: GameFlowEvents.GameConfigApplied (data: DifficultyConfig)
    ///    Publishes: GameFlowEvents.LevelApplied (data: int level number)
    /// </summary>
    internal class LevelConfigApplier : MonoBehaviour
    {
        private void Awake()
        {
            GameFlowBus.Subscribe(
                GameFlowEvents.LevelSelected, OnLevelSelected);
        }

        private void OnDestroy()
        {
            GameFlowBus.Unsubscribe(
                GameFlowEvents.LevelSelected, OnLevelSelected);
        }

        private void OnLevelSelected(string eventName, object sender, object data)
        {
            var levelConfig = data as LevelConfig;
            if (levelConfig == null) return;

            if (levelConfig.Difficulty != null)
            {
                GameFlowBus.Publish(
                    GameFlowEvents.GameConfigApplied, this, levelConfig.Difficulty);
            }

            GameFlowBus.Publish(
                GameFlowEvents.LevelApplied, this, levelConfig.LevelNumber);
        }
    }
}
