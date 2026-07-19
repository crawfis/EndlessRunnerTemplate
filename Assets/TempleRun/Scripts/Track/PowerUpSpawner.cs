using CrawfisSoftware.TempleRun.GameConfig;

using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Spawns power-ups on track segments based on difficulty settings.
    /// Procedural — random placement using DifficultyConfig rates.
    /// Preset — place power-ups exactly where SpawnSlots dictate.
    /// Hybrid — place Required SpawnSlots then fill remaining space procedurally.
    ///
    ///    Dependencies: Blackboard, DifficultyConfig, PowerUpDefinition[], SpawnPrefabRegistry (optional)
    ///    Subscribes: TempleRunEvents.SplineSegmentCreated
    ///    Subscribes: TempleRunEvents.TeleportEnded
    /// </summary>
    internal class PowerUpSpawner : SpawnerBase
    {
        [Header("Power-Up Definitions")]
        [Tooltip("Available power-up types. Weighted random selection chooses from this list.")]
        [SerializeField] private PowerUpDefinition[] _powerUpDefinitions;

        [Header("Spawn Settings")]
        [Tooltip("Minimum distance from segment start before a power-up can spawn.")]
        [SerializeField] private float _minDistanceFromSegmentStart = 2f;

        [Tooltip("Minimum distance from segment end before a power-up can spawn.")]
        [SerializeField] private float _minDistanceFromSegmentEnd = 2f;

        [Tooltip("Height above the track surface to place power-ups.")]
        [SerializeField] private float _platformHeight = 2.0f;

        // -----------------------------------------------------------------
        // SpawnerBase overrides
        // -----------------------------------------------------------------

        protected override string ContainerName => "Generated PowerUps";

        protected override bool HandlesSlotType(string slotType)
            => slotType == "PowerUp";

        protected override void SpawnProcedural(SplineSegmentData data)
        {
            if (_powerUpDefinitions == null || _powerUpDefinitions.Length == 0) return;

            float spawnRate = Blackboard.Instance.GameConfig.PowerUpSpawnRate;
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

            int lane = RandomLane();
            spawnPosition -= data.Perpendicular * (lane * GetLaneWidth());

            PowerUpDefinition powerUpDef = SelectWeightedRandom();
            Track(SpawnPowerUp(spawnPosition, data.UnitDirection, powerUpDef));
        }

        protected override GameObject SpawnFromSlot(SplineSegmentData data, SpawnSlotDefinition slot)
        {
            Vector3 pos = SlotWorldPosition(data, slot, _platformHeight, GetLaneWidth());

            // Try to match a PowerUpDefinition by PrefabTag
            PowerUpDefinition matchedDef = null;
            GameObject prefab = ResolvePrefab(slot, null);

            if (!string.IsNullOrWhiteSpace(slot.PrefabTag) && _powerUpDefinitions != null)
            {
                foreach (var def in _powerUpDefinitions)
                {
                    if (def.PowerUpId == slot.PrefabTag)
                    {
                        matchedDef = def;
                        if (prefab == null) prefab = def.Prefab;
                        break;
                    }
                }
            }

            if (matchedDef == null && _powerUpDefinitions != null && _powerUpDefinitions.Length > 0)
                matchedDef = SelectWeightedRandom();

            Quaternion rotation = Quaternion.LookRotation(data.UnitDirection);
            GameObject powerUp;

            if (prefab != null)
            {
                powerUp = Instantiate(prefab, pos, rotation, _parentTransform);
            }
            else
            {
                Color tint = matchedDef != null ? matchedDef.TintColor : Color.cyan;
                powerUp = CreateDefaultPowerUpPrefab(tint);
                powerUp.transform.SetParent(_parentTransform, false);
                powerUp.transform.SetLocalPositionAndRotation(pos, rotation);
            }

            powerUp.name = $"SlotPowerUp_{_segmentNumber}_{slot.PrefabTag}";
            powerUp.tag = "PowerUp";

            if (matchedDef != null)
            {
                var identifier = powerUp.GetComponent<PowerUpIdentifier>();
                if (identifier == null) identifier = powerUp.AddComponent<PowerUpIdentifier>();
                identifier.Definition = matchedDef;
            }

            return powerUp;
        }

        // -----------------------------------------------------------------
        // PowerUp-specific helpers
        // -----------------------------------------------------------------

        private PowerUpDefinition SelectWeightedRandom()
        {
            float totalWeight = 0f;
            foreach (var def in _powerUpDefinitions)
                totalWeight += def.SpawnWeight;

            float roll = (float)_random.NextDouble() * totalWeight;
            float cumulative = 0f;
            foreach (var def in _powerUpDefinitions)
            {
                cumulative += def.SpawnWeight;
                if (roll <= cumulative) return def;
            }
            return _powerUpDefinitions[^1];
        }

        private GameObject SpawnPowerUp(Vector3 position, Vector3 forward, PowerUpDefinition definition)
        {
            GameObject prefab = definition.Prefab;
            Quaternion rotation = Quaternion.LookRotation(forward);
            GameObject powerUp;

            if (prefab != null)
            {
                powerUp = Instantiate(prefab, position, rotation, _parentTransform);
            }
            else
            {
                powerUp = CreateDefaultPowerUpPrefab(definition.TintColor);
                powerUp.transform.SetParent(_parentTransform, false);
                powerUp.transform.localPosition = position;
                powerUp.transform.localRotation = rotation;
            }

            powerUp.name = $"PowerUp_{definition.PowerUpId}_{_segmentNumber}";
            powerUp.tag = "PowerUp";

            var identifier = powerUp.GetComponent<PowerUpIdentifier>();
            if (identifier == null)
                identifier = powerUp.AddComponent<PowerUpIdentifier>();
            identifier.Definition = definition;

            return powerUp;
        }

        /// <summary>
        /// Creates a default power-up (tinted capsule with trigger collider) when no custom prefab is assigned.
        /// </summary>
        private GameObject CreateDefaultPowerUpPrefab(Color tintColor)
        {
            GameObject powerUp = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            powerUp.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

            Collider defaultCollider = powerUp.GetComponent<Collider>();
            if (defaultCollider != null)
                defaultCollider.isTrigger = true;

            Renderer renderer = powerUp.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = tintColor;

            return powerUp;
        }
    }
}
