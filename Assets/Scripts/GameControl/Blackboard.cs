using CrawfisSoftware.Unity3D.Utility;
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
        internal DistanceTracker DistanceTracker { get; set; }
        public float TrackWidthOffset { get; set; } = 1f;
        public float TileLength { get; set; } = 4f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }
    }
}