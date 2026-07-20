using CrawfisSoftware.TempleRun.GameConfig;

using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Spawns obstacles on track segments based on difficulty settings.
    /// Procedural — random placement using DifficultyConfig rates.
    /// Preset — place obstacles exactly where SpawnSlots dictate.
    /// Hybrid — place Required SpawnSlots then fill remaining space procedurally.
    ///
    ///    Dependencies: Blackboard, DifficultyConfig, LaneConfig, SpawnPrefabRegistry (optional)
    ///    Subscribes: TempleRunEvents.SplineSegmentCreated
    ///    Subscribes: TempleRunEvents.TeleportEnded
    /// </summary>
    internal class ObstacleSpawner : SpawnerBase
    {
        [Header("Obstacle Prefabs")]
        [Tooltip("Obstacle that spans the full track width — player must jump to clear it.")]
        [SerializeField] private GameObject _fullWidthObstaclePrefab;

        [Tooltip("Obstacle that blocks a single lane — player can jump or lane-change to avoid.")]
        [SerializeField] private GameObject _laneObstaclePrefab;

        [Header("Spawn Settings")]
        [Tooltip("Minimum distance from segment start before an obstacle can spawn (prevents spawning at turn points).")]
        [SerializeField] private float _minDistanceFromSegmentStart = 2f;

        [Tooltip("Minimum distance from segment end before an obstacle can spawn.")]
        [SerializeField] private float _minDistanceFromSegmentEnd = 2f;

        [Tooltip("Probability (0-1) that a spawned obstacle is full-width rather than lane-specific.")]
        [SerializeField] [Range(0f, 1f)] private float _fullWidthProbability = 0.3f;

        [Header("Obstacle Dimensions")]
        [Tooltip("Height of obstacle colliders (Y-axis). Should be less than jump clearance height.")]
        [SerializeField] private float _obstacleHeight = 0.5f;

        [Tooltip("Depth of obstacle colliders (Z-axis along track).")]
        [SerializeField] private float _obstacleDepth = 0.5f;

        [Tooltip("Initial height the obstacle should be placed.")]
        [SerializeField] private float _platformHeight = 1.5f;

        // -----------------------------------------------------------------
        // SpawnerBase overrides
        // -----------------------------------------------------------------

        protected override string ContainerName => "Generated Obstacles";

        protected override bool HandlesSlotType(string slotType)
            => slotType == "Obstacle" || slotType == "Hazard";

        protected override void SpawnProcedural(SplineSegmentData data)
        {
            float spawnRate = Blackboard.Instance.GameConfig.ObstacleSpawnRate;
            if ((float)_random.NextDouble() > spawnRate) return;

            float usableLength = data.SegmentLength
                                 - _minDistanceFromSegmentStart
                                 - _minDistanceFromSegmentEnd;
            if (usableLength <= 0f) return;

            float distanceAlongSegment = _minDistanceFromSegmentStart
                                         + (float)_random.NextDouble() * usableLength;
            Vector3 spawnPosition = data.Point1
                                    + data.UnitDirection * distanceAlongSegment
                                    + _platformHeight * Vector3.up;

            bool isFullWidth = (float)_random.NextDouble() < _fullWidthProbability;

            GameObject obstacle = isFullWidth
                ? SpawnFullWidthObstacle(spawnPosition, data.UnitDirection)
                : SpawnLaneObstacle(spawnPosition, data.UnitDirection);

            Track(obstacle);
        }

        protected override GameObject SpawnFromSlot(SplineSegmentData data, SpawnSlotDefinition slot)
        {
            Vector3 pos = SlotWorldPosition(data, slot, _platformHeight, GetLaneWidth());
            GameObject prefab = ResolvePrefab(slot, _laneObstaclePrefab);

            if (prefab != null)
            {
                Quaternion rotation = Quaternion.LookRotation(data.UnitDirection);
                GameObject obstacle = Instantiate(prefab, pos, rotation, _parentTransform);
                obstacle.name = $"SlotObstacle_{_segmentNumber}_{slot.PrefabTag}";
                return obstacle;
            }

            // Absolute fallback: default primitive
            float laneWidth = GetLaneWidth();
            var defaultObj = CreateDefaultObstaclePrefab(laneWidth * 0.8f, _obstacleHeight, _obstacleDepth);
            Quaternion rot = Quaternion.LookRotation(data.UnitDirection);
            defaultObj.transform.SetParent(_parentTransform, false);
            defaultObj.transform.SetLocalPositionAndRotation(pos, rot);
            defaultObj.name = $"SlotObstacle_{_segmentNumber}_default";
            return defaultObj;
        }

        // -----------------------------------------------------------------
        // Obstacle-specific helpers
        // -----------------------------------------------------------------

        private GameObject SpawnFullWidthObstacle(Vector3 position, Vector3 forward)
        {
            GameObject prefab = _fullWidthObstaclePrefab;
            if (prefab == null)
                prefab = CreateDefaultObstaclePrefab(GetFullTrackWidth(), _obstacleHeight, _obstacleDepth);

            Quaternion rotation = Quaternion.LookRotation(forward);
            GameObject obstacle = Instantiate(prefab, position, rotation, _parentTransform);
            obstacle.name = $"FullWidthBarrier_{_segmentNumber}";

            if (_fullWidthObstaclePrefab == null)
                Destroy(prefab);

            return obstacle;
        }

        private GameObject SpawnLaneObstacle(Vector3 position, Vector3 forward)
        {
            GameObject prefab = _laneObstaclePrefab;
            float laneWidth = GetLaneWidth();
            int lane = RandomLane();

            Vector3 perpendicular = Vector3.Cross(forward, Vector3.up).normalized;
            Vector3 lanePosition = position - perpendicular * (lane * laneWidth);

            if (prefab == null)
                prefab = CreateDefaultObstaclePrefab(laneWidth * 0.8f, _obstacleHeight, _obstacleDepth);

            Quaternion rotation = Quaternion.LookRotation(forward);
            GameObject obstacle = Instantiate(prefab, lanePosition, rotation, _parentTransform);
            obstacle.name = $"LaneBarrier_{_segmentNumber}_Lane{lane}";

            if (_laneObstaclePrefab == null)
                Destroy(prefab);

            return obstacle;
        }

        private float GetFullTrackWidth()
        {
            LaneConfig lc = Blackboard.Instance.LaneConfig;
            return lc != null ? lc.LaneWidth * lc.LaneCount : 6f;
        }

        /// <summary>
        /// Creates a default obstacle prefab (cube with trigger collider) when no custom prefab is assigned.
        /// </summary>
        private GameObject CreateDefaultObstaclePrefab(float width, float height, float depth)
        {
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.localScale = new Vector3(width, height, depth);

            Collider defaultCollider = obstacle.GetComponent<Collider>();
            if (defaultCollider != null)
                defaultCollider.isTrigger = true;

            obstacle.tag = "Obstacle";

            Renderer renderer = obstacle.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = Color.red;

            return obstacle;
        }
    }
}
