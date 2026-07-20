using CrawfisSoftware.TempleRun.GameConfig;

using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Spawns lines of coins on track segments based on difficulty settings.
    /// Procedural — random placement using CoinConfig rates.
    /// Preset — place coins exactly where SpawnSlots dictate.
    /// Hybrid — place Required SpawnSlots then fill remaining space procedurally.
    ///
    ///    Dependencies: Blackboard, DifficultyConfig, CoinConfig, LaneConfig, SpawnPrefabRegistry (optional)
    ///    Subscribes: TempleRunEvents.SplineSegmentCreated
    ///    Subscribes: TempleRunEvents.TeleportEnded
    /// </summary>
    internal class CoinSpawner : SpawnerBase
    {
        [Header("Coin Config")]
        [SerializeField] private CoinConfig _coinConfig;

        // -----------------------------------------------------------------
        // SpawnerBase overrides
        // -----------------------------------------------------------------

        protected override string ContainerName => "Generated Coins";

        protected override bool HandlesSlotType(string slotType)
            => slotType == "Coin";

        protected override void SpawnProcedural(SplineSegmentData data)
        {
            if (_coinConfig == null) return;

            float spawnRate = Blackboard.Instance.GameConfig.CoinSpawnRate;
            if ((float)_random.NextDouble() > spawnRate) return;

            float minStart = _coinConfig.MinDistanceFromSegmentStart;
            float minEnd = _coinConfig.MinDistanceFromSegmentEnd;
            float usableLength = data.SegmentLength - minStart - minEnd;

            int coinCount = _random.Next(_coinConfig.MinCoinsPerLine, _coinConfig.MaxCoinsPerLine + 1);
            float totalLineLength = (coinCount - 1) * _coinConfig.SpacingBetweenCoins;

            if (usableLength <= 0f || totalLineLength > usableLength)
            {
                coinCount = Mathf.Max(1, Mathf.FloorToInt(usableLength / _coinConfig.SpacingBetweenCoins) + 1);
                totalLineLength = (coinCount - 1) * _coinConfig.SpacingBetweenCoins;
            }

            if (usableLength <= 0f) return;

            float maxStartDistance = usableLength - totalLineLength;
            float startDistance = minStart + (float)_random.NextDouble() * Mathf.Max(0f, maxStartDistance);

            float laneWidth = GetLaneWidth();
            int lane = RandomLane();
            Vector3 laneOffset = -data.Perpendicular * (lane * laneWidth);

            for (int i = 0; i < coinCount; i++)
            {
                float dist = startDistance + i * _coinConfig.SpacingBetweenCoins;
                Vector3 pos = data.Point1
                              + data.UnitDirection * dist
                              + _coinConfig.PlatformHeight * Vector3.up
                              + laneOffset;

                Track(SpawnCoin(pos, data.UnitDirection));
            }
        }

        protected override GameObject SpawnFromSlot(SplineSegmentData data, SpawnSlotDefinition slot)
        {
            float height = slot.Height > 0f ? slot.Height : (_coinConfig != null ? _coinConfig.PlatformHeight : 1f);
            Vector3 pos = SlotWorldPosition(data, slot, height, GetLaneWidth());

            GameObject prefab = ResolvePrefab(slot, _coinConfig != null ? _coinConfig.CoinPrefab : null);

            if (prefab != null)
            {
                Quaternion rotation = Quaternion.LookRotation(data.UnitDirection);
                GameObject coin = Instantiate(prefab, pos, rotation, _parentTransform);
                coin.name = $"SlotCoin_{_segmentNumber}_{slot.PrefabTag}";
                coin.tag = "Coin";
                return coin;
            }

            GameObject defaultCoin = CreateDefaultCoinPrefab();
            defaultCoin.transform.SetParent(_parentTransform, false);
            defaultCoin.transform.localPosition = pos;
            defaultCoin.name = $"SlotCoin_{_segmentNumber}_default";
            defaultCoin.tag = "Coin";
            return defaultCoin;
        }

        // -----------------------------------------------------------------
        // Coin-specific helpers
        // -----------------------------------------------------------------

        private GameObject SpawnCoin(Vector3 position, Vector3 forward)
        {
            GameObject prefab = _coinConfig != null ? _coinConfig.CoinPrefab : null;
            GameObject coin;

            if (prefab != null)
            {
                Quaternion rotation = Quaternion.LookRotation(forward);
                coin = Instantiate(prefab, position, rotation, _parentTransform);
            }
            else
            {
                coin = CreateDefaultCoinPrefab();
                coin.transform.SetParent(_parentTransform, false);
                coin.transform.localPosition = position;
            }

            coin.name = $"Coin_{_segmentNumber}";
            coin.tag = "Coin";
            return coin;
        }

        /// <summary>
        /// Creates a default coin (yellow sphere with trigger collider) when no custom prefab is assigned.
        /// </summary>
        private GameObject CreateDefaultCoinPrefab()
        {
            GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            coin.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            Collider defaultCollider = coin.GetComponent<Collider>();
            if (defaultCollider != null)
                defaultCollider.isTrigger = true;

            Renderer renderer = coin.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = Color.yellow;

            return coin;
        }
    }
}
