using UnityEngine;

namespace CrawfisSoftware.TempleRun.GameConfig
{
    /// <summary>
    /// Configuration for the dash mechanic.
    /// Create asset via Assets > Create > CrawfisSoftware > TempleRun > DashConfig.
    /// Dash provides a temporary speed boost for rapid movement.
    /// </summary>
    [CreateAssetMenu(fileName = "DashConfig", menuName = "CrawfisSoftware/TempleRun/DashConfig")]
    public class DashConfig : ScriptableObject
    {
        [Header("Dash Physics")]
        [Tooltip("Speed multiplier during dash (e.g., 2.0 = 2x normal speed)")]
        public float DashSpeedMultiplier = 2.0f;

        [Tooltip("Total duration of the dash effect in seconds")]
        public float DashDuration = 0.8f;

        [Tooltip("Animation curve for dash acceleration/deceleration. X = normalized time, Y = normalized multiplier (0→1→0 for ease-in/out).")]
        public AnimationCurve DashCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 2f, 2f),
            new Keyframe(0.5f, 1f, 0f, 0f),
            new Keyframe(1f, 0f, -2f, -2f)
        );

        [Header("Dash Cooldown")]
        [Tooltip("Minimum time between dash activations in seconds")]
        public float DashCooldown = 1.0f;
    }
}
