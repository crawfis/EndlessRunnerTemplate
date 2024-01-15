using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrawfisSoftware.TempleRun
{
    public class LoadSceneAdditively : MonoBehaviour
    {
        [SerializeField] private string _sceneName;

        private void Start()
        {
            SceneManager.LoadSceneAsync(_sceneName, LoadSceneMode.Additive);
        }
    }
}