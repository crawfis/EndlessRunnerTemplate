using UnityEditor;
using UnityEditor.SceneManagement;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrawfisSoftware.Utility
{
    /// <summary>
    /// This script overrides the default behavior when pressing Play to mimic it as if you loaded the first scene in the Build index and then hit Play.
    /// It is useful when you have a "bootstrap" scene or need to always load the Main Menu first.
    /// </summary>
    public static class EditorPlayFirstSceneAlways
    {
        private const string PREF_KEY = "PlayFirstSceneAlwaysEnabled";
        private const string MENU_LOCATION = "Crawfis/Play Scene 0 Always";

        // Ensure setup happens after the Editor is loaded (domain reload/compile), before entering Play Mode.
        [InitializeOnLoadMethod]
        private static void InitializeOnEditorLoad()
        {
            // Keep menu in sync
            Menu.SetChecked(MENU_LOCATION, IsEnabled);

            // Ensure the playModeStartScene is set to the first scene in Build Settings if enabled
            if (IsEnabled)
            {
                if (EditorBuildSettings.scenes != null && EditorBuildSettings.scenes.Length > 0)
                {
                    EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorBuildSettings.scenes[0].path);
                }
                else
                {
                    // No scenes in Build Settings. Make sure we don't point to an invalid scene.
                    EditorSceneManager.playModeStartScene = null;
                }
            }

            // Register play mode change callback
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static bool IsEnabled
        {
            get { return EditorPrefs.GetBool(PREF_KEY, true); }
            set { EditorPrefs.SetBool(PREF_KEY, value); }
        }

        [MenuItem(MENU_LOCATION)]
        private static void ToggleAction()
        {
            IsEnabled = !IsEnabled;
            Menu.SetChecked(MENU_LOCATION, IsEnabled);
            if (IsEnabled)
            {
                // Set Play Mode scene to first scene defined in build settings, if any.
                if (EditorBuildSettings.scenes != null && EditorBuildSettings.scenes.Length > 0)
                {
                    EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorBuildSettings.scenes[0].path);
                }
                else
                {
                    EditorSceneManager.playModeStartScene = null;
                }
            }
            else
            {
                // Reset the play mode start scene to default.
                EditorSceneManager.playModeStartScene = null;
            }

        }

        [MenuItem(MENU_LOCATION, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MENU_LOCATION, IsEnabled);
            return !Application.isPlaying;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!IsEnabled)
                return;
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
}