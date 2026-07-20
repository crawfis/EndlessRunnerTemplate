using System;
using System.Collections.Generic;

using UnityEngine;

namespace CrawfisSoftware.TempleRun.GameConfig
{
    /// <summary>
    /// Maps PrefabTag strings (from track segment JSON) to Unity prefab references.
    /// Decouples data-driven track definitions from Unity asset GUIDs.
    ///
    ///   Usage: Assign in the Inspector, then look up at runtime:
    ///     var prefab = registry.GetPrefab("low_barrier");
    ///
    ///   Create via: Assets > Create > TempleRun > Spawn Prefab Registry
    /// </summary>
    [CreateAssetMenu(
        fileName = "SpawnPrefabRegistry",
        menuName = "TempleRun/Spawn Prefab Registry",
        order = 200)]
    public class SpawnPrefabRegistry : ScriptableObject
    {
        [Serializable]
        public class PrefabEntry
        {
            [Tooltip("Tag string used in segment JSON SpawnSlots (e.g., 'low_barrier', 'coin_line', 'speed_boost').")]
            public string Tag;

            [Tooltip("The prefab to instantiate for this tag.")]
            public GameObject Prefab;

            [Tooltip("Optional fallback colour when no prefab is assigned (used by default primitive spawning).")]
            public Color FallbackColor = Color.magenta;
        }

        [SerializeField]
        [Tooltip("All registered prefab tags and their associated prefabs.")]
        private List<PrefabEntry> _entries = new List<PrefabEntry>();

        // Runtime lookup built on first access
        private Dictionary<string, PrefabEntry> _lookup;

        /// <summary>All registered entries (read-only for editor tooling).</summary>
        public IReadOnlyList<PrefabEntry> Entries => _entries;

        /// <summary>
        /// Returns the prefab for the given tag, or null if not registered.
        /// </summary>
        public GameObject GetPrefab(string prefabTag)
        {
            EnsureLookup();
            return _lookup.TryGetValue(prefabTag, out var entry) ? entry.Prefab : null;
        }

        /// <summary>
        /// Returns the full entry (prefab + fallback colour) for the given tag, or null.
        /// </summary>
        public PrefabEntry GetEntry(string prefabTag)
        {
            EnsureLookup();
            return _lookup.TryGetValue(prefabTag, out var entry) ? entry : null;
        }

        /// <summary>
        /// Returns true if the tag is registered (even if the prefab is null).
        /// </summary>
        public bool HasTag(string prefabTag)
        {
            EnsureLookup();
            return _lookup.ContainsKey(prefabTag);
        }

        private void EnsureLookup()
        {
            if (_lookup != null) return;

            _lookup = new Dictionary<string, PrefabEntry>(_entries.Count);
            foreach (var entry in _entries)
            {
                if (!string.IsNullOrWhiteSpace(entry.Tag))
                    _lookup[entry.Tag] = entry;
            }
        }

        private void OnValidate()
        {
            // Force rebuild on Inspector changes
            _lookup = null;
        }
    }
}
