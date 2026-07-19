using CrawfisSoftware.Config;

using System;
using UnityEngine;

namespace CrawfisSoftware.TempleRun.GameConfig
{
    /// <summary>
    /// Create different files to control the overall game and difficulty
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "CrawfisSoftware/TempleRun/GameConfigs")]
    public class TempleRunGameConfig : ScriptableObject
    {
        [SerializeField] public DifficultyConfig[] DifficultyConfigs;
    }
}