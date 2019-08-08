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
using System.IO;
using GooglePlayInstant.Editor;
using GooglePlayInstant.Editor.GooglePlayServices;
using UnityEditor;
using UnityEngine;

namespace GooglePlayInstant.Samples.TestApp.Editor
{
    /// <summary>
    /// Provides a method to build the plugin TestApp from the command line.
    /// </summary>
    public static class TestAppBuilder
    {
        private static readonly string[] TestScenePaths = {"Assets/TestApp/Scenes/TestScene.unity"};

        public static void Build()
        {
            PlayerSettings.applicationIdentifier = "com.google.android.instantapps.samples.unity.testapp";
            PlayerSettings.companyName = "Google";
            PlayerSettings.productName = "testapp";

            CommandLineBuilder.ConfigureProject(TestScenePaths);


            var outputFilePrefix = CommandLineBuilder.GetOutputFilePrefix();
            var apkPath = outputFilePrefix + ".apk";
            var aabPath = outputFilePrefix + ".aab";

            var buildPlayerOptions = PlayInstantBuilder.CreateBuildPlayerOptions(apkPath, BuildOptions.None);
            if (!PlayInstantBuilder.BuildAndSign(buildPlayerOptions))
            {
                throw new Exception("APK build failed");
            }

            DownloadBundletoolIfNecessary();
            if (!AppBundlePublisher.Build(aabPath))
            {
                throw new Exception("AAB build failed");
            }
        }

        private static void DownloadBundletoolIfNecessary()
        {
            var bundletoolJarPath = Bundletool.BundletoolJarPath;
            if (File.Exists(bundletoolJarPath))
            {
                Debug.LogFormat("Found existing bundletool: {0}", bundletoolJarPath);
                return;
            }

            var arguments =
                string.Format(
                    "{0} -O {1}", Bundletool.BundletoolDownloadUrl, CommandLine.QuotePath(bundletoolJarPath));
            var result = CommandLine.Run("wget", arguments);
            if (result.exitCode == 0)
            {
                Debug.LogFormat("Downloaded bundletool: {0}", bundletoolJarPath);
            }
            else
            {
                throw new Exception("Failed to download bundletool: " + result.message);
            }
        }
    }
}