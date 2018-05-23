// Copyright 2018 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace GooglePlayInstant.Editor
{
    public static class PlayInstantBuildConfiguration
    {
        public const string PlayInstantScriptingDefineSymbol = "PLAY_INSTANT";

        // Allowed characters for splitting PlayerSettings.GetScriptingDefineSymbolsForGroup().
        private static readonly char[] ScriptingDefineSymbolsSplitChars = {';', ',', ' '};
        private const string PlayInstantUrlKeyPrefix = "GooglePlayInstant.InstantUrl.";

        public static string GetInstantUrl()
        {
            return EditorPrefs.GetString(PlayInstantUrlKey);
        }

        public static void SetInstantUrl(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                EditorPrefs.DeleteKey(PlayInstantUrlKey);
            }
            else
            {
                EditorPrefs.SetString(PlayInstantUrlKey, value);
            }
        }

        private static string PlayInstantUrlKey
        {
            get
            {
                // TODO: figure out the best approach for per-project config.
                var packageName = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android) ?? "unknown";
                return PlayInstantUrlKeyPrefix + packageName;
            }
        }

        public static bool IsPlayInstantScriptingSymbolDefined()
        {
            return IsPlayInstantScriptingSymbolDefined(GetScriptingDefineSymbols());
        }

        public static void DefinePlayInstantScriptingSymbol()
        {
            var scriptingDefineSymbols = GetScriptingDefineSymbols();
            if (!IsPlayInstantScriptingSymbolDefined(scriptingDefineSymbols))
            {
                SetScriptingDefineSymbols(scriptingDefineSymbols.Concat(new[] {PlayInstantScriptingDefineSymbol}));
            }
        }

        public static void UndefinePlayInstantScriptingSymbol()
        {
            var scriptingDefineSymbols = GetScriptingDefineSymbols();
            if (IsPlayInstantScriptingSymbolDefined(scriptingDefineSymbols))
            {
                SetScriptingDefineSymbols(scriptingDefineSymbols.Where(sym => sym != PlayInstantScriptingDefineSymbol));
            }
        }

        private static bool IsPlayInstantScriptingSymbolDefined(string[] scriptingDefineSymbols)
        {
            return Array.IndexOf(scriptingDefineSymbols, PlayInstantScriptingDefineSymbol) >= 0;
        }

        private static string[] GetScriptingDefineSymbols()
        {
            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);
            if (string.IsNullOrEmpty(symbols))
            {
                return new string[0];
            }

            return symbols.Split(ScriptingDefineSymbolsSplitChars, StringSplitOptions.RemoveEmptyEntries);
        }

        private static void SetScriptingDefineSymbols(IEnumerable<string> scriptingDefineSymbols)
        {
            var symbols = string.Join(";", scriptingDefineSymbols.ToArray());
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, symbols);
        }
    }
}