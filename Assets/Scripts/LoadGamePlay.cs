using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrawfisSoftware.TempleRun
{
    internal class LoadGamePlay : MonoBehaviour
    {
        [SerializeField] private TempleRunGameConfig _gameConfig;
        [SerializeField] private string _sceneName;

        private void Start()
        {
            Blackboard.Instance.GameConfig = _gameConfig;
            SceneManager.LoadSceneAsync(_sceneName, LoadSceneMode.Additive);
        }
    }
}