using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Maps a selected level number to its <see cref="TrackLevelSO"/> ruleset. This is the seam that
    /// keeps GameFlow ignorant of tracks: GameFlow only ever names an <c>int</c> ("level 3"), and
    /// this TempleRun-owned asset turns that number into a track. Int keys are deliberate — a string
    /// key would be typo-prone with no compile-time check.
    ///
    /// Pure authoring data; <see cref="TrackLibraryLoader"/> is what reads it.
    ///
    /// Create via: Assets > Create > CrawfisSoftware > TempleRun > Track Level Registry
    /// </summary>
    [CreateAssetMenu(
        fileName = "TrackLevelRegistry",
        menuName = "CrawfisSoftware/TempleRun/Track Level Registry",
        order = 213)]
    public class TrackLevelRegistrySO : ScriptableObject
    {
        [Tooltip("All levels, resolved by their LevelNumber. The order here is not significant.")]
        public TrackLevelSO[] Levels;
    }
}
