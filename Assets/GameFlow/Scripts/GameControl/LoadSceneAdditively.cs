using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrawfisSoftware.GameFlow
{
    /// <summary>
    /// Load the specified scene as an additive scene.
    /// </summary>
    public class LoadSceneAdditively : MonoBehaviour
    {
        [SerializeField] private string _sceneName;

        private void Start()
        {
            SceneManager.LoadSceneAsync(_sceneName, LoadSceneMode.Additive);
        }
    }
}