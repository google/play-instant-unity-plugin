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
using UnityEditor;
using UnityEngine;

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// Helper to build an Android App Bundle file on Unity version 2018.2 and earlier.
    /// </summary>
    public static class AppBundleBuilder
    {
        private const string BaseModuleZipFileName = "base.zip";

        public static void Build(string aabFilePath)
        {
            var tempFilePath = Path.GetTempFileName();
            Debug.LogFormat("Building Package: {0}", tempFilePath);
            var buildPlayerOptions = PlayInstantBuilder.CreateBuildPlayerOptions(tempFilePath, BuildOptions.None);

            // Do not use BuildAndSign since this signature won't be used.
            if (!PlayInstantBuilder.Build(buildPlayerOptions))
            {
                // Do not log here. The method we called was responsible for logging.
                return;
            }

            CreateAppBundle(tempFilePath, aabFilePath);
        }

        // TODO: currently all processing is synchronous; consider moving to a separate thread
        private static void CreateAppBundle(string inputFile, string aabFilePath)
        {
            try
            {
                DisplayProgress("Running aapt2", 0.2f);
                var workingDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "play-instant-unity"));
                if (workingDirectory.Exists)
                {
                    workingDirectory.Delete(true);
                }

                workingDirectory.Create();
                var sourceDirectoryInfo = workingDirectory.CreateSubdirectory("source");
                var destinationDirectoryInfo = workingDirectory.CreateSubdirectory("destination");

                var tempFileName = Path.GetRandomFileName();
                var tempFilePath = Path.Combine(sourceDirectoryInfo.FullName, tempFileName);
                var aaptResult = AndroidAssetPackagingTool.Convert(inputFile, tempFilePath);
                if (aaptResult != null)
                {
                    LogError("aapt2", aaptResult);
                    return;
                }

                DisplayProgress("Creating base module", 0.4f);
                var unzipFileResult = ZipUtils.UnzipFile(tempFileName, sourceDirectoryInfo.FullName);
                if (unzipFileResult != null)
                {
                    LogError("Unzip", unzipFileResult);
                    return;
                }

                File.Delete(tempFilePath);

                var baseModuleZip = Path.Combine(workingDirectory.FullName, BaseModuleZipFileName);
                ConvertFiles(sourceDirectoryInfo, destinationDirectoryInfo);

                var zipFileResult = ZipUtils.CreateZipFile(baseModuleZip, destinationDirectoryInfo.FullName, ".");
                if (zipFileResult != null)
                {
                    LogError("Zip creation", zipFileResult);
                    return;
                }

                DisplayProgress("Running bundletool", 0.6f);
                var buildBundleResult = Bundletool.BuildBundle(baseModuleZip, aabFilePath);
                if (buildBundleResult != null)
                {
                    LogError("bundletool", buildBundleResult);
                    return;
                }

                DisplayProgress("Signing bundle", 0.8f);
                var signingResult = ApkSigner.SignZip(aabFilePath);
                if (signingResult != null)
                {
                    LogError("Signing", signingResult);
                    return;
                }
            }
            finally
            {
                if (!PlayInstantBuilder.IsHeadlessMode())
                {
                    EditorUtility.ClearProgressBar();
                }
            }

            Debug.LogFormat("Finished building app bundle: {0}", aabFilePath);
        }

        private static void DisplayProgress(string info, float progress)
        {
            Debug.LogFormat("{0}...", info);
            if (!PlayInstantBuilder.IsHeadlessMode())
            {
                EditorUtility.DisplayProgressBar("Building App Bundle", info, progress);
            }
        }

        private static void LogError(string errorType, string errorMessage)
        {
            if (!PlayInstantBuilder.IsHeadlessMode())
            {
                EditorUtility.ClearProgressBar();
            }

            PlayInstantBuilder.LogError(string.Format("{0} failed: {1}", errorType, errorMessage));
        }

        private static void ConvertFiles(DirectoryInfo source, DirectoryInfo destination)
        {
            // Copy files in the root directory.
            foreach (var fileInfo in source.GetFiles())
            {
                var fileName = fileInfo.Name;
                if (fileName == "AndroidManifest.xml")
                {
                    var subdirectory = destination.CreateSubdirectory("manifest");
                    fileInfo.CopyTo(Path.Combine(subdirectory.FullName, fileName));
                }
                else if (fileName == "resources.pb")
                {
                    fileInfo.CopyTo(Path.Combine(destination.FullName, fileName));
                }
                else if (fileName.EndsWith("dex"))
                {
                    var subdirectory = destination.CreateSubdirectory("dex");
                    fileInfo.CopyTo(Path.Combine(subdirectory.FullName, fileName));
                }
                else
                {
                    var subdirectory = destination.CreateSubdirectory("root");
                    fileInfo.CopyTo(Path.Combine(subdirectory.FullName, fileName));
                }
            }

            // Copy all other files.
            foreach (var directoryInfo in source.GetDirectories())
            {
                var directoryName = directoryInfo.Name;
                switch (directoryName)
                {
                    case "META-INF":
                        // Skip files like MANIFEST.MF
                        break;
                    case "assets":
                    case "lib":
                    case "res":
                        CopyDirectory(directoryInfo, destination.CreateSubdirectory(directoryName));
                        break;
                    default:
                        var subdirectory = destination.CreateSubdirectory("root");
                        CopyDirectory(directoryInfo, subdirectory.CreateSubdirectory(directoryName));
                        break;
                }
            }
        }

        private static void CopyDirectory(DirectoryInfo source, DirectoryInfo destination)
        {
            if (!destination.Exists)
            {
                destination.Create();
            }

            foreach (var fileInfo in source.GetFiles())
            {
                fileInfo.CopyTo(Path.Combine(destination.FullName, fileInfo.Name));
            }

            foreach (var subdirectoryInfo in source.GetDirectories())
            {
                CopyDirectory(subdirectoryInfo, destination.CreateSubdirectory(subdirectoryInfo.Name));
            }
        }
    }
}