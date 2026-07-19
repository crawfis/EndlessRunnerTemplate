using System;

using UnityEngine;

namespace CrawfisSoftware.Config
{
    /// <summary>
    /// Shared difficulty configuration used across GameFlow, TempleRun, and UGS domains.
    /// This is a domain-neutral serializable container for difficulty settings.
    /// </summary>
    [Serializable]
    public class DifficultyConfig
    {
        // Todo: Make this a composite of various settings classes: MovementSettings, LevelGenerationSettings, GameMechanicsSettings, etc.
        public string DifficultyName = "Medium";

        [Header("Movement Settings")]
        public float InitialSpeed = 5f;
        public float MaxSpeed = 80f;
        public float Acceleration = 0.2f;

        // Make sure these values make sense with the desired tiles used in level generation. For instance, voxel tiles are 4 units long, so min greater than 3 makes sense.
        // The system will clamp values to make sure they are valid. So, 4->7 will always return 4 for tiles of length 4.
        [Header("Level Generation")]
        public int StartRunway = 8;
        public int MinTrackLength = 4;
        public int MaxTrackLength = 19;
        //public int TileSetId = TileSet.Default.Id;

        [Header("Game Mechanics")]
        public float SafePreTurnDistance = 5f; // Maximum distance bfore a turn target that a turn is valid
        public float SafePostTurnDistance = 0.5f; // Maximum distance after a turn target that a turn is valid
        public float InputCoolDownForTurns = 1f; // Minimum time between turn requests. Hopefully less than MinTrackLength / MaxSpeed
        public int NumberOfLives = 2;

        [Header("Spawn Rates")]
        public float ObstacleSpawnRate = 1f;
        public float CoinSpawnRate = 0.8f;
        public float PowerUpSpawnRate = 0.15f;
        public int ScoreMultiplier = 1;
        public bool EnableAdvancedObstacles = false;
    }
}
