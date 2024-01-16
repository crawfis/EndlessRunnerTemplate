using CrawfisSoftware.Unity3D.Utility;
using UnityEditor;
using UnityEngine;

namespace GTMY.EditorUtil
{
    /// <summary>
    /// This will add a menu that allows you to toggle the build or compiler setting DEV_BUILD.
    /// </summary>
    internal class ToggleDevBuild : EditorWindow
    {
        private const string MENU_LOCATION = "Crawfis/Compile as Dev Build";
        internal const string DEV_BUILD_SYMBOL = "DEV_BUILD";

        internal static bool IsDevBuild
        {
            get { return EditorSymbols.DefinedSymbolList.Contains(DEV_BUILD_SYMBOL); }
            set
            {
                if (value)
                {
                    EditorSymbols.AddSymbol(DEV_BUILD_SYMBOL);
                }
                else
                {
                    EditorSymbols.RemoveSymbol(DEV_BUILD_SYMBOL);
                }
            }
        }

        [MenuItem(MENU_LOCATION)]
        private static void ToggleAction()
        {
            IsDevBuild = !IsDevBuild;
        }

        [MenuItem(MENU_LOCATION, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MENU_LOCATION, IsDevBuild);
            return !Application.isPlaying;
        }

    }
}