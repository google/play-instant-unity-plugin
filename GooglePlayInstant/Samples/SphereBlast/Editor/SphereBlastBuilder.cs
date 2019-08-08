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
using UnityEditor;

namespace GooglePlayInstant.Samples.SphereBlast.Editor
{
    /// <summary>
    /// Provides a method to build the SphereBlast sample from the command line.
    /// </summary>
    public static class SphereBlastBuilder
    {
        // TODO: currently including both scenes in the app for coverage, but prefer to build SphereScene separately.
        private static readonly string[] ScenesInBuild = {"Assets/SphereBlast/scenes/LoadingScene.unity"};

        public static void Build()
        {
            PlayerSettings.applicationIdentifier = "com.google.android.instantapps.samples.unity.sphereblast";
            PlayerSettings.companyName = "Google";
            PlayerSettings.productName = "Sphere Blast";

            CommandLineBuilder.ConfigureProject(ScenesInBuild);

            var apkPath = CommandLineBuilder.GetOutputFilePrefix() + ".apk";
            var buildPlayerOptions = PlayInstantBuilder.CreateBuildPlayerOptions(apkPath, BuildOptions.None);
            if (!PlayInstantBuilder.BuildAndSign(buildPlayerOptions))
            {
                throw new Exception("APK build failed");
            }
        }
    }
}