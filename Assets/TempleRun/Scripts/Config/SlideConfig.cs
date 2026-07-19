using UnityEngine;

namespace CrawfisSoftware.TempleRun.GameConfig
{
    /// <summary>
    /// Configuration for the slide mechanic.
    /// Create asset via Assets > Create > CrawfisSoftware > TempleRun > SlideConfig.
    /// Slide reduces player height (for obstacle clearance) while increasing movement speed.
    /// </summary>
    [CreateAssetMenu(fileName = "SlideConfig", menuName = "CrawfisSoftware/TempleRun/SlideConfig")]
    public class SlideConfig : ScriptableObject
    {
        [Header("Slide Physics")]
        [Tooltip("How much to reduce player height when sliding in world units (e.g., -0.5 for crouching)")]
        public float SlideHeightOffset = -0.5f;

        [Tooltip("Total duration of the slide animation in seconds")]
        public float SlideDuration = 0.5f;

        [Tooltip("Speed multiplier during slide (e.g., 1.5 = 1.5x normal speed)")]
        public float SlideSpeedMultiplier = 1.5f;

        [Tooltip("Animation curve for slide (0→1→0 for acceleration/deceleration). X = normalized time, Y = normalized multiplier.")]
        public AnimationCurve SlideCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 2f, 2f),
            new Keyframe(0.5f, 1f, 0f, 0f),
            new Keyframe(1f, 0f, -2f, -2f)
        );

        [Header("Slide Cooldown")]
        [Tooltip("Minimum time between slide input requests in seconds")]
        public float SlideCooldown = 0.3f;

        [Header("Obstacle Clearance")]
        [Tooltip("Height reduction needed to clear an obstacle (should match or exceed absolute value of SlideHeightOffset)")]
        public float SlideObstacleClearanceHeight = 0.5f;

        [Tooltip("Optional: forward distance to travel during slide (set to 0 for no distance bonus)")]
        public float SlideDistance = 0f;
    }
}
