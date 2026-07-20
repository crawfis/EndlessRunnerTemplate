using UnityEngine;

namespace CrawfisSoftware.TempleRun.GameConfig
{
    /// <summary>
    /// Data-driven power-up type definition. Each power-up type is a ScriptableObject instance.
    /// Create asset via Assets > Create > CrawfisSoftware > TempleRun > PowerUpDefinition.
    /// </summary>
    [CreateAssetMenu(fileName = "PowerUpDefinition", menuName = "CrawfisSoftware/TempleRun/PowerUpDefinition")]
    public class PowerUpDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique identifier for this power-up type (e.g., SpeedBoost, CoinMagnet).")]
        public string PowerUpId;

        [Tooltip("The buff type this power-up applies. Determines which Blackboard property is modified.")]
        public PowerUpType Type;

        [Header("Visual")]
        [Tooltip("Prefab spawned on the track. If null, a default tinted capsule is created.")]
        public GameObject Prefab;

        [Tooltip("Fallback tint color when no prefab is assigned.")]
        public Color TintColor = Color.yellow;

        [Header("Buff Settings")]
        [Tooltip("How long the buff lasts in seconds after collection.")]
        public float Duration = 5f;

        [Tooltip("Effect strength — meaning varies by type (speed multiplier, score multiplier, magnet radius, etc.).")]
        public float Magnitude = 2f;

        [Header("Spawn Weight")]
        [Tooltip("Relative weight in weighted random selection. Higher = more likely to be chosen when a power-up spawns.")]
        [Range(0f, 1f)]
        public float SpawnWeight = 0.25f;
    }
}
