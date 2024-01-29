#define LoadScene0OnPlay
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrawfisSoftware.Unity3D.Utility
{
#if LoadScene0OnPlay
    /// <summary>
    /// This script overrides the default behavior when pressing Play to mimic it as if you loaded the first scene in the Build index and then hit Play.
    /// It is useful when you have a "bootstrap" scene or need to always load the Main Menu first.
    /// </summary>
    public static class EditorPlayFirstSceneAlways
    {
        [InitializeOnEnterPlayMode]
        private static void OnLoad()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            // Ensure at least one build scene exist.
            if (EditorBuildSettings.scenes.Length == 0)
                return;

            // SetProperties Play Mode scene to first scene defined in build settings.
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorBuildSettings.scenes[0].path);
        }
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    Scene openInEditor = SceneManager.GetActiveScene();
                    if (SceneManager.GetActiveScene().buildIndex == 0)
                    {
                        return;
                    }
                    // Save off the current scene so it will be reloaded after the Play session is over.
                    PlayerPrefs.SetString("DefaultScene", openInEditor.name);
                    /// Debug.Log("SetProperties DefaultScene pref to " + openInEditor.name);
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    PlayerPrefs.SetString("DefaultScene", "");
                    /// Debug.Log("SetProperties DefaultScene pref to \"\"");
                    break;
            }
        }
    }
#endif
}