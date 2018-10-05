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
using GooglePlayInstant.Editor;
using GooglePlayInstant.Editor.AndroidManifest;
using UnityEditor;

namespace GooglePlayInstant.Samples.TestApp.Editor
{
    /// <summary>
    /// Exposes build functionality to the command line for testing.
    /// </summary>
    public static class TestAppBuilder
    {
        private const string ApkPathArg = "-outputFile";
        private const string ApplicationIdentifier = "com.google.android.instantapps.samples.unity.testapp";
        private const string CompanyName = "Google";
        private const string ProductName = "testapp";

        private static readonly string[] TestScenePaths = {"Assets/TestApp/Scenes/TestScene.unity"};

        public static void Build()
        {
            ConfigureProject();

            // Build APK
            var apkPath = GetApkPath();
            var buildPlayerOptions = PlayInstantBuilder.CreateBuildPlayerOptions(apkPath, BuildOptions.None);
            PlayInstantBuilder.BuildAndSign(buildPlayerOptions);

            // Build AAB
            var aabPath = apkPath.Substring(0, apkPath.Length - 3) + "aab";
            AppBundlePublisher.Build(aabPath);
        }

        private static void ConfigureProject()
        {
            var requiredPolicies = PlayInstantSettingPolicy.GetRequiredPolicies();
            foreach (var policy in requiredPolicies)
            {
                var policyChangeCompleted = policy.ChangeState();
                if (!policyChangeCompleted)
                {
                    throw new Exception(string.Format("Failed to change policy: {0}", policy.Name));
                }
            }

            SetTargetArchitectures();
            PlayerSettings.applicationIdentifier = ApplicationIdentifier;
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;

            var manifestUpdater = GetAndroidManifestUpdater();
            var errorMessage = manifestUpdater.SwitchToInstant(null);
            if (errorMessage != null)
            {
                throw new Exception(string.Format("Error updating AndroidManifest.xml: {0}", errorMessage));
            }

            PlayInstantBuildConfiguration.AddScriptingDefineSymbol(
                PlaySignatureVerifier.SkipVerifyGooglePlayServicesScriptingDefineSymbol);
            PlayInstantBuildConfiguration.SaveConfiguration("", TestScenePaths, "");
            PlayInstantBuildConfiguration.SetInstantBuildType();
        }

        /// <summary>
        /// Gets the apk path passed in via command line.
        /// </summary>
        private static string GetApkPath()
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == ApkPathArg)
                {
                    return args[i + 1];
                }
            }

            throw new Exception(string.Format("Missing required argument \"{0}\"", ApkPathArg));
        }

        private static void SetTargetArchitectures()
        {
#if UNITY_2018_1_OR_NEWER
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.X86 | AndroidArchitecture.ARMv7;
#else
            PlayerSettings.Android.targetDevice = AndroidTargetDevice.FAT;
#endif
        }

        private static IAndroidManifestUpdater GetAndroidManifestUpdater()
        {
#if UNITY_2018_1_OR_NEWER
            return new PostGenerateGradleProjectAndroidManifestUpdater();
#else
            return new LegacyAndroidManifestUpdater();
#endif
        }
    }
}
