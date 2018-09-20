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
    /// Helper to build an Android App Bundle file suitable for publishing on Play Console.
    /// </summary>
    public static class AppBundlePublisher
    {
        /// <summary>
        /// Builds an Android App Bundle.
        /// </summary>
        public static void Build()
        {
#if UNITY_2018_3_OR_NEWER
            var aabFilePath = EditorUtility.SaveFilePanel("Create Android App Bundle", null, null, "aab");
            if (string.IsNullOrEmpty(aabFilePath))
            {
                // Assume cancelled.
                return;
            }

            Debug.LogFormat("Building app bundle: {0}", aabFilePath);
            EditorUserBuildSettings.buildAppBundle = true;
            var buildPlayerOptions = PlayInstantBuilder.CreateBuildPlayerOptions(aabFilePath, BuildOptions.None);
            if (PlayInstantBuilder.Build(buildPlayerOptions))
            {
                // Do not log in case of failure. The method we called was responsible for logging.
                Debug.LogFormat("Finished building app bundle: {0}", aabFilePath);
            }
#else
            throw new InvalidOperationException("Android App Bundles are only supported on Unity 2018.3+");
#endif
        }
    }
}