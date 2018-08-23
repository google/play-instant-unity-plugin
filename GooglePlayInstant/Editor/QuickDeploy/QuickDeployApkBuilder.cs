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

using UnityEditor;

namespace GooglePlayInstant.Editor.QuickDeploy
{
    /// <summary>
    /// A handler for building apks using quick deploy.
    /// </summary>
    public static class QuickDeployApkBuilder
    {
        /// <summary>
        /// Determine whether or not the project is using IL2CPP as scripting backend.
        /// </summary>
        public static bool ProjectIsUsingIl2cpp()
        {
            return PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) == ScriptingImplementation.IL2CPP;
        }

        /// <summary>
        /// Build an android apk with quick deploy. Prompts the user to enable IL2CPP and engine stripping if not
        /// enabled, and builds the apk with the settings that the user chooses.
        /// Produces a resulting apk that contains the splash scene and functionality that will load the game's
        /// asset bundle from the cloud at the game's runtime.
        /// Logs success message to the console with built apk's path when the apk is successfully built, otherwise logs
        /// error message.
        /// </summary>
        public static void BuildQuickDeployInstantGameApk()
        {
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] {LoadingScreenGenerator.LoadingSceneName + ".unity"},
                locationPathName = QuickDeployConfig.ApkFileName,
                target = BuildTarget.Android,
                options = BuildOptions.None
                // TODO: include asset bundle manifest path in options.
            };

            const string recommendationDialogTitle = "Scripting Backend Recommendation";
            const string textForYes = "Yes";
            const string textForNo = "No";

            if (!ProjectIsUsingIl2cpp())
            {
                var enableIl2cppAndEngineStripping = EditorUtility.DisplayDialog(recommendationDialogTitle,
                    "Your project currently uses the Mono scripting backend. Would you like to switch to the IL2CPP " +
                    "scripting backend with engine stripping to improve game performance and reduce APK size?",
                    textForYes, textForNo);
                if (enableIl2cppAndEngineStripping)
                {
                    // Note: These changes are not undone after this build and will affect future builds.
                    PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
                    PlayerSettings.stripEngineCode = true;
                }
            }
            else
            {
                if (!PlayerSettings.stripEngineCode)
                {
                    var stripEngineCode = EditorUtility.DisplayDialog(recommendationDialogTitle,
                        "Your project currently uses the IL2CPP scripting backend without engine stripping. Would you " +
                        "like to enable engine stripping to reduce APK size?",
                        textForYes, textForNo);
                    if (stripEngineCode)
                    {
                        // Note: This change is not undone after this build and will affect future builds.
                        PlayerSettings.stripEngineCode = true;
                    }
                }
            }

            // Note: Do not print an error if build fails since Build() does this already
            PlayInstantBuilder.BuildAndSign(buildPlayerOptions);
        }
    }
}