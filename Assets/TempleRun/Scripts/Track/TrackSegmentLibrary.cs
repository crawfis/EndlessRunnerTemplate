using System;
using System.Collections.Generic;
using UnityEngine;

using CrawfisSoftware.TempleRun.GameConfig;
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

        /// <summary>Turn direction. Set directly from the authoring SO (or inline construction);
        /// no string parsing is involved. Length below is derived from it by Normalize().</summary>
        public Direction    Direction;

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
        public float ToPivotDistance = 0f;

        /// <summary>
        /// Distance past the Entrance beyond which a required turn is considered failed.
        /// Set by NormalizeSegments(): float.MaxValue for Straight (never fails), otherwise
        /// ToPivotDistance + 1 when left at 0.
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
    /// Level-specific ruleset: which segments are active and how they connect.
    /// Built at runtime from a <see cref="TrackLevelSO"/>; the segment pool is already
    /// tag/id-filtered from the registry by the time this reaches the track system.
    /// </summary>
    public class TrackSegmentLibraryDefinition
    {
        public string LevelName;
        public int    LevelNumber;
        public float  DifficultyRating;
        public int    LaneCount = 3;
        public float  LaneWidth = 2f;
        public string StartSegmentId;

        public List<string> ActiveSegmentTags = new List<string>();
        public List<string> ActiveSegmentIds  = new List<string>();

        // --- Segment data (merged + filtered from the registry at build time) ---
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
    /// <see cref="Track.WeightedDifficultySelector"/>); construct one from a
    /// <see cref="TrackSegmentLibraryDefinition"/> (built by <see cref="TrackLibraryLoader"/>) per level.
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
        /// 3-point model: ToPivotDistance (→ Pivot) + ExitDistance (→ Exit) = Length.
        /// For Straight: Pivot == Exit, so ToPivotDistance = Length, ExitDistance = 0.
        /// For Left/Right/Either: both ToPivotDistance and ExitDistance should be > 0.
        /// TeleportDistance defaults to ExitDistance * 0.5 when not specified.
        /// </summary>
        private static void NormalizeSegments(List<TrackSegmentDefinition> segments)
        {
            foreach (var seg in segments)
                Normalize(seg);
        }

        /// <summary>
        /// Turns one authored definition into a well-formed one. This is the single boundary where
        /// segment data becomes trustworthy, so every construction path must pass through it —
        /// including definitions built inline at runtime rather than loaded from the registry.
        /// Downstream code (builders, controllers) may then assume the invariants below hold
        /// instead of re-checking them.
        ///
        /// Invariants established here (Direction is already set by the caller and every rule
        /// below branches on it):
        ///   Length               == ToPivotDistance + ExitDistance
        ///   Straight             => ExitDistance == 0 and TurnFailureDistance == MaxValue
        ///   Left/Right/Either    => ExitDistance  > 0 and TurnFailureDistance strictly &lt; Length
        ///   TeleportDistance      > 0 exactly when ExitDistance > 0
        /// </summary>
        public static void Normalize(TrackSegmentDefinition seg)
        {
            // ToPivotDistance: default to Length (covers full segment for Straight)
            if (seg.ToPivotDistance <= 0f)
                seg.ToPivotDistance = seg.Length;

            if (seg.Direction == Direction.Straight)
            {
                // Pivot coincides with Exit, so a Straight has no exit section by definition.
                if (seg.ExitDistance != 0f)
                {
                    Debug.LogError($"[TrackSegmentLibrary] Straight segment '{seg.Id}' declares " +
                                   $"ExitDistance {seg.ExitDistance}. A Straight ends at its pivot " +
                                   $"and its exit section is ignored when building geometry. " +
                                   $"Remove ExitDistance, or give the segment a turn Direction.");
                    seg.ExitDistance = 0f;
                }
                seg.TurnFailureDistance = float.MaxValue;
            }
            else
            {
                // A turn needs somewhere to run after the pivot. Without it the exit sub-spline
                // collapses to a point: no direction to face, no length to build along.
                if (seg.ExitDistance <= 0f)
                {
                    Debug.LogError($"[TrackSegmentLibrary] Turn segment '{seg.Id}' declares " +
                                   $"ExitDistance {seg.ExitDistance}. A turn must have a post-pivot " +
                                   $"exit section; using {TempleRunConstants.MinimumTurnExitDistance}. " +
                                   $"Set ExitDistance > 0 in the registry.");
                    seg.ExitDistance = TempleRunConstants.MinimumTurnExitDistance;
                }
            }

            // Length is always the sum of the two sections.
            seg.Length = seg.ToPivotDistance + seg.ExitDistance;

            if (seg.Direction != Direction.Straight)
            {
                if (seg.TurnFailureDistance <= 0)
                    seg.TurnFailureDistance = seg.ToPivotDistance + 1; // + Width/2 ;

                // The failure point must sit strictly inside the segment. SegmentExited fires
                // at Length and immediately re-arms TurnCollisionDetector for the next segment,
                // so a failure distance at or past Length is never reached and a missed turn
                // goes undetected. Segments with a short ExitDistance (e.g. ToPivot 15 /
                // Exit 1 => Length 16, default failure 16) hit this every time.
                float latestFailure = seg.Length - TempleRunConstants.TurnFailureMarginBeforeExit;
                if (seg.TurnFailureDistance > latestFailure)
                    seg.TurnFailureDistance = latestFailure;
            }

            // Default TeleportDistance for segments that have an exit section
            if (seg.TeleportDistance <= 0f && seg.ExitDistance > 0f)
                seg.TeleportDistance = seg.ExitDistance * 0.5f;
        }

        // The library is constructed directly from a resolved definition — see
        // TrackLibraryLoader, which reads the authoring SOs, merges + tag/id-filters the pool,
        // and calls the constructor (which normalizes).

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
        // Registry merge
        // ---------------------------------------------------------------------

        /// <summary>
        /// Appends the level's active segments to <paramref name="levelDef"/>, selecting from the
        /// registry pool by the level's id/tag filter. The start segment is always included.
        /// The <paramref name="registrySegments"/> are fresh runtime definitions (one per authored
        /// asset), so they may be added directly and later normalized in place.
        /// </summary>
        public static void MergeRegistry(
            TrackSegmentLibraryDefinition levelDef, IReadOnlyList<TrackSegmentDefinition> registrySegments)
        {
            bool filterByIds  = levelDef.ActiveSegmentIds?.Count  > 0;
            bool filterByTags = levelDef.ActiveSegmentTags?.Count > 0;

            foreach (var seg in registrySegments)
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
