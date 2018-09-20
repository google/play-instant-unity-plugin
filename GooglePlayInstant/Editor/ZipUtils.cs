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

using GooglePlayInstant.Editor.GooglePlayServices;
using UnityEngine;

namespace GooglePlayInstant.Editor
{
    public static class ZipUtils
    {
        /// <summary>
        /// Creates a ZIP file containing the specified file in the specified directory.
        /// </summary>
        public static void CreateZipFile(string inputDirectoryName, string inputFileName, string zipFilePath)
        {
            var arguments = string.Format(
                "cvf {0} -C {1} {2}",
                CommandLine.QuotePathIfNecessary(zipFilePath),
                CommandLine.QuotePathIfNecessary(inputDirectoryName),
                inputFileName);
            var result = CommandLine.Run(JavaUtilities.JarBinaryPath, arguments);
            if (result.exitCode == 0)
            {
                Debug.LogFormat("Created ZIP file: {0}", zipFilePath);
            }
            else
            {
                PlayInstantBuilder.LogError(string.Format("Zip creation failed: {0}", result.message));
            }
        }
    }
}