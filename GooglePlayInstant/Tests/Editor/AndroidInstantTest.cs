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
using UnityEngine;
using UnityEditor;
using System.Linq;
using GooglePlayInstant;

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// Exposes build and run functionality to the command line for testing.
    /// </summary>
    public static class AndroidInstantTest
    {
        public const string BundleIdentifier = "com.google.play.playInstantTest";
        public const string ScenesPath = "Assets/TestProject/scenes/";
        public static readonly string[] SceneFilesToTest = new string[] { "TestScene.unity" };

        public static void BuildAndRunTestProject()
        {
            ConfigureProject();
            PlayInstantRunner.BuildAndRun();
        }

        private static void ConfigureProject()
        {
            var requiredPolicies = PlayInstantSettingPolicy.GetRequiredPolicies();
            foreach (var policy in requiredPolicies)
            {
                policy.ChangeState();
            }

            var testScenePaths = SceneFilesToTest
                .Select(filename => System.IO.Path.Combine(ScenesPath, filename))
                .ToArray();
            PlayInstantBuildConfiguration.SaveConfiguration("", testScenePaths, "");
            PlayInstantBuildConfiguration.SetInstantBuildType();
            PlayerSettings.applicationIdentifier = BundleIdentifier;
        }
    }
}
