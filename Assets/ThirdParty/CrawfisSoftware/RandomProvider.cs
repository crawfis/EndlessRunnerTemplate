using UnityEngine;

namespace CrawfisSoftware.Unity3D.Utility
{
    /// <summary>
    /// Master control for consistent random number generation.
    /// </summary>
    public class RandomProvider : MonoBehaviour
    {
        [SerializeField] public int _currentSeed;
        [SerializeField] private bool _generateNewSeed = false;

        /// <summary>
        /// The System.Random instance that this class encapsulates.
        /// </summary>
        /// <remarks>Do not cache. You need to re-fetch after set-seed is called.</remarks>
        public System.Random RandomGenerator { get; private set; }

        protected virtual void Awake()
        {
            Initialize();
            //Blackboard.RandomProvider = this;
        }

        /// <summary>
        /// Set the random seed and create a new random generator.
        /// </summary>
        /// <param name="seed">The seed to use for a new random.</param>
        public void SetSeed(int seed)
        {
            this._currentSeed = seed;
            RandomGenerator = new System.Random(_currentSeed);
            UnityEngine.Random.InitState(_currentSeed);
        }

        public void SetAutoGenerateSeed(bool newValue)
        {
            _generateNewSeed = newValue;
        }

        /// <summary>
        /// Create a new random generator using this random to generate the seed.
        /// </summary>
        /// <returns>A new System.Random.</returns>
        public System.Random NewGenerator()
        {
            return new System.Random(RandomGenerator.Next());
        }

        private void Initialize()
        {
            int newSeed = _currentSeed;
            if (_generateNewSeed)
                newSeed = new System.Random().Next();
            SetSeed(newSeed);
        }
    }
}