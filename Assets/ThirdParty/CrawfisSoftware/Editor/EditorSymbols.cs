using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEditor.Build;

namespace CrawfisSoftware.Utility
{
    /// <summary>
    /// Allows editor scripts to "define" scripting symbols. Currently works only for standalone builds.
    /// </summary>
    internal class EditorSymbols
    {
        internal static string DefinedSymbols
        {
            get { return PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone); }
            private set { PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, value); }
        }

        internal static List<string> DefinedSymbolList
        {
            get { return DefinedSymbols.Split(';').ToList(); }
            private set { DefinedSymbols = string.Join(";", value.ToArray()); }
        }

        internal static void AddSymbol(string symbol)
        {
            var list = DefinedSymbolList;
            if (!list.Contains(symbol))
            {
                list.Add(symbol);
                DefinedSymbolList = list;
            }
        }

        internal static void RemoveSymbol(string symbol)
        {
            var list = DefinedSymbolList;
            if (list.Contains(symbol))
            {
                list.Remove(symbol);
                DefinedSymbolList = list;
            }
        }
    }
}