using UnityEditor;
using UnityEditor.SceneManagement;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrawfisSoftware.Unity3D.Utility
{
    /// <summary>
    /// This script overrides the default behavior when pressing Play to mimic it as if you loaded the first scene in the Build index and then hit Play.
    /// It is useful when you have a "bootstrap" scene or need to always load the Main Menu first.
    /// </summary>
    public static class EditorPlayFirstSceneAlways
    {
        private const string PREF_KEY = "PlayFirstSceneAlwaysEnabled";
        private const string MENU_LOCATION = "Crawfis/Play Scene 0 Always";

        static EditorPlayFirstSceneAlways()
        {
            // Ensure the menu is checked/unchecked on load
            Menu.SetChecked(MENU_LOCATION, IsEnabled);
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
                // SetProperties Play Mode scene to first scene defined in build settings.
                EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorBuildSettings.scenes[0].path);
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

        [InitializeOnEnterPlayMode]
        private static void OnLoad()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            if (!IsEnabled)
                return;
            // Ensure at least one build scene exist.
            if (EditorBuildSettings.scenes.Length == 0)
                return;
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