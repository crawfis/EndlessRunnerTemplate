using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Loads a configuration ScriptableObject and puts in on the Blackboard
    /// Dependencies: Blackboad, TempleRunGameConfig
    /// </summary>
    public class LoadGameConfig : MonoBehaviour
    {
        [SerializeField] private TempleRunGameConfig _gameConfig;

        private void Start()
        {
            Blackboard.Instance.GameConfig = _gameConfig;
        }
    }
}