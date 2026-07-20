using UnityEngine;

namespace CrawfisSoftware.TempleRun.GameConfig
{
    /// <summary>
    /// Configuration for coin spawning and collection.
    /// Create asset via Assets > Create > CrawfisSoftware > TempleRun > CoinConfig.
    /// </summary>
    [CreateAssetMenu(fileName = "CoinConfig", menuName = "CrawfisSoftware/TempleRun/CoinConfig")]
    public class CoinConfig : ScriptableObject
    {
        [Header("Spawn Settings")]
        [Tooltip("Coin prefab to spawn. If null, a default yellow sphere with trigger collider is created.")]
        public GameObject CoinPrefab;

        [Tooltip("Height above the track surface to place coins.")]
        public float PlatformHeight = 2.0f;

        [Tooltip("Minimum distance from segment start before coins can spawn.")]
        public float MinDistanceFromSegmentStart = 1.5f;

        [Tooltip("Minimum distance from segment end before coins can spawn.")]
        public float MinDistanceFromSegmentEnd = 1.5f;

        [Header("Coin Lines")]
        [Tooltip("Minimum number of coins per line.")]
        public int MinCoinsPerLine = 3;

        [Tooltip("Maximum number of coins per line.")]
        public int MaxCoinsPerLine = 7;

        [Tooltip("Distance between coins in a line along the track direction.")]
        public float SpacingBetweenCoins = 1.0f;

        [Header("Value")]
        [Tooltip("Base point value per coin collected.")]
        public int CoinValue = 1;
    }
}
