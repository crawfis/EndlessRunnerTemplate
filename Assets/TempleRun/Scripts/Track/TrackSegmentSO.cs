using System.Collections.Generic;

using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Authoring asset for a single track segment. One ScriptableObject per segment,
    /// gathered into a <see cref="TrackSegmentRegistrySO"/> pool.
    ///
    /// Pure authoring data — no logic. <see cref="TrackLibraryLoader"/> reads it into a fresh,
    /// mutable <see cref="TrackSegmentDefinition"/> that <see cref="TrackSegmentLibrary.Normalize"/>
    /// then fills in, so the asset itself is never mutated at runtime.
    ///
    /// Create via: Assets > Create > CrawfisSoftware > TempleRun > Track Segment
    /// </summary>
    [CreateAssetMenu(
        fileName = "TrackSegment",
        menuName = "CrawfisSoftware/TempleRun/Track Segment",
        order = 210)]
    public class TrackSegmentSO : ScriptableObject
    {
        [Header("Core")]
        [Tooltip("Unique identifier used for connections, repeat tracking and start selection.")]
        public string Id;

        [Tooltip("Turn direction. A real enum — the Inspector shows a dropdown, and no string " +
                 "parsing or field-name drift is possible (the failure mode this migration removed).")]
        public Direction Direction = Direction.Straight;

        [Header("Selection")]
        [Tooltip("Relative weight in weighted-random selection.")]
        public float Weight = 1f;

        [Tooltip("Maximum consecutive repeats allowed (0 = unlimited).")]
        public int MaxRepeat = 0;

        [Tooltip("Difficulty rating used by difficulty-gated selectors.")]
        public float DifficultyRating = 0f;

        [Tooltip("Tags used by per-level tag filters to include this segment in a level's pool.")]
        public List<string> Tags = new List<string>();

        [Header("Geometry (Entrance -> Pivot -> Exit)")]
        [Tooltip("Distance from Entrance to Pivot. For a Straight this covers the whole segment. " +
                 "Left 0 means 'use Length'; Normalize resolves it.")]
        public float ToPivotDistance = 0f;

        [Tooltip("Distance from Pivot to Exit, run through after turning. 0 for Straight; " +
                 "> 0 for turns. Normalize sets Length = ToPivotDistance + ExitDistance.")]
        public float ExitDistance = 0f;

        [Tooltip("Distance from Pivot where the player lands after the turn animation. " +
                 "0 means 'use ExitDistance * 0.5'; Normalize resolves it.")]
        public float TeleportDistance = 0f;

        [Tooltip("Distance past Entrance beyond which a required turn is failed. 0 means " +
                 "'use ToPivotDistance + 1, clamped inside the segment'; Normalize resolves it.")]
        public float TurnFailureDistance = 0f;

        [Tooltip("Optional turn radius for rounded corners (read by ArcTurnBuilder). 0 = hard 90 degrees.")]
        public float TurnRadius = 0f;

        [Header("Role & Speed")]
        [Tooltip("Segment role (Normal, Opening, Checkpoint, ...). See SegmentRoles.")]
        public string Role = SegmentRoles.Normal;

        [Tooltip("Speed multiplier applied while on this segment.")]
        public float SpeedMultiplier = 1f;

        [Header("Lane Overrides")]
        public List<int>   BlockedLanes = new List<int>();
        public List<float> LaneHeights  = new List<float>();
        public List<int>   ActiveLanes  = new List<int>();

        [Header("Spawning")]
        [Tooltip("Spawn mode (Procedural, Preset, Hybrid). See SpawnModes.")]
        public string SpawnMode = SpawnModes.Procedural;

        [Tooltip("Explicit spawn slots for Preset / Hybrid modes.")]
        public List<SpawnSlotDefinition> SpawnSlots = new List<SpawnSlotDefinition>();

        [Header("Visual / Determinism")]
        public string VisualTheme;
        public int    SpawnSeed;
    }
}
