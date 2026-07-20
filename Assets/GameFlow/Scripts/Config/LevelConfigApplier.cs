using CrawfisSoftware.GameFlow.Events;

using UnityEngine;

namespace CrawfisSoftware.GameFlow.Config
{
    /// <summary>
    /// Applies the selected level's configuration to the game config pipeline.
    /// Bridges level selection to both the difficulty system and track generation.
    ///    Subscribes: GameFlowEvents.LevelSelected
    ///    Publishes: GameFlowEvents.GameConfigApplied (data: DifficultyConfig)
    ///    Publishes: GameFlowEvents.TrackConfigApplied (data: string trackLevelResourcePath)
    /// </summary>
    internal class LevelConfigApplier : MonoBehaviour
    {
        private void Awake()
        {
            EventsPublisherGameFlow.Instance.SubscribeToEvent(
                GameFlowEvents.LevelSelected, OnLevelSelected);
        }

        private void OnDestroy()
        {
            EventsPublisherGameFlow.Instance.UnsubscribeToEvent(
                GameFlowEvents.LevelSelected, OnLevelSelected);
        }

        private void OnLevelSelected(string eventName, object sender, object data)
        {
            var levelConfig = data as LevelConfig;
            if (levelConfig == null) return;

            if (levelConfig.Difficulty != null)
            {
                EventsPublisherGameFlow.Instance.PublishEvent(
                    GameFlowEvents.GameConfigApplied, this, levelConfig.Difficulty);
            }

            EventsPublisherGameFlow.Instance.PublishEvent(
                GameFlowEvents.TrackConfigApplied, this, levelConfig.TrackLevelResourcePath);
        }
    }
}
