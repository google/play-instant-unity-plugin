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
using System.IO;
using System.Linq;
using GooglePlayInstant.Editor.GooglePlayServices;
using UnityEditor;
using UnityEngine;
#if UNITY_2018_1_OR_NEWER
using UnityEditor.Build.Reporting;
#endif

namespace GooglePlayInstant.Editor
{
    public static class PlayInstantRunner
    {
        private const string BuildAndRunErrorTitle = "Build and Run Error";
        private const string OkButtonText = "OK";
        private const string CancelButtonText = "Cancel";

        public static void BuildAndRun()
        {
            if (!PlayInstantBuildConfiguration.IsPlayInstantScriptingSymbolDefined())
            {
                Debug.LogError("Build and Run halted since selected platform is Installed");
                const string message = "The currently selected Android Platform is \"Installed\".\n\n" +
                                       "Click \"OK\" to open the \"Configure Instant or Installed\" " +
                                       "window where the platform can be changed to \"Instant\".";
                if (EditorUtility.DisplayDialog(BuildAndRunErrorTitle, message, OkButtonText, CancelButtonText))
                {
                    PlayInstantSettingsWindow.ShowWindow();
                }

                return;
            }

            var jarPath = Path.Combine(AndroidSdkManager.AndroidSdkRoot, "extras/google/instantapps/tools/ia.jar");
            if (!File.Exists(jarPath))
            {
                Debug.LogErrorFormat("Build and Run failed to locate ia.jar file at: {0}", jarPath);
                var message =
                    string.Format(
                        "Failed to locate version 1.2 or later of the {0}.\n\nClick \"OK\" to install the {0}.",
                        PlayInstantSdkInstaller.InstantAppsSdkName);
                if (EditorUtility.DisplayDialog(BuildAndRunErrorTitle, message, OkButtonText, CancelButtonText))
                {
                    PlayInstantSdkInstaller.SetUp();
                }

                return;
            }

            var failedPolicies = new List<string>(PlayInstantSettingPolicy.GetRequiredPolicies()
                .Where(policy => !policy.IsCorrectState())
                .Select(policy => policy.Name));
            if (failedPolicies.Count > 0)
            {
                Debug.LogErrorFormat("Build and Run halted due to incompatible settings: {0}",
                    string.Join(", ", failedPolicies.ToArray()));
                var message = string.Format(
                    "{0}\n\nClick \"OK\" to open the settings window and make required changes.",
                    string.Join("\n\n", failedPolicies.ToArray()));
                if (EditorUtility.DisplayDialog(BuildAndRunErrorTitle, message, OkButtonText, CancelButtonText))
                {
                    PlayerAndBuildSettingsWindow.ShowWindow();
                }

                return;
            }

            var apkPath = Path.Combine(Path.GetTempPath(), "temp.apk");
            Debug.LogFormat("Build and Run package location: {0}", apkPath);

            var buildPlayerOptions = CreateBuildPlayerOptions(apkPath);
#if UNITY_2018_1_OR_NEWER
            var buildReport = BuildPipeline.BuildPlayer(buildPlayerOptions);
            switch (buildReport.summary.result)
            {
                case BuildResult.Cancelled:
                    Debug.Log("Build cancelled");
                    return;
                case BuildResult.Succeeded:
                    // BuildPlayer can fail and still return BuildResult.Succeeded so detect by checking totalErrors.
                    if (buildReport.summary.totalErrors > 0)
                    {
                        // No need to display a message since Unity will already have done this.
                        return;
                    }

                    // Actual success: continue on to the run step.
                    break;
                case BuildResult.Failed:
                    LogError(string.Format("Build failed with {0} error(s)", buildReport.summary.totalErrors));
                    return;
                default:
                    LogError("Build failed with unknown error");
                    return;
            }
#else
            var buildPlayerResult = BuildPipeline.BuildPlayer(buildPlayerOptions);
            if (!string.IsNullOrEmpty(buildPlayerResult))
            {
                // Check for intended build cancellation.
                if (buildPlayerResult == "Building Player was cancelled")
                {
                    Debug.Log(buildPlayerResult);
                }
                else
                {
                    LogError(buildPlayerResult);
                }

                return;
            }
#endif

            var window = PostBuildCommandLineDialog.CreateDialog("Install and run app");
            window.modal = false;
            window.summaryText = "Installing app on device";
            window.bodyText = "The APK built successfully. Waiting for scripts to reload...\n";
            window.autoScrollToBottom = true;
            window.commandLineParameters = new PostBuildCommandLineDialog.CommandLineParameters()
            {
                FileName = JavaUtilities.JavaBinaryPath,
                Arguments = string.Format("-jar {0} run {1}", jarPath, apkPath),
                EnvironmentKey = AndroidSdkManager.AndroidHome,
                EnvironmentValue = AndroidSdkManager.AndroidSdkRoot
            };
            window.Show();
        }

        private static BuildPlayerOptions CreateBuildPlayerOptions(string apkPath)
        {
            var scenes = new List<string>(EditorBuildSettings.scenes
                .Where(scene => scene.enabled && !string.IsNullOrEmpty(scene.path))
                .Select(scene => scene.path));
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = EditorUserBuildSettings.development ? BuildOptions.Development : BuildOptions.None
            };
            return buildPlayerOptions;
        }

        private static void LogError(string message)
        {
            Debug.LogErrorFormat("Build and Run error: {0}", message);
            EditorUtility.DisplayDialog(BuildAndRunErrorTitle, message, OkButtonText);
        }
    }
}