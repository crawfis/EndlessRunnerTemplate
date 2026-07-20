using CrawfisSoftware.TempleRun.GameConfig;

using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Detects collisions between the player and obstacles using Unity trigger colliders.
    /// If the player is jumping high enough, or sliding low enough, the collision is ignored (obstacle cleared).
    /// Otherwise publishes ObstacleHit which auto-chains to PlayerFailing.
    ///    Dependencies: Blackboard, JumpConfig, SlideConfig
    ///    Publishes: TempleRunEvents.ObstacleHit
    /// </summary>
    [RequireComponent(typeof(Collider))]
    internal class ObstacleCollisionDetector : MonoBehaviour
    {
        [Tooltip("Tag used to identify obstacle GameObjects. Must match the tag set on obstacle prefabs.")]
        [SerializeField] private string _obstacleTag = "Obstacle";

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(_obstacleTag)) return;

            // Check if the player is jumping high enough to clear the obstacle
            float currentJumpHeight = Blackboard.Instance.JumpHeightOffset;
            JumpConfig jumpConfig = Blackboard.Instance.JumpConfig;
            float jumpClearanceHeight = jumpConfig != null ? jumpConfig.ObstacleClearanceHeight : 1f;

            if (currentJumpHeight >= jumpClearanceHeight)
            {
                // Player cleared the obstacle by jumping — no collision
                return;
            }

            // Check if the player is sliding low enough to pass under the obstacle
            float currentSlideHeight = Blackboard.Instance.SlideHeightOffset;
            SlideConfig slideConfig = Blackboard.Instance.SlideConfig;
            float slideClearanceHeight = slideConfig != null ? slideConfig.SlideObstacleClearanceHeight : 0.5f;

            if (currentSlideHeight <= -slideClearanceHeight)
            {
                // Player cleared the obstacle by sliding under — no collision
                return;
            }

            // Player hit the obstacle
            EventsPublisherTempleRun.Instance.PublishEvent(
                TempleRunEvents.ObstacleHit, this, other.gameObject);
        }
    }
}
