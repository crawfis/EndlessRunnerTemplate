using CrawfisSoftware.Utility;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Space for "global" variables using a singleton.
    /// </summary>
    public class Blackboard : MonoBehaviour
    {
        [SerializeField] private RandomProviderFromList _randomProvider;
        public static Blackboard Instance { get; private set; }
        public System.Random MasterRandom { get { return _randomProvider.RandomGenerator; } }
        public TempleRunGameConfig GameConfig { get; set; }
        public DistanceTracker DistanceTracker { get; set; }
        public float TrackWidthOffset { get; set; } = 1f;
        public float TileLength { get; set; } = 4f;
        public float CurrentSpeed { get; set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

#if UNITY_EDITOR
        [InitializeOnEnterPlayMode()]
        public static void InitializeOnPlay()
        {
            Instance = null;
        }
#endif
    }
}