using System.Collections.Generic;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// The reading layer between the authored track ScriptableObjects and the runtime track system.
    /// Given the level registry and the selected level number, it reads the (inert) SOs and produces
    /// a runtime <see cref="TrackSegmentLibrary"/>.
    ///
    /// This is the only code that reads the authoring SOs — keeping the three concerns separate:
    /// <b>input</b> (the SO assets, pure data), <b>reading</b> (here), and <b>using</b> the data
    /// (TrackManager and the geometry/spawn consumers, which touch only the runtime library).
    /// </summary>
    public static class TrackLibraryLoader
    {
        /// <summary>
        /// Resolves the <see cref="TrackLevelSO"/> for <paramref name="levelNumber"/> from the
        /// registry and builds its runtime library. Returns null when no level matches (e.g. none
        /// selected) so callers can fall back to procedural generation.
        /// </summary>
        public static TrackSegmentLibrary Load(TrackLevelRegistrySO registry, int levelNumber)
        {
            var level = FindLevel(registry, levelNumber);
            if (level == null) return null;

            return new TrackSegmentLibrary(ToLibraryDefinition(level));
        }

        private static TrackLevelSO FindLevel(TrackLevelRegistrySO registry, int levelNumber)
        {
            foreach (var level in registry.Levels)
                if (level != null && level.LevelNumber == levelNumber)
                    return level;
            return null;
        }

        /// <summary>
        /// Reads a level SO into a runtime definition: metadata plus the tag/id-filtered segment pool.
        /// The definition is fresh and un-normalized; constructing a <see cref="TrackSegmentLibrary"/>
        /// from it runs <see cref="TrackSegmentLibrary.Normalize"/>.
        /// </summary>
        public static TrackSegmentLibraryDefinition ToLibraryDefinition(TrackLevelSO level)
        {
            var definition = new TrackSegmentLibraryDefinition
            {
                LevelName         = level.LevelName,
                LevelNumber       = level.LevelNumber,
                DifficultyRating  = level.DifficultyRating,
                LaneCount         = level.LaneCount,
                LaneWidth         = level.LaneWidth,
                StartSegmentId    = level.StartSegmentId,
                ActiveSegmentTags = new List<string>(level.ActiveSegmentTags),
                ActiveSegmentIds  = new List<string>(level.ActiveSegmentIds),
            };

            var pool = new List<TrackSegmentDefinition>(level.Registry.Segments.Length);
            foreach (var segment in level.Registry.Segments)
                pool.Add(ToDefinition(segment));

            TrackSegmentLibrary.MergeRegistry(definition, pool);
            return definition;
        }

        /// <summary>
        /// Reads one segment SO into a fresh, mutable runtime definition. Lists are copied so the
        /// definition owns its own state and normalization never writes back into the asset.
        /// </summary>
        public static TrackSegmentDefinition ToDefinition(TrackSegmentSO so)
        {
            return new TrackSegmentDefinition
            {
                Id                  = so.Id,
                Direction           = so.Direction,
                Weight              = so.Weight,
                MaxRepeat           = so.MaxRepeat,
                DifficultyRating    = so.DifficultyRating,
                Tags                = new List<string>(so.Tags),
                ToPivotDistance     = so.ToPivotDistance,
                ExitDistance        = so.ExitDistance,
                TeleportDistance    = so.TeleportDistance,
                TurnFailureDistance = so.TurnFailureDistance,
                TurnRadius          = so.TurnRadius,
                Role                = so.Role,
                SpeedMultiplier     = so.SpeedMultiplier,
                BlockedLanes        = new List<int>(so.BlockedLanes),
                LaneHeights         = new List<float>(so.LaneHeights),
                ActiveLanes         = new List<int>(so.ActiveLanes),
                SpawnMode           = so.SpawnMode,
                SpawnSlots          = new List<SpawnSlotDefinition>(so.SpawnSlots),
                VisualTheme         = so.VisualTheme,
                SpawnSeed           = so.SpawnSeed,
            };
        }
    }
}
