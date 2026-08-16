using CrawfisSoftware.Config;

using UnityEngine;

namespace CrawfisSoftware.GameFlow.Config
{
    /// <summary>
    /// Defines a single level in the game. Each level combines a unique
    /// environment/scene with its own set of difficulty variants.
    ///    Dependencies: DifficultyConfig (from _Common)
    /// </summary>
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "CrawfisSoftware/GameFlow/LevelConfig")]
    public class LevelConfig : ScriptableObject
    {
        [Header("Display")]
        [Tooltip("Name shown in the level selector UI")]
        public string LevelName;

        [TextArea(2, 4)]
        [Tooltip("Description shown in the level selector UI")]
        public string Description;

        [Tooltip("Icon/thumbnail shown in the level selector card")]
        public Sprite Thumbnail;

        [Tooltip("Controls display ordering in the level selector (ascending)")]
        public int SortOrder;

        [Header("Gameplay")]
        [Tooltip("The gameplay scene to load for this level")]
        public string GameplaySceneName = "TempleRunGameplay";

        [Tooltip("This level's difficulty variants, one per difficulty name. Selecting the level " +
                 "publishes the whole set; the player's chosen difficulty picks one from it by " +
                 "DifficultyName. A level that offers only one variant is a set of one.")]
        public DifficultyConfig[] Difficulties;

        [Header("Track Generation")]
        [Tooltip("The level number published when this level is selected. Gameplay maps it to a track " +
                 "ruleset; GameFlow itself knows nothing of tracks.")]
        public int LevelNumber;

        [Header("Unlock Requirements")]
        [Tooltip("Cumulative best score threshold required to unlock. 0 = always unlocked.")]
        public float UnlockScoreThreshold;

        [Tooltip("Displayed to the user when the level is locked (e.g., 'Score 500 total points')")]
        public string UnlockRequirementDescription;
    }
}
