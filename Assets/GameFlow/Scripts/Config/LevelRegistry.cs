using UnityEngine;

namespace CrawfisSoftware.GameFlow.Config
{
    /// <summary>
    /// Registry of all available levels in the game.
    /// Designers add LevelConfig assets to this array to include them in the level selector.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelRegistry", menuName = "CrawfisSoftware/GameFlow/LevelRegistry")]
    public class LevelRegistry : ScriptableObject
    {
        [Tooltip("All levels in the game. Sort order is determined by each LevelConfig.SortOrder.")]
        public LevelConfig[] Levels;

        /// <summary>
        /// Returns a copy of levels sorted by SortOrder for display.
        /// </summary>
        public LevelConfig[] GetSortedLevels()
        {
            var sorted = new LevelConfig[Levels.Length];
            System.Array.Copy(Levels, sorted, Levels.Length);
            System.Array.Sort(sorted, (a, b) => a.SortOrder.CompareTo(b.SortOrder));
            return sorted;
        }
    }
}
