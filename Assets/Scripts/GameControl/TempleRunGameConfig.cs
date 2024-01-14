using System;
using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Create different files to control the overall game and difficulty
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "CrawfisSoftware/TempleRun/GameConfig")]
    public class TempleRunGameConfig : ScriptableObject
    {
        [SerializeField] public float InitialSpeed = 5f;
        [SerializeField] public float MaxSpeed = 8f;
        [SerializeField] public float Acceleration = 0.2f;
        [SerializeField] public int StartRunway = 10;
        [SerializeField] public float SafeTurnDistance = 5f;
        [SerializeField, Range(2, 92)] public int MinDistance = 3;
        [SerializeField, Range(2, 92)] public int MaxDistance = 9;
        // At this point, this file should be re-factored into several classes. Leaving it here to make a point.
        // For instance, the above can all be grouped into difficulty. The next two are more of a game style.
        // configurations will explode as we start looking at all combinations (e.g., single life, with Insane difficulty).
        [SerializeField] public float InputCoolDownForTurns = 1f;
        // This might be considered "difficulty", but a single life would be the original Temple Run where the goal is a
        //   high-score. A maximum value allows for infinite lives and perhaps a time-based game.
        [SerializeField] public int NumberOfLives = int.MaxValue;

        // A GREAT ScriptableObject has no behaviors (methods)!!! It is a file of data (an asset). This "class" is just a tool
        // to create these files and access the data in them.
    }
}