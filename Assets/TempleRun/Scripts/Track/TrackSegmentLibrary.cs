using System;
using System.Collections.Generic;
using UnityEngine;

using CrawfisSoftware.TempleRun.Track;

namespace CrawfisSoftware.TempleRun
{
    // =========================================================================
    // String constants (JSON-compatible — used by JsonUtility serialization)
    // =========================================================================

    /// <summary>Values for <see cref="TrackSegmentDefinition.SpawnMode"/>.</summary>
    public static class SpawnModes
    {
        public const string Procedural = "Procedural";
        public const string Preset     = "Preset";
        public const string Hybrid     = "Hybrid";
    }

    /// <summary>Values for <see cref="TrackSegmentDefinition.Role"/>.</summary>
    public static class SegmentRoles
    {
        public const string Normal     = "Normal";
        public const string Opening    = "Opening";
        public const string Checkpoint = "Checkpoint";
        public const string Challenge  = "Challenge";
        public const string Reward     = "Reward";
        public const string Boss       = "Boss";
        public const string Tutorial   = "Tutorial";
    }

    // =========================================================================
    // Data classes (JSON-serializable)
    // =========================================================================

    /// <summary>
    /// Defines where a specific object (obstacle, coin, power-up) should spawn
    /// within a track segment.  Used by <see cref="SpawnModes.Preset"/>
    /// and <see cref="SpawnModes.Hybrid"/> spawn modes.
    /// </summary>
    [Serializable]
    public class SpawnSlotDefinition
    {
        public float  NormalizedPosition;           // 0–1 fraction along segment length
        public int    Lane;                         // 0 = centre, negative = left, positive = right
        public float  Height;                       // height offset above track surface
        public string Type     = "Obstacle";        // Obstacle | Coin | PowerUp | Hazard
        public string PrefabTag;                    // maps to SpawnPrefabRegistry
        public float  Weight   = 1f;                // selection weight (Hybrid mode)
        public bool   Required = true;              // true = always spawns; false = probabilistic
    }

    /// <summary>
    /// A single track segment definition stored in the segment registry.
    /// </summary>
    [Serializable]
    public class TrackSegmentDefinition
    {
        // ----- Core -----
        public string       Id;
        public Direction    Direction; // Calculated in Normalize from the DirectionString
        public float        Length           = 5f;
        public float        Weight           = 1f;
        public int          MaxRepeat        = 0;
        public float        DifficultyRating = 0f;
        public List<string> Tags             = new List<string>();

        // ----- Role & speed -----
        public string Role             = SegmentRoles.Normal;
        public float  SpeedMultiplier  = 1f;

        // ----- Lane overrides -----
        public List<int>   BlockedLanes = new List<int>();
        public List<float> LaneHeights  = new List<float>();
        public List<int>   ActiveLanes  = new List<int>();

        // ----- Spawn slots -----
        public string                   SpawnMode  = SpawnModes.Procedural;
        public List<SpawnSlotDefinition> SpawnSlots = new List<SpawnSlotDefinition>();

        // ----- Visual / determinism -----
        public string VisualTheme;
        public int    SpawnSeed;

        // ----- 3-point segment geometry (Entrance → Pivot → Exit) -----

        /// <summary>
        /// Distance from Entrance to Pivot (the turn / placeholder point).
        /// For Straight segments Pivot coincides with Exit so this equals Length.
        /// Set by NormalizeSegments() when 0.
        /// </summary>
        public float EntranceDistance = 0f;

        /// <summary>
        /// Distance past the Entrance beyond which a required turn is considered failed.
        /// Set by NormalizeSegments(): float.MaxValue for Straight (never fails), otherwise
        /// EntranceDistance + 1 when left at 0.
        /// </summary>
        public float TurnFailureDistance = 0f;

        /// <summary>
        /// Distance from Pivot to Exit in the post-turn direction.
        /// Applies to ALL segment types: Left/Right/Either have a non-zero exit
        /// section the player runs through after turning.
        /// For Straight segments this is 0 (Pivot == Exit).
        /// </summary>
        public float ExitDistance = 0f;

        /// <summary>
        /// Distance from Pivot where the player lands after turning animation (in exit direction).
        /// Must be less than ExitDistance. Only meaningful for turn segments.
        /// Defaults to ExitDistance * 0.5 in NormalizeSegments().
        /// </summary>
        public float TeleportDistance = 0f;

        /// <summary>
        /// Optional turn radius (world units) for rounded corners, read by
        /// <see cref="Track.Geometry.ArcTurnBuilder"/>. Default 0 means a hard 90° corner —
        /// geometry is bit-identical to the default <see cref="Track.Geometry.AxisAligned90Builder"/>.
        /// Ignored by the default builder and by non-turn segments.
        /// </summary>
        public float TurnRadius = 0f;
    }

    [Serializable]
    public class TrackSegmentConnection
    {
        public string FromId;
        public string ToId;
    }

    /// <summary>
    /// Standalone registry of all available track segments.
    /// Loaded once and shared across multiple level rulesets.
    /// </summary>
    [Serializable]
    public class TrackSegmentRegistryDefinition
    {
        public string Version;
        public List<TrackSegmentDefinition> Segments = new List<TrackSegmentDefinition>();
    }

    /// <summary>
    /// Level-specific ruleset: which segments are active and how they connect.
    /// </summary>
    [Serializable]
    public class TrackSegmentLibraryDefinition
    {
        public string Version;
        public string LevelName;
        public int    LevelNumber;
        public float  DifficultyRating;
        public int    LaneCount = 3;
        public float  LaneWidth = 2f;
        public string StartSegmentId;

        // --- Two-file split ---
        public string       SegmentRegistryFile;
        public List<string> ActiveSegmentTags = new List<string>();
        public List<string> ActiveSegmentIds  = new List<string>();

        // --- Segment data (populated directly or merged from registry at load time) ---
        public List<TrackSegmentDefinition> Segments    = new List<TrackSegmentDefinition>();
        public List<TrackSegmentConnection> Connections  = new List<TrackSegmentConnection>();
    }

    // =========================================================================
    // SplineSegmentData — event payload for SplineSegmentCreated
    // =========================================================================

    /// <summary>
    /// Data payload published with <c>TempleRunEvents.SplineSegmentCreated</c>.
    /// Carries spline geometry plus the full segment definition so spawners
    /// can access SpawnSlots, BlockedLanes, SpeedMultiplier, etc.
    /// </summary>
    [Serializable]
    public struct SplineSegmentData
    {
        public Vector3                  Point1;
        public Vector3                  Point2;
        public Direction                EndDirection;
        public TrackSegmentDefinition   Definition;

        public SplineSegmentData(Vector3 point1, Vector3 point2, Direction endDirection,
                                  TrackSegmentDefinition definition)
        {
            Point1       = point1;
            Point2       = point2;
            EndDirection = endDirection;
            Definition   = definition;
        }

        public override string ToString()
        {
            return $"SplineSegmentData: Point1={Point1}, Point2={Point2}, EndDirection={EndDirection}, Definition={Definition}";
        }

        // ----- Computed geometry (avoids duplicate math in every spawner) -----

        public Vector3 SegmentVector  => Point2 - Point1;
        public float   SegmentLength  => SegmentVector.magnitude;
        public Vector3 UnitDirection  => SegmentVector.normalized;
        public Vector3 Perpendicular  => Vector3.Cross(UnitDirection, Vector3.up).normalized;
    }

    // =========================================================================
    // TrackSegmentLibrary — runtime selection engine
    // =========================================================================

    /// <summary>
    /// Runtime data source for track segments during gameplay. Owns the segment pool,
    /// connections and lane configuration for a level and exposes them via
    /// <see cref="ISegmentPool"/>. The selection algorithm lives in an
    /// <see cref="Track.ISegmentSelector"/> (default
    /// <see cref="Track.WeightedDifficultySelector"/>); call
    /// <see cref="LoadFromResources"/> once per level to build the pool.
    /// </summary>
    public class TrackSegmentLibrary : ISegmentPool
    {
        private readonly TrackSegmentLibraryDefinition _definition;
        private readonly Dictionary<string, TrackSegmentDefinition> _segmentsById
            = new Dictionary<string, TrackSegmentDefinition>();
        private readonly Dictionary<string, List<string>> _connectionsByFromId
            = new Dictionary<string, List<string>>();

        public string LevelName        => _definition.LevelName;
        public int    LevelNumber      => _definition.LevelNumber;
        public float  DifficultyRating => _definition.DifficultyRating;
        public int    LaneCount        => _definition.LaneCount > 0 ? _definition.LaneCount : 3;
        public float  LaneWidth        => _definition.LaneWidth > 0f ? _definition.LaneWidth : 2f;
        public int    SegmentCount     => _definition.Segments.Count;

        // ---------------------------------------------------------------------
        // ISegmentPool — read-only data view consumed by ISegmentSelector.
        // Thin wrappers over the existing definition/maps; no behaviour of their own.
        // ---------------------------------------------------------------------

        /// <inheritdoc />
        public IReadOnlyList<TrackSegmentDefinition> Segments => _definition.Segments;

        /// <inheritdoc />
        public string StartSegmentId => _definition.StartSegmentId;

        /// <inheritdoc />
        public TrackSegmentDefinition ById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return _segmentsById.TryGetValue(id, out var segment) ? segment : null;
        }

        /// <inheritdoc />
        public IReadOnlyList<string> ConnectionsFrom(string id)
        {
            if (!string.IsNullOrWhiteSpace(id) &&
                _connectionsByFromId.TryGetValue(id, out var list))
                return list;
            return Array.Empty<string>();
        }

        public TrackSegmentLibrary(TrackSegmentLibraryDefinition definition)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));

            NormalizeSegments(_definition.Segments);

            foreach (var segment in _definition.Segments)
                if (!string.IsNullOrWhiteSpace(segment.Id))
                    _segmentsById[segment.Id] = segment;

            foreach (var connection in _definition.Connections)
            {
                if (string.IsNullOrWhiteSpace(connection.FromId) ||
                    string.IsNullOrWhiteSpace(connection.ToId))
                    continue;

                if (!_connectionsByFromId.TryGetValue(connection.FromId, out var list))
                {
                    list = new List<string>();
                    _connectionsByFromId[connection.FromId] = list;
                }
                list.Add(connection.ToId);
            }
        }

        /// <summary>
        /// Pre-computes derived fields on each segment so runtime code reads clean data.
        /// Called once at library construction — never inline in runtime callbacks.
        ///
        /// 3-point model: EntranceDistance (→ Pivot) + ExitDistance (→ Exit) = Length.
        /// For Straight: Pivot == Exit, so EntranceDistance = Length, ExitDistance = 0.
        /// For Left/Right/Either: both EntranceDistance and ExitDistance should be > 0.
        /// TeleportDistance defaults to ExitDistance * 0.5 when not specified.
        /// </summary>
        private static void NormalizeSegments(List<TrackSegmentDefinition> segments)
        {
            foreach (var seg in segments)
            {
                // EntranceDistance: default to Length (covers full segment for Straight)
                if (seg.EntranceDistance <= 0f)
                    seg.EntranceDistance = seg.Length;

                // Recompute Length from parts when ExitDistance is specified
                if (seg.ExitDistance > 0f)
                    seg.Length = seg.EntranceDistance + seg.ExitDistance;
                //seg.Direction = Enum.Parse<Direction>(seg.DirectionString);
                if (seg.Direction == Direction.Straight)
                    seg.TurnFailureDistance = float.MaxValue;
                else if (seg.TurnFailureDistance <= 0)
                    seg.TurnFailureDistance = seg.EntranceDistance + 1; // + Width/2 ;
                // Default TeleportDistance for segments that have an exit section
                if (seg.TeleportDistance <= 0f && seg.ExitDistance > 0f)
                    seg.TeleportDistance = seg.ExitDistance * 0.5f;
            }
        }

        // ---------------------------------------------------------------------
        // Factory methods
        // ---------------------------------------------------------------------

        public static TrackSegmentLibrary LoadFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            var def = JsonUtility.FromJson<TrackSegmentLibraryDefinition>(json);
            return def == null ? null : new TrackSegmentLibrary(def);
        }

        public static TrackSegmentLibrary LoadFromJson(string levelJson, string registryJson)
        {
            if (string.IsNullOrWhiteSpace(levelJson)) return null;
            var levelDef = JsonUtility.FromJson<TrackSegmentLibraryDefinition>(levelJson);
            if (levelDef == null) return null;

            if (!string.IsNullOrWhiteSpace(registryJson) && levelDef.Segments.Count == 0)
                MergeRegistrySegments(levelDef, registryJson);

            return new TrackSegmentLibrary(levelDef);
        }

        public static TrackSegmentLibrary LoadFromDefinition(
            TrackSegmentLibraryDefinition levelDef, string registryJson = null)
        {
            if (levelDef == null) return null;
            if (!string.IsNullOrWhiteSpace(registryJson) && levelDef.Segments.Count == 0)
                MergeRegistrySegments(levelDef, registryJson);
            return new TrackSegmentLibrary(levelDef);
        }

        public static TrackSegmentLibrary LoadFromResources(string levelResourcePath)
        {
            var levelAsset = Resources.Load<TextAsset>(levelResourcePath);
            if (levelAsset == null)
            {
                Debug.LogError($"[TrackSegmentLibrary] Level resource not found: '{levelResourcePath}'");
                return null;
            }

            var levelDef = JsonUtility.FromJson<TrackSegmentLibraryDefinition>(levelAsset.text);
            if (levelDef == null)
            {
                Debug.LogError($"[TrackSegmentLibrary] Failed to parse JSON: '{levelResourcePath}'");
                return null;
            }

            if (!string.IsNullOrWhiteSpace(levelDef.SegmentRegistryFile))
            {
                var registryAsset = Resources.Load<TextAsset>(levelDef.SegmentRegistryFile);
                if (registryAsset != null)
                    return LoadFromJson(levelAsset.text, registryAsset.text);

                Debug.LogWarning($"[TrackSegmentLibrary] Registry '{levelDef.SegmentRegistryFile}' " +
                                 $"not found; loading level standalone.");
            }

            return LoadFromJson(levelAsset.text);
        }

        // ---------------------------------------------------------------------
        // Selection
        // ---------------------------------------------------------------------
        //
        // The selection algorithm (GetStartSegment / SelectNext / SelectWeighted /
        // IsAllowed / IsInDifficultyRange) moved verbatim into
        // CrawfisSoftware.TempleRun.Track.WeightedDifficultySelector as part of the
        // ISegmentSelector extraction (M2). This class now supplies only the data,
        // via ISegmentPool. TrackManager holds an ISegmentSelector and calls it,
        // passing this library as the pool.

        // ---------------------------------------------------------------------
        // Private helpers
        // ---------------------------------------------------------------------

        private static void MergeRegistrySegments(
            TrackSegmentLibraryDefinition levelDef, string registryJson)
        {
            var registry = JsonUtility.FromJson<TrackSegmentRegistryDefinition>(registryJson);
            if (registry?.Segments == null || registry.Segments.Count == 0) return;

            bool filterByIds  = levelDef.ActiveSegmentIds?.Count  > 0;
            bool filterByTags = levelDef.ActiveSegmentTags?.Count > 0;

            foreach (var seg in registry.Segments)
            {
                if (string.IsNullOrWhiteSpace(seg.Id)) continue;

                bool include;

                if (seg.Id == levelDef.StartSegmentId)
                    include = true;
                else if (filterByIds)
                    include = levelDef.ActiveSegmentIds.Contains(seg.Id);
                else if (filterByTags)
                    include = seg.Tags != null && seg.Tags.Exists(t => levelDef.ActiveSegmentTags.Contains(t));
                else
                    include = true;

                if (include) levelDef.Segments.Add(seg);
            }
        }
    }
}
