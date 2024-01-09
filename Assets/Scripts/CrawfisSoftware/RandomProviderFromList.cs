using UnityEngine;

namespace CrawfisSoftware.Unity3D.Utility
{
    public class RandomProviderFromList : RandomProvider
    {
        [SerializeField] public IntListScriptable _fixedSeedList;
        //[field: SerializeField] public IntListScriptable FixedSeedList { get; set; }
        [SerializeField] protected int _seedIndex = -1;

        private System.Random _random = new System.Random();

        public int SeedIndex
        {
            get { return _seedIndex; }
            set { _seedIndex = value; SetSeedFromList(); }
        }

        public void SetSeedFromList()
        {
            int seed = 0;
            if (_fixedSeedList == null || _fixedSeedList.List == null || _seedIndex > _fixedSeedList.List.Count)
            {
                _seedIndex = -1;
                seed = _random.Next();
            }
            else if(_seedIndex == -1)
            {
                // Randomly select from list.
                _seedIndex = _random.Next(0, _fixedSeedList.List.Count);
            }
            else
            {
                seed = _fixedSeedList.List[_seedIndex];
            }
            SetSeed(seed);
        }

        protected override void Awake()
        {
            //SetSeedFromList();
            //SetAutoGenerateSeed(false);
            base.Awake();
        }
    }
}