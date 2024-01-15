using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    internal class LoadGameConfig : MonoBehaviour
    {
        [SerializeField] private TempleRunGameConfig _gameConfig;

        private void Start()
        {
            Blackboard.Instance.GameConfig = _gameConfig;
        }
    }
}