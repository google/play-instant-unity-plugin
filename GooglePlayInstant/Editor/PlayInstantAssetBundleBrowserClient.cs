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
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// A static class responsible for interactions with the Asset Bundle Browser
    /// <see cref="https://github.com/Unity-Technologies/AssetBundles-Browser"/>, such as detecting whether the browser
    /// is present or getting the current version of the Asset Bundle Browser.
    /// </summary>
    public static class AssetBundleBrowserClient
    {
        private const string AssetBundleBrowserPackageName = "com.unity.assetbundlebrowser";
        private const string AssetBundleBrowserMenuItem = "Window/AssetBundle Browser";
        private static bool? _assetBundleBrowserIsPresent;
        private static string _assetBundleBrowserVersion;


        /// <summary>
        /// Determine whether Asset Bundle Browser is present based on the currently loaded assemblies.
        /// </summary>
        /// <param name="useCurrentValueIfPresent">A boolean value corresponding to whether the caller wants to
        /// to re-use the cached value if it is present. Using the default value(true) provides a significant
        /// peformance improvement when calling this method from functions that are invoked so many times
        /// such as EditorWindow.OnGUI().</param>
        public static bool AssetBundleBrowserIsPresent(bool useCurrentValueIfPresent = true)
        {
            if (useCurrentValueIfPresent && _assetBundleBrowserIsPresent.HasValue)
            {
                return _assetBundleBrowserIsPresent.Value;
            }

            // Use Reflection to detect AssetBundleBrowserMain Class in the AssetBundleBrowser namespace.
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    // Look for AssetBundleBrowserMain in the AssetBundleBrowser Namespace.
                    if (string.Equals(type.Namespace, "AssetBundleBrowser") &&
                        type.Name.Equals("AssetBundleBrowserMain"))
                    {
                        _assetBundleBrowserIsPresent = true;
                        return _assetBundleBrowserIsPresent.Value;
                    }
                }
            }

            _assetBundleBrowserIsPresent = false;
            return false;
        }

        /// <summary>
        /// Get the version of Asset Bundle Browser if available in the package information.
        /// </summary>
        /// <param name="useCurrentValueIfPresent">A boolean value corresponding to whether the caller wants to
        /// to re-use the cached value if it is present. Using the default value(true) provides a significant
        /// peformance improvement when calling this method from functions that are invoked so many times
        /// such as EditorWindow.OnGUI().</param>
        /// <returns>"not found" if the version cannot be found, otherwise returns the version.</returns>
        public static string GetAssetBundleBrowserVersion(bool useCurrentValueIfPresent = true)
        {
            if (useCurrentValueIfPresent && _assetBundleBrowserVersion != null)
            {
                return _assetBundleBrowserVersion;
            }

            // Extract AssetBundleBrowser version name from the Asset Bundle Browser package.json file.
            // All folders with "AssetBundles-Browser" in their names are considered candidates.
            var assetBundleBrowserFolderPaths =
                Directory.GetDirectories(Application.dataPath).ToArray().Where(folderName =>
                    Regex.IsMatch(folderName, "AssetBundles-Browser", RegexOptions.IgnoreCase));
            foreach (var folderPath in assetBundleBrowserFolderPaths)
            {
                var expectedPackageDotJsonPath = Path.Combine(folderPath, "package.json");
                if (!File.Exists(expectedPackageDotJsonPath))
                {
                    Debug.LogWarningFormat("Could not find file {0}", expectedPackageDotJsonPath);
                    continue;
                }

                var data = File.ReadAllText(expectedPackageDotJsonPath);
                try
                {
                    var json = JsonUtility.FromJson<PackageDotJsonContent>(data);
                    if (string.Equals(json.name, AssetBundleBrowserPackageName))
                    {
                        _assetBundleBrowserVersion = json.version;
                        return _assetBundleBrowserVersion;
                    }
                }
                catch (ArgumentException e)
                {
                    Debug.LogWarningFormat("Unable to read Asset Bundle Browser version contents from {0}. \n {1}",
                        expectedPackageDotJsonPath, e.Message);
                }
            }

            _assetBundleBrowserVersion = "not found";
            return _assetBundleBrowserVersion;
        }

        /// <summary>
        /// Display the Asset Bundle Browser window or log an error if not present.
        /// </summary>
        public static void DisplayAssetBundleBrowser()
        {
            if (!AssetBundleBrowserIsPresent())
            {
                Debug.LogError("Cannot detect Unity Asset Bundle Browser");
                return;
            }

            EditorApplication.ExecuteMenuItem(AssetBundleBrowserMenuItem);
        }

        /// <summary>
        /// Reload and update information about Asset Bundle Browser. Helpful as a callback method for when tabs relying
        /// on information from this class are re-opened after changes about Asset Bundle Browser have taken place.
        /// </summary>
        public static void ReloadAndUpdateBrowserInfo()
        {
            _assetBundleBrowserIsPresent = AssetBundleBrowserIsPresent(false);
            _assetBundleBrowserVersion = GetAssetBundleBrowserVersion(false);
        }

        // Represents name and version fields from the package.json file of the Asset Bundle Browser project:
        // https://github.com/Unity-Technologies/AssetBundles-Browser/blob/master/package.json
        // Suppress warnings about non-initialization of fields.
#pragma warning disable CS0649 
        [Serializable]
        private class PackageDotJsonContent
        {
            public string name;
            public string version;
        }
    }
}