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
using System.Text.RegularExpressions;
using GooglePlayInstant.Editor.GooglePlayServices;
using UnityEngine;

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// Provides utility methods related to Android SDK build-tools.
    /// </summary>
    public static class AndroidBuildTools
    {
        private static readonly Regex VersionRegex =
            new Regex(@"^(\d+)\.(\d+)\.(\d+)(-rc(\d+))?$", RegexOptions.Compiled);

        /// <summary>
        /// Returns the newest build-tools path as a string, or null if one couldn't be found.
        /// </summary>
        public static string GetNewestBuildToolsPath()
        {
            var buildToolsPath = Path.Combine(AndroidSdkManager.AndroidSdkRoot, "build-tools");
            if (!Directory.Exists(buildToolsPath))
            {
                Debug.LogErrorFormat("Failed to locate build-tools path: {0}", buildToolsPath);
                return null;
            }

            var directoryInfo = new DirectoryInfo(buildToolsPath);
            var directoryNames = directoryInfo.GetDirectories().Select(dir => dir.Name);
            var newestBuildTools = GetNewestVersion(directoryNames);
            if (newestBuildTools == null)
            {
                Debug.LogErrorFormat("Failed to locate newest build-tools: {0}", buildToolsPath);
                return null;
            }

            return Path.Combine(buildToolsPath, newestBuildTools);
        }

        // Visible for testing.
        public static string GetNewestVersion(IEnumerable<string> versions)
        {
            if (versions == null)
            {
                return null;
            }

            var maxVersionLong = -1L;
            string maxVersionString = null;
            foreach (var versionString in versions)
            {
                var versionLong = ConvertVersionStringToLong(versionString);
                if (versionLong > maxVersionLong)
                {
                    maxVersionLong = versionLong;
                    maxVersionString = versionString;
                }
            }

            return maxVersionString;
        }

        private static long ConvertVersionStringToLong(string versionString)
        {
            var match = VersionRegex.Match(versionString);
            if (!match.Success)
            {
                return -1L;
            }

            var versionLong = 0L;
            for (var i = 1; i <= 3; i++)
            {
                versionLong += long.Parse(match.Groups[i].Value);
                // Multiply by a somewhat arbitrary value since major version outweighs minor version.
                // This particular arbitrary value supports up to 4 digits per version component.
                versionLong *= 10000L;
            }

            var releaseCandidateVersionGroup = match.Groups[5];
            if (releaseCandidateVersionGroup.Success)
            {
                // Add the release candidate version, e.g. rc2 is newer than rc1.
                versionLong += long.Parse(releaseCandidateVersionGroup.Value);
                // But also subtract a little, since any "rc" is earlier than the equivalent release,
                // e.g. "28.0.0-rc2" is older than "28.0.0".
                versionLong -= 5000L;
            }

            return versionLong;
        }
    }
}