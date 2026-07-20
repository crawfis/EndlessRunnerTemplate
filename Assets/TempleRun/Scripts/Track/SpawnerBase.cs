using CrawfisSoftware.TempleRun.GameConfig;

using System.Collections.Generic;

using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Abstract base for segment-reactive spawners (obstacles, coins, power-ups).
    /// Handles event subscription, SpawnMode routing, object tracking,
    /// and per-segment cleanup on teleport.
    ///
    ///    Subscribes: TempleRunEvents.SplineSegmentCreated
    ///    Subscribes: TempleRunEvents.TeleportEnded
    /// </summary>
    internal abstract class SpawnerBase : MonoBehaviour
    {
        [Header("Prefab Registry (optional — for Preset/Hybrid SpawnSlots)")]
        [SerializeField] protected SpawnPrefabRegistry _prefabRegistry;

        protected Transform _parentTransform;
        protected readonly Dictionary<int, List<GameObject>> _spawnedBySegment = new();
        protected int _currentSegmentID = -1;
        protected int _segmentNumber = 0;
        protected System.Random _random;

        /// <summary>Name for the container GameObject.</summary>
        protected abstract string ContainerName { get; }

        /// <summary>SpawnSlot.Type values this spawner handles (e.g. "Obstacle").</summary>
        protected abstract bool HandlesSlotType(string slotType);

        /// <summary>Spawn objects using the procedural algorithm.</summary>
        protected abstract void SpawnProcedural(SplineSegmentData data);

        /// <summary>Spawn a single object from a SpawnSlot.</summary>
        protected abstract GameObject SpawnFromSlot(SplineSegmentData data, SpawnSlotDefinition slot);

        // -----------------------------------------------------------------
        // Lifecycle
        // -----------------------------------------------------------------

        protected virtual void Awake()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(
                TempleRunEvents.SplineSegmentCreated, OnSplineSegmentCreated);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(
                TempleRunEvents.TeleportEnded, OnTeleportEnded);

            _parentTransform = new GameObject(ContainerName).transform;
            _random = new System.Random(Blackboard.Instance.MasterRandom.Next());
        }

        protected virtual void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(
                TempleRunEvents.SplineSegmentCreated, OnSplineSegmentCreated);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(
                TempleRunEvents.TeleportEnded, OnTeleportEnded);
        }

        // -----------------------------------------------------------------
        // Event handlers
        // -----------------------------------------------------------------

        private void OnSplineSegmentCreated(string eventName, object sender, object data)
        {
            if (_random == null) return;

            var splineData = (SplineSegmentData)data;
            string mode = splineData.Definition?.SpawnMode ?? SpawnModes.Procedural;

            switch (mode)
            {
                case SpawnModes.Preset:
                    SpawnFromSlots(splineData, hybrid: false);
                    break;
                case SpawnModes.Hybrid:
                    SpawnFromSlots(splineData, hybrid: true);
                    break;
                default:
                    SpawnProcedural(splineData);
                    break;
            }

            _segmentNumber++;
        }

        private void OnTeleportEnded(string eventName, object sender, object data)
        {
            if (_currentSegmentID >= 0 &&
                _spawnedBySegment.TryGetValue(_currentSegmentID, out var objects))
            {
                foreach (var obj in objects)
                    if (obj != null) Destroy(obj);
                _spawnedBySegment.Remove(_currentSegmentID);
            }
            _currentSegmentID++;
        }

        // -----------------------------------------------------------------
        // Slot routing (shared across all spawners)
        // -----------------------------------------------------------------

        private void SpawnFromSlots(SplineSegmentData data, bool hybrid)
        {
            var slots = data.Definition?.SpawnSlots;
            if (slots == null || slots.Count == 0)
            {
                if (hybrid) SpawnProcedural(data);
                return;
            }

            foreach (var slot in slots)
            {
                if (!HandlesSlotType(slot.Type)) continue;
                if (!slot.Required && (float)_random.NextDouble() > slot.Weight) continue;

                Track(SpawnFromSlot(data, slot));
            }

            if (hybrid) SpawnProcedural(data);
        }

        // -----------------------------------------------------------------
        // Helpers available to subclasses
        // -----------------------------------------------------------------

        /// <summary>Register a spawned object for per-segment cleanup.</summary>
        protected void Track(GameObject obj)
        {
            if (obj == null) return;
            if (!_spawnedBySegment.ContainsKey(_segmentNumber))
                _spawnedBySegment[_segmentNumber] = new List<GameObject>();
            _spawnedBySegment[_segmentNumber].Add(obj);
        }

        /// <summary>Compute world position from a SpawnSlot definition.</summary>
        protected Vector3 SlotWorldPosition(SplineSegmentData data, SpawnSlotDefinition slot,
                                            float defaultHeight, float laneWidth)
        {
            float height = slot.Height > 0f ? slot.Height : defaultHeight;
            return data.Point1
                   + data.UnitDirection * (slot.NormalizedPosition * data.SegmentLength)
                   + height * Vector3.up
                   - data.Perpendicular * (slot.Lane * laneWidth);
        }

        /// <summary>Resolve a prefab by PrefabTag via registry, falling back to <paramref name="fallback"/>.</summary>
        protected GameObject ResolvePrefab(SpawnSlotDefinition slot, GameObject fallback)
        {
            if (_prefabRegistry != null && !string.IsNullOrWhiteSpace(slot.PrefabTag))
            {
                var found = _prefabRegistry.GetPrefab(slot.PrefabTag);
                if (found != null) return found;
            }
            return fallback;
        }

        /// <summary>Convenience: get the LaneConfig-derived lane width.</summary>
        protected float GetLaneWidth()
        {
            LaneConfig lc = Blackboard.Instance.LaneConfig;
            return lc != null ? lc.LaneWidth : 2f;
        }

        /// <summary>Convenience: get the LaneConfig-derived lane count.</summary>
        protected int GetLaneCount()
        {
            LaneConfig lc = Blackboard.Instance.LaneConfig;
            return lc != null ? lc.LaneCount : 3;
        }

        /// <summary>Pick a random lane index in the range [-halfLanes, +halfLanes].</summary>
        protected int RandomLane()
        {
            int half = (GetLaneCount() - 1) / 2;
            return _random.Next(-half, half + 1);
        }
    }
}
