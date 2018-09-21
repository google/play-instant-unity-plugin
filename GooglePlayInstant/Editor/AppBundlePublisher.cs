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
using UnityEngine;

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// Helper to build <a href="https://developer.android.com/platform/technology/app-bundle/">Android App Bundle</a>
    /// files suitable for publishing on Play Console.
    /// </summary>
    public static class AppBundlePublisher
    {
        /// <summary>
        /// Builds an Android App Bundle at a user-specified file location.
        /// </summary>
        public static void Build()
        {
#if !UNITY_2018_3_OR_NEWER
            if (!AndroidAssetPackagingTool.CheckConvert())
            {
                return;
            }

            if (!Bundletool.CheckBundletool())
            {
                return;
            }
#endif

            // TODO: add checks for preferred Scripting Backend and Target Architectures.

            var aabFilePath = EditorUtility.SaveFilePanel("Create Android App Bundle", null, null, "aab");
            if (string.IsNullOrEmpty(aabFilePath))
            {
                // Assume cancelled.
                return;
            }

            Build(aabFilePath);
        }

        /// <summary>
        /// Builds an Android App Bundle at the specified location. Assumes that all dependencies (e.g. aapt)
        /// are already in-place.
        /// </summary>
        public static void Build(string aabFilePath)
        {
            Debug.LogFormat("Building app bundle: {0}", aabFilePath);
#if UNITY_2018_3_OR_NEWER
            EditorUserBuildSettings.buildAppBundle = true;
            var buildPlayerOptions = PlayInstantBuilder.CreateBuildPlayerOptions(aabFilePath, BuildOptions.None);
            if (PlayInstantBuilder.Build(buildPlayerOptions))
            {
                // Do not log in case of failure. The method we called was responsible for logging.
                Debug.LogFormat("Finished building app bundle: {0}", aabFilePath);
            }
#else
            AppBundleBuilder.Build(aabFilePath);
#endif
        }
    }
}