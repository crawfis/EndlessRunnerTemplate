using CrawfisSoftware.Unity3D.Utility;
using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    public class Blackboard : MonoBehaviour
    {
        public static Blackboard Instance { get; private set; }
        [SerializeField] private RandomProviderFromList _randomProvider;
        public System.Random MasterRandom { get { return _randomProvider.RandomGenerator; } }
        public TempleRunGameConfig GameConfig { get; set; }
        internal DistanceTracker DistanceTracker { get; set; }

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