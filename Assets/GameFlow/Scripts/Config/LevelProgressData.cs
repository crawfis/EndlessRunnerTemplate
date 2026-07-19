using System;
using System.Collections.Generic;

namespace CrawfisSoftware.GameFlow.Config
{
    /// <summary>
    /// Serializable container for level progress persistence.
    /// Stores best scores and unlock status per level.
    /// Serialized as JSON to PlayerPrefs (extensible to UGS Cloud Save).
    /// </summary>
    [Serializable]
    public class LevelProgressData
    {
        public List<LevelScoreEntry> LevelScores = new List<LevelScoreEntry>();

        [Serializable]
        public class LevelScoreEntry
        {
            public string LevelName;
            public float BestScore;
            public bool IsUnlocked;
        }

        public float GetBestScore(string levelName)
        {
            var entry = LevelScores.Find(e => e.LevelName == levelName);
            return entry?.BestScore ?? 0f;
        }

        public bool IsLevelUnlocked(string levelName)
        {
            var entry = LevelScores.Find(e => e.LevelName == levelName);
            return entry?.IsUnlocked ?? false;
        }

        public void SetBestScore(string levelName, float score)
        {
            var entry = GetOrCreateEntry(levelName);
            if (score > entry.BestScore)
                entry.BestScore = score;
        }

        public void UnlockLevel(string levelName)
        {
            var entry = GetOrCreateEntry(levelName);
            entry.IsUnlocked = true;
        }

        private LevelScoreEntry GetOrCreateEntry(string levelName)
        {
            var entry = LevelScores.Find(e => e.LevelName == levelName);
            if (entry == null)
            {
                entry = new LevelScoreEntry { LevelName = levelName };
                LevelScores.Add(entry);
            }
            return entry;
        }
    }
}
