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

using System.IO;
using System.Linq;
using GooglePlayInstant.Editor;
using UnityEditor;

namespace GooglePlayInstant.Samples.TestApp.Editor
{
    /// <summary>
    /// Exposes build functionality to the command line for testing.
    /// </summary>
    public static class TestAppBuilder
    {
        private const string BundleIdentifier = "com.google.android.instantapps.samples.unity.testapp";
        private const string DefaultApkPath = "Assets/../testApp.apk";
        private const string ApkPathArg = "-outputFile";
        private static readonly string[] TestScenePaths = {"Assets/TestApp/Scenes/TestScene.unity"};

        public static void Build()
        {
            ConfigureProject();
            var apkPath = GetApkPath();
            var buildPlayerOptions = PlayInstantBuilder.CreateBuildPlayerOptions(apkPath, BuildOptions.None);
            PlayInstantBuilder.BuildAndSign(buildPlayerOptions);
        }

        private static void ConfigureProject()
        {
            var requiredPolicies = PlayInstantSettingPolicy.GetRequiredPolicies();
            foreach (var policy in requiredPolicies)
            {
                policy.ChangeState();
            }

            PlayInstantBuildConfiguration.SaveConfiguration("", TestScenePaths, "");
            PlayInstantBuildConfiguration.SetInstantBuildType();
            PlayerSettings.applicationIdentifier = BundleIdentifier;
        }

        /// <summary>
        /// Gets the apk path passed in via command line.
        /// </summary>
        private static string GetApkPath()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (var i=0; i<args.Length-1; i++)
            {
                if (Equals(args[i], ApkPathArg))
                {
                    return args[i + 1];
                }
            }
            return DefaultApkPath;
        }
    }
}