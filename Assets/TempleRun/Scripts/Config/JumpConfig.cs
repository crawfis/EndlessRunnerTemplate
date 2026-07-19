using UnityEngine;

namespace CrawfisSoftware.TempleRun.GameConfig
{
    /// <summary>
    /// Configuration for the jump mechanic.
    /// Create asset via Assets > Create > CrawfisSoftware > TempleRun > JumpConfig.
    /// </summary>
    [CreateAssetMenu(fileName = "JumpConfig", menuName = "CrawfisSoftware/TempleRun/JumpConfig")]
    public class JumpConfig : ScriptableObject
    {
        [Header("Jump Physics")]
        [Tooltip("Maximum height of the jump arc in world units")]
        public float JumpHeight = 3f;

        [Tooltip("Total duration of the jump arc in seconds")]
        public float JumpDuration = 0.6f;

        [Tooltip("Animation curve for the jump arc (0→1→0 parabola). X = normalized time, Y = normalized height.")]
        public AnimationCurve JumpCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 4f),
            new Keyframe(0.5f, 1f, 0f, 0f),
            new Keyframe(1f, 0f, -4f, 0f)
        );

        [Header("Jump Cooldown")]
        [Tooltip("Minimum time between jump input requests in seconds")]
        public float JumpCooldown = 0.4f;

        [Header("Obstacle Clearance")]
        [Tooltip("Minimum JumpHeightOffset required to clear an obstacle")]
        public float ObstacleClearanceHeight = 1f;
    }
}
