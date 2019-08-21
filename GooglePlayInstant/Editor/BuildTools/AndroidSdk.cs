// Copyright 2019 Google LLC
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
using UnityEditor;

namespace GooglePlayInstant.Editor.BuildTools
{
    /// <summary>
    /// Build tool for finding the Android SDK root path.
    /// </summary>
    public class AndroidSdk
    {
        /// <summary>
        /// The ANDROID_HOME environment variable key.
        /// </summary>
        public const string AndroidHomeEnvironmentVariableKey = "ANDROID_HOME";
        private const string AndroidNdkHomeEnvironmentVariableKey = "ANDROID_NDK_HOME";
        private const string AndroidSdkRootEditorPrefsKey = "AndroidSdkRoot";
        private const string AndroidNdkRootEditorPrefsKey = "AndroidNdkRoot";

        /// <summary>
        /// Apply a workaround for Unity versions that fail if the AndroidSdkRoot/AndroidNdkRoot preferences aren't set.
        /// </summary>
        public static void ApplyEditorPrefsWorkaround()
        {
            // Unlike older versions of Unity, app builds on 2019.2 fail if the SDK/NDK preferences aren't set.
#if UNITY_2019_2_OR_NEWER
            var sdkPath = AndroidHomeEnvironmentVariable;
            if (!string.IsNullOrEmpty(sdkPath))
            {
                EditorPrefs.SetString(AndroidSdkRootEditorPrefsKey, sdkPath);
            }

            var ndkPath = Environment.GetEnvironmentVariable(AndroidNdkHomeEnvironmentVariableKey);
            if (!string.IsNullOrEmpty(ndkPath))
            {
                EditorPrefs.SetString(AndroidNdkRootEditorPrefsKey, ndkPath);
            }
#endif
        }

        private static string AndroidHomeEnvironmentVariable
        {
            get { return Environment.GetEnvironmentVariable(AndroidHomeEnvironmentVariableKey); }
        }
    }
}