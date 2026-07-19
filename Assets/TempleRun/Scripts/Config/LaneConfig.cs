using UnityEngine;

namespace CrawfisSoftware.TempleRun.GameConfig
{
    /// <summary>
    /// Configuration for the 3-lane movement system.
    /// Create asset via Assets > Create > CrawfisSoftware > TempleRun > LaneConfig.
    /// </summary>
    [CreateAssetMenu(fileName = "LaneConfig", menuName = "CrawfisSoftware/TempleRun/LaneConfig")]
    public class LaneConfig : ScriptableObject
    {
        [Header("Lane Layout")]
        [Tooltip("Number of lanes (must be odd for a center lane). Default: 3")]
        public int LaneCount = 3;

        [Tooltip("Distance between adjacent lane centers in world units")]
        public float LaneWidth = 2f;

        [Header("Lane Change Animation")]
        [Tooltip("Duration of the lateral lane change movement in seconds")]
        public float LaneChangeDuration = 0.2f;

        [Tooltip("Animation curve for lane change lerp (use ease-in-out for polish)")]
        public AnimationCurve LaneChangeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Lane Change Cooldown")]
        [Tooltip("Minimum time between lane change requests in seconds")]
        public float LaneChangeCooldown = 0.3f;
    }
}
