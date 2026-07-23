using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// The shared pool of authored track segments. A single asset referenced by every
    /// <see cref="TrackLevelSO"/>; each level selects a subset by tag or id.
    ///
    /// Mirrors the LevelRegistry / SpawnPrefabRegistry pattern: a registry SO holding
    /// references to the individual authoring assets. Pure authoring data — no logic;
    /// <see cref="TrackLibraryLoader"/> is what reads it.
    ///
    /// Create via: Assets > Create > CrawfisSoftware > TempleRun > Track Segment Registry
    /// </summary>
    [CreateAssetMenu(
        fileName = "TrackSegmentRegistry",
        menuName = "CrawfisSoftware/TempleRun/Track Segment Registry",
        order = 211)]
    public class TrackSegmentRegistrySO : ScriptableObject
    {
        [Tooltip("Every segment available across all levels. Levels filter this pool by tag or id.")]
        public TrackSegmentSO[] Segments;
    }
}
