using CrawfisSoftware.GameFlow.Events;

using UnityEngine;
using UnityEngine.SceneManagement;
using GameFlowBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.GameFlow.Events.GameFlowEvents>;

namespace CrawfisSoftware.GameFlow.SceneManagement
{
    /// <summary>
    /// Loads the selected level's gameplay scene when GameScenesLoading fires.
    /// Reads the scene name from GameState.SelectedLevel, falling back to
    /// a configurable default for backward compatibility.
    ///    Subscribes: GameFlowEvents.GameScenesLoading
    ///    Dependencies: GameState.SelectedLevel
    /// </summary>
    class DynamicLevelSceneLoader : MonoBehaviour
    {
        [SerializeField] private string _fallbackGameplayScene = "TempleRunGameplay";
        [SerializeField] private string _sharedTrackPCGScene = "TempleRunTrackPCG";

        private void Start()
        {
            GameFlowBus.Subscribe(
                GameFlowEvents.GameScenesLoading, OnScenesLoading);
        }

        private void OnDestroy()
        {
            GameFlowBus.Unsubscribe(
                GameFlowEvents.GameScenesLoading, OnScenesLoading);
        }

        private void OnScenesLoading(string eventName, object sender, object data)
        {
            string gameplayScene = GameState.SelectedLevel != null
                ? GameState.SelectedLevel.GameplaySceneName
                : _fallbackGameplayScene;

            SceneManager.LoadSceneAsync(gameplayScene, LoadSceneMode.Additive);

            if (!string.IsNullOrEmpty(_sharedTrackPCGScene))
            {
                SceneManager.LoadSceneAsync(_sharedTrackPCGScene, LoadSceneMode.Additive);
            }
        }
    }
}
