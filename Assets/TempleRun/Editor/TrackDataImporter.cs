using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using CrawfisSoftware.GameFlow.Config;

using UnityEditor;
using UnityEngine;

namespace CrawfisSoftware.TempleRun.Editor
{
    /// <summary>
    /// One-shot migration: converts the legacy track JSON in Assets/TempleRun/Resources into
    /// ScriptableObject assets (one <see cref="TrackSegmentSO"/> per segment, one
    /// <see cref="TrackSegmentRegistrySO"/> pool, one <see cref="TrackLevelSO"/> per level),
    /// rewires the <see cref="LevelConfig"/> assets to reference the new level SOs, and verifies
    /// that every segment still normalizes to the same Length and TurnFailureDistance.
    ///
    /// Legacy JSON field names (DirectionString, SegmentRegistryFile, ...) live only in the
    /// throwaway DTOs below — they never touch the runtime model.
    ///
    /// Run once via: CrawfisSoftware > Track > Import JSON -> ScriptableObjects.
    /// After verifying the generated assets, delete Assets/TempleRun/Resources by hand.
    /// </summary>
    public static class TrackDataImporter
    {
        private const string ResourcesDir = "Assets/TempleRun/Resources";
        private const string OutputDir    = "Assets/TempleRun/Scriptables/Track";
        private const string SegmentsDir  = OutputDir + "/Segments";
        private const string RegistryJson = "TrackSegments_Registry";

        [MenuItem("CrawfisSoftware/Track/Import JSON -> ScriptableObjects")]
        public static void Import()
        {
            var registryJsonPath = $"{ResourcesDir}/{RegistryJson}.json";
            if (!File.Exists(registryJsonPath))
            {
                Debug.LogError($"[TrackDataImporter] Registry JSON not found at '{registryJsonPath}'. " +
                               $"Nothing to import.");
                return;
            }

            EnsureFolder(OutputDir);
            EnsureFolder(SegmentsDir);

            // --- Segments + registry pool -------------------------------------------------
            var registryDto = JsonUtility.FromJson<JsonRegistry>(File.ReadAllText(registryJsonPath));
            var segmentAssets = new List<TrackSegmentSO>(registryDto.Segments.Count);
            var segmentById   = new Dictionary<string, TrackSegmentSO>();

            foreach (var dto in registryDto.Segments)
            {
                var so = ScriptableObject.CreateInstance<TrackSegmentSO>();
                ApplyToSegment(dto, so);
                AssetDatabase.CreateAsset(so, $"{SegmentsDir}/{dto.Id}.asset");
                segmentAssets.Add(so);
                segmentById[dto.Id] = so;
            }

            var registrySO = ScriptableObject.CreateInstance<TrackSegmentRegistrySO>();
            registrySO.Segments = segmentAssets.ToArray();
            AssetDatabase.CreateAsset(registrySO, $"{OutputDir}/TrackSegmentRegistry.asset");

            // --- Level rulesets -----------------------------------------------------------
            var levelSOByResourceName = new Dictionary<string, TrackLevelSO>();
            foreach (var levelJsonPath in Directory.GetFiles(ResourcesDir, "TrackLevel_*.json"))
            {
                var resourceName = Path.GetFileNameWithoutExtension(levelJsonPath);
                var levelDto = JsonUtility.FromJson<JsonLevel>(File.ReadAllText(levelJsonPath));

                var levelSO = ScriptableObject.CreateInstance<TrackLevelSO>();
                levelSO.LevelName         = levelDto.LevelName;
                levelSO.LevelNumber       = levelDto.LevelNumber;
                levelSO.DifficultyRating  = levelDto.DifficultyRating;
                levelSO.LaneCount         = levelDto.LaneCount > 0 ? levelDto.LaneCount : 3;
                levelSO.LaneWidth         = levelDto.LaneWidth > 0f ? levelDto.LaneWidth : 2f;
                levelSO.Registry          = registrySO;
                levelSO.StartSegmentId    = levelDto.StartSegmentId;
                levelSO.ActiveSegmentTags = levelDto.ActiveSegmentTags ?? new List<string>();
                levelSO.ActiveSegmentIds  = levelDto.ActiveSegmentIds  ?? new List<string>();

                AssetDatabase.CreateAsset(levelSO, $"{OutputDir}/{resourceName}.asset");
                levelSOByResourceName[resourceName] = levelSO;
            }

            // Level registry: the int-keyed map GameFlow's LevelNumber resolves through.
            var levelRegistry = ScriptableObject.CreateInstance<TrackLevelRegistrySO>();
            levelRegistry.Levels = levelSOByResourceName.Values.ToArray();
            AssetDatabase.CreateAsset(levelRegistry, $"{OutputDir}/TrackLevelRegistry.asset");

            AssetDatabase.SaveAssets();

            RewireLevelConfigs(levelSOByResourceName);
            Verify(registryDto, segmentById);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[TrackDataImporter] Imported {segmentAssets.Count} segments, " +
                      $"{levelSOByResourceName.Count} levels into '{OutputDir}'. " +
                      $"Assign TrackLevelRegistry to TrackManager._trackLevels, verify the assets, " +
                      $"then delete '{ResourcesDir}'.");
        }

        /// <summary>Maps a legacy segment DTO onto an authoring SO, applying the same
        /// "ToPivotDistance defaults to Length" rule Normalize uses so a Straight authored with
        /// only Length round-trips correctly (the SO has no Length field).</summary>
        private static void ApplyToSegment(JsonSegment dto, TrackSegmentSO so)
        {
            so.Id               = dto.Id;
            so.Direction        = (Direction)Enum.Parse(typeof(Direction), dto.DirectionString, ignoreCase: true);
            so.Weight           = dto.Weight;
            so.MaxRepeat        = dto.MaxRepeat;
            so.DifficultyRating = dto.DifficultyRating;
            so.Tags             = dto.Tags ?? new List<string>();
            so.ToPivotDistance  = dto.ToPivotDistance > 0f ? dto.ToPivotDistance : dto.Length;
            so.ExitDistance     = dto.ExitDistance;
            so.TeleportDistance = dto.TeleportDistance;
            so.TurnFailureDistance = dto.TurnFailureDistance;
            so.TurnRadius       = dto.TurnRadius;
        }

        /// <summary>
        /// Sets each LevelConfig's new int LevelNumber from its old TrackLevelResourcePath. The old
        /// string field is read straight from the asset YAML (its C# field is gone), mapped to the
        /// generated TrackLevelSO, and its LevelNumber written back via SerializedObject (which also
        /// drops the stale line).
        /// </summary>
        private static void RewireLevelConfigs(IReadOnlyDictionary<string, TrackLevelSO> levelSOByResourceName)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:LevelConfig"))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var match = Regex.Match(File.ReadAllText(assetPath), @"TrackLevelResourcePath:\s*(\S+)");
                if (!match.Success) continue;

                var resourceName = match.Groups[1].Value;
                if (!levelSOByResourceName.TryGetValue(resourceName, out var levelSO))
                {
                    Debug.LogWarning($"[TrackDataImporter] LevelConfig '{assetPath}' referenced " +
                                     $"'{resourceName}', which has no matching TrackLevelSO.");
                    continue;
                }

                var config = AssetDatabase.LoadAssetAtPath<LevelConfig>(assetPath);
                var so = new SerializedObject(config);
                so.FindProperty("LevelNumber").intValue = levelSO.LevelNumber;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(config);
                Debug.Log($"[TrackDataImporter] Wired LevelConfig '{config.name}' -> level {levelSO.LevelNumber}.");
            }
        }

        /// <summary>
        /// Regression gate: every segment must normalize to the same Length and TurnFailureDistance
        /// whether it comes from the legacy JSON or the new SO.
        /// </summary>
        private static void Verify(JsonRegistry registryDto, IReadOnlyDictionary<string, TrackSegmentSO> segmentById)
        {
            int mismatches = 0;
            foreach (var dto in registryDto.Segments)
            {
                var expected = FromJson(dto);
                TrackSegmentLibrary.Normalize(expected);

                var actual = TrackLibraryLoader.ToDefinition(segmentById[dto.Id]);
                TrackSegmentLibrary.Normalize(actual);

                if (expected.Length != actual.Length ||
                    expected.TurnFailureDistance != actual.TurnFailureDistance)
                {
                    mismatches++;
                    Debug.LogError($"[TrackDataImporter] '{dto.Id}' mismatch — " +
                                   $"JSON (Length {expected.Length}, TFD {expected.TurnFailureDistance}) vs " +
                                   $"SO (Length {actual.Length}, TFD {actual.TurnFailureDistance}).");
                }
            }

            if (mismatches == 0)
                Debug.Log($"[TrackDataImporter] Verified: all {registryDto.Segments.Count} segments " +
                          $"normalize to identical Length and TurnFailureDistance.");
            else
                Debug.LogError($"[TrackDataImporter] {mismatches} segment(s) failed verification.");
        }

        /// <summary>Builds a runtime definition directly from the legacy DTO (the reference side of
        /// the verification), mirroring the importer's field mapping.</summary>
        private static TrackSegmentDefinition FromJson(JsonSegment dto)
        {
            return new TrackSegmentDefinition
            {
                Id                  = dto.Id,
                Direction           = (Direction)Enum.Parse(typeof(Direction), dto.DirectionString, ignoreCase: true),
                ToPivotDistance     = dto.ToPivotDistance > 0f ? dto.ToPivotDistance : dto.Length,
                ExitDistance        = dto.ExitDistance,
                TeleportDistance    = dto.TeleportDistance,
                TurnFailureDistance = dto.TurnFailureDistance,
            };
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            var leaf   = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        // ---------------------------------------------------------------------------------
        // Legacy JSON DTOs — the only place the old field names survive.
        // Fields are assigned by JsonUtility via reflection, hence the CS0649 suppression.
        // ---------------------------------------------------------------------------------
#pragma warning disable CS0649

        [Serializable]
        private class JsonSegment
        {
            public string Id;
            public string DirectionString;
            public float  Length;
            public float  ToPivotDistance;
            public float  ExitDistance;
            public float  TeleportDistance;
            public float  TurnFailureDistance;
            public float  TurnRadius;
            public float  Weight = 1f;
            public int    MaxRepeat;
            public float  DifficultyRating;
            public List<string> Tags;
        }

        [Serializable]
        private class JsonRegistry
        {
            public List<JsonSegment> Segments = new List<JsonSegment>();
        }

        [Serializable]
        private class JsonLevel
        {
            public string LevelName;
            public int    LevelNumber;
            public float  DifficultyRating;
            public int    LaneCount = 3;
            public float  LaneWidth = 2f;
            public string StartSegmentId;
            public List<string> ActiveSegmentTags;
            public List<string> ActiveSegmentIds;
        }
#pragma warning restore CS0649
    }
}
