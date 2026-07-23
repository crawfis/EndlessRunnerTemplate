using System.Collections.Generic;

using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Authoring asset for one level's track ruleset: lane configuration, the start segment,
    /// and a tag/id filter selecting which segments from the shared
    /// <see cref="TrackSegmentRegistrySO"/> make up this level's pool.
    ///
    /// Resolved by <see cref="TrackLevelRegistrySO"/> from a level number, then read by
    /// <see cref="TrackLibraryLoader"/>. Pure authoring data — no logic.
    ///
    /// Create via: Assets > Create > CrawfisSoftware > TempleRun > Track Level
    /// </summary>
    [CreateAssetMenu(
        fileName = "TrackLevel",
        menuName = "CrawfisSoftware/TempleRun/Track Level",
        order = 212)]
    public class TrackLevelSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("The level number GameFlow selects by. Must match the LevelNumber this asset is " +
                 "registered under in the TrackLevelRegistrySO.")]
        public int    LevelNumber;
        public string LevelName;
        public float  DifficultyRating;

        [Header("Lanes")]
        public int   LaneCount = 3;
        public float LaneWidth = 2f;

        [Header("Segment Pool")]
        [Tooltip("The shared segment pool this level draws from.")]
        public TrackSegmentRegistrySO Registry;

        [Tooltip("Id of the segment the level always starts on.")]
        public string StartSegmentId;

        [Tooltip("Segments whose tags intersect this list are included. Used when no explicit ids are given.")]
        public List<string> ActiveSegmentTags = new List<string>();

        [Tooltip("Explicit segment ids to include. Takes precedence over the tag filter when non-empty.")]
        public List<string> ActiveSegmentIds = new List<string>();
    }
}
