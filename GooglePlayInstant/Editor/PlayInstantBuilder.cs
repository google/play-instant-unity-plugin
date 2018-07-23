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

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

#if UNITY_2018_1_OR_NEWER
using UnityEditor.Build.Reporting;
#endif

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// Builder for Play Instant APKs.
    /// </summary>
    public static class PlayInstantBuilder
    {
        private const string BuildErrorTitle = "Build Error";
        private const string OkButtonText = "OK";
        private const string CancelButtonText = "Cancel";

        /// <summary>
        /// Builds a Play Instant APK based on the specified options. Displays warning/error dialogs if there
        /// are issues during the build.
        /// </summary>
        /// <returns>True if the build succeeded, false if it failed or was cancelled.</returns>
        public static bool Build(BuildPlayerOptions buildPlayerOptions)
        {
            if (!PlayInstantBuildConfiguration.IsPlayInstantScriptingSymbolDefined())
            {
                Debug.LogError("Build halted since selected platform is \"Installed\"");
                var message = string.Format(
                    "The currently selected Android Platform is \"Installed\".\n\n" +
                    "Click \"OK\" to open the \"{0}\" window where the platform can be changed to \"Instant\".",
                    PlayInstantSettingsWindow.WindowTitle);
                if (DisplayBuildErrorDialog(message))
                {
                    PlayInstantSettingsWindow.ShowWindow();
                }

                return false;
            }

            var failedPolicies = new List<string>(PlayInstantSettingPolicy.GetRequiredPolicies()
                .Where(policy => !policy.IsCorrectState())
                .Select(policy => policy.Name));
            if (failedPolicies.Count > 0)
            {
                Debug.LogErrorFormat("Build halted due to incompatible settings: {0}",
                    string.Join(", ", failedPolicies.ToArray()));
                var message = string.Format(
                    "{0}\n\nClick \"OK\" to open the settings window and make required changes.",
                    string.Join("\n\n", failedPolicies.ToArray()));
                if (DisplayBuildErrorDialog(message))
                {
                    PlayerAndBuildSettingsWindow.ShowWindow();
                }

                return false;
            }

            var buildReport = BuildPipeline.BuildPlayer(buildPlayerOptions);
#if UNITY_2018_1_OR_NEWER
            switch (buildReport.summary.result)
            {
                case BuildResult.Cancelled:
                    Debug.Log("Build cancelled");
                    return false;
                case BuildResult.Succeeded:
                    // BuildPlayer can fail and still return BuildResult.Succeeded so detect by checking totalErrors.
                    if (buildReport.summary.totalErrors > 0)
                    {
                        // No need to display a message since Unity will already have done this.
                        return false;
                    }

                    // Actual success.
                    return true;
                case BuildResult.Failed:
                    LogError(string.Format("Build failed with {0} error(s)", buildReport.summary.totalErrors));
                    return false;
                default:
                    LogError("Build failed with unknown error");
                    return false;
            }
#else
            if (string.IsNullOrEmpty(buildReport))
            {
                return true;
            }

            // Check for intended build cancellation.
            if (buildReport == "Building Player was cancelled")
            {
                Debug.Log(buildReport);
            }
            else
            {
                LogError(buildReport);
            }

            return false;
#endif
        }

        /// <summary>
        /// Displays the specified message indicating that a build error occurred. Displays an "OK" button that can
        /// be used to indicate that the user wants to perform a followup action, e.g. fixing a build setting.
        /// </summary>
        /// <returns>True if the user clicks "OK", otherwise false.</returns>
        public static bool DisplayBuildErrorDialog(string message)
        {
            return EditorUtility.DisplayDialog(BuildErrorTitle, message, OkButtonText, CancelButtonText);
        }

        private static void LogError(string message)
        {
            Debug.LogErrorFormat("Build error: {0}", message);
            EditorUtility.DisplayDialog(BuildErrorTitle, message, OkButtonText);
        }
    }
}