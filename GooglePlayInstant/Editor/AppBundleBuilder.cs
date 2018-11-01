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
using GooglePlayInstant.Editor.AndroidManifest;
using GooglePlayInstant.Editor.GooglePlayServices;
using GooglePlayInstant.Editor.QuickDeploy;
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
        private const string FeatureModuleName = "feature";
        private const string FeatureModuleZipFileName = FeatureModuleName + ".zip";

        /// <summary>
        /// Build an app bundle at the specified path, overwriting an existing file if one exists.
        /// </summary>
        /// <returns>True if the build succeeded, false if it failed or was cancelled.</returns>
        public static bool Build(string aabFilePath)
        {
            var binaryFormatFilePath = Path.GetTempFileName();
            Debug.LogFormat("Building Package: {0}", binaryFormatFilePath);

            // Do not use BuildAndSign since this signature won't be used.
            if (!PlayInstantBuilder.Build(
                PlayInstantBuilder.CreateBuildPlayerOptions(binaryFormatFilePath, BuildOptions.None)))
            {
                // Do not log here. The method we called was responsible for logging.
                return false;
            }

            var workingDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "play-instant-unity"));
            if (workingDirectory.Exists)
            {
                workingDirectory.Delete(true);
            }

            workingDirectory.Create();
            var sourceDirectoryInfo = workingDirectory.CreateSubdirectory("source");
            var destinationDirectoryInfo = workingDirectory.CreateSubdirectory("destination");

            var featureModuleZip = GetFeatureModuleZip(workingDirectory);

            // TODO: currently all processing is synchronous; consider moving to a separate thread
            try
            {
                DisplayProgress("Running aapt2", 0.2f);
                var protoFormatFileName = Path.GetRandomFileName();
                var protoFormatFilePath = Path.Combine(sourceDirectoryInfo.FullName, protoFormatFileName);
                var aaptResult = AndroidAssetPackagingTool.Convert(binaryFormatFilePath, protoFormatFilePath);
                if (aaptResult != null)
                {
                    DisplayBuildError("aapt2", aaptResult);
                    return false;
                }

                DisplayProgress("Creating base module", 0.4f);
                var unzipFileResult = ZipUtils.UnzipFile(protoFormatFileName, sourceDirectoryInfo.FullName);
                if (unzipFileResult != null)
                {
                    DisplayBuildError("Unzip", unzipFileResult);
                    return false;
                }

                File.Delete(protoFormatFilePath);

                ArrangeFiles(sourceDirectoryInfo, destinationDirectoryInfo);
                var baseModuleZip = Path.Combine(workingDirectory.FullName, BaseModuleZipFileName);
                var zipFileResult = ZipUtils.CreateZipFile(baseModuleZip, destinationDirectoryInfo.FullName, ".");
                if (zipFileResult != null)
                {
                    DisplayBuildError("Zip creation", zipFileResult);
                    return false;
                }

                // If the .aab file exists, EditorUtility.SaveFilePanel() has already prompted for whether to overwrite.
                // Therefore, prevent Bundletool from throwing an IllegalArgumentException that "File already exists."
                File.Delete(aabFilePath);

                DisplayProgress("Running bundletool", 0.6f);
                var modules =
                    featureModuleZip == null ? new[] {baseModuleZip} : new[] {baseModuleZip, featureModuleZip};
                var buildBundleResult = Bundletool.BuildBundle(modules, aabFilePath);
                if (buildBundleResult != null)
                {
                    DisplayBuildError("bundletool", buildBundleResult);
                    return false;
                }

                DisplayProgress("Signing bundle", 0.8f);
                var signingResult = ApkSigner.SignZip(aabFilePath);
                if (signingResult != null)
                {
                    DisplayBuildError("Signing", signingResult);
                    return false;
                }
            }
            finally
            {
                if (!WindowUtils.IsHeadlessMode())
                {
                    EditorUtility.ClearProgressBar();
                }
            }

            return true;
        }

        private static string GetFeatureModuleZip(DirectoryInfo workingDirectory)
        {
            var featureDirectoryInfo = workingDirectory.CreateSubdirectory("feature");
            var assetsDirectoryPath = featureDirectoryInfo.CreateSubdirectory("assets").FullName;

//            var config = new QuickDeployConfig();
//            config.LoadConfiguration();
//            // TODO: check scene enabled
//            if (config.AssetBundleScenes.ScenePaths.Length == 0)
//            {
//                return null;
//            }

//            var assetBundleBuild = new AssetBundleBuild
//            {
//                assetBundleName = "feature",
//                assetNames = config.AssetBundleScenes.ScenePaths
//            };
//
//            var builtAssetBundleManifest = BuildPipeline.BuildAssetBundles(
//                buildDirectory.FullName, new[] {assetBundleBuild}, BuildAssetBundleOptions.None, BuildTarget.Android);
//            if (builtAssetBundleManifest == null)
//            {
//                DisplayBuildError("Asset bundle creation", "TODO");
//                return null;
//            }

            // TODO: replace temp file with asset bundle
            File.WriteAllText(Path.Combine(assetsDirectoryPath, "file.txt"), "Hello!");

            var sourceDirectoryInfo = featureDirectoryInfo.CreateSubdirectory("source");
            var destinationDirectoryInfo = featureDirectoryInfo.CreateSubdirectory("destination");

            var manifestFileName = Path.Combine(featureDirectoryInfo.FullName, "AndroidManifest.xml");
            var manifestXmlDocument =
                AndroidManifestHelper.CreateFeatureModule(PlayerSettings.applicationIdentifier, FeatureModuleName);
            manifestXmlDocument.Save(manifestFileName);

            // TODO: pick the platform instead of hardcoding
            var androidJarPath = Path.Combine(AndroidSdkManager.AndroidSdkRoot, "platforms/android-27/android.jar");
            var protoFormatFileName = Path.GetRandomFileName();
            var protoFormatFilePath = Path.Combine(sourceDirectoryInfo.FullName, protoFormatFileName);
            var aaptResult = AndroidAssetPackagingTool.Link(
                manifestFileName, androidJarPath, assetsDirectoryPath, protoFormatFilePath);
            if (aaptResult != null)
            {
                DisplayBuildError("aapt2 X", aaptResult);
                throw new Exception();
            }

            var unzipFileResult = ZipUtils.UnzipFile(protoFormatFileName, sourceDirectoryInfo.FullName);
            if (unzipFileResult != null)
            {
                DisplayBuildError("Unzip X", unzipFileResult);
                throw new Exception();
            }

            File.Delete(protoFormatFilePath);

            ArrangeFiles(sourceDirectoryInfo, destinationDirectoryInfo);
            var featureModuleZip = Path.Combine(workingDirectory.FullName, FeatureModuleZipFileName);
            var zipFileResult = ZipUtils.CreateZipFile(featureModuleZip, destinationDirectoryInfo.FullName, ".");
            if (zipFileResult != null)
            {
                DisplayBuildError("Zip creation X", zipFileResult);
                throw new Exception();
            }

            return featureModuleZip;
        }

        /// <summary>
        /// Arrange files according to the <a href="https://developer.android.com/guide/app-bundle/#aab_format">
        /// Android App Bundle format</a>.
        /// </summary>
        private static void ArrangeFiles(DirectoryInfo source, DirectoryInfo destination)
        {
            foreach (var sourceFileInfo in source.GetFiles())
            {
                DirectoryInfo destinationSubdirectory;
                var fileName = sourceFileInfo.Name;
                if (fileName == "AndroidManifest.xml")
                {
                    destinationSubdirectory = destination.CreateSubdirectory("manifest");
                }
                else if (fileName == "resources.pb")
                {
                    destinationSubdirectory = destination;
                }
                else if (fileName.EndsWith("dex"))
                {
                    destinationSubdirectory = destination.CreateSubdirectory("dex");
                }
                else
                {
                    destinationSubdirectory = destination.CreateSubdirectory("root");
                }

                sourceFileInfo.MoveTo(Path.Combine(destinationSubdirectory.FullName, fileName));
            }

            foreach (var sourceDirectoryInfo in source.GetDirectories())
            {
                var directoryName = sourceDirectoryInfo.Name;
                switch (directoryName)
                {
                    case "META-INF":
                        // Skip files like MANIFEST.MF
                        break;
                    case "assets":
                    case "lib":
                    case "res":
                        sourceDirectoryInfo.MoveTo(Path.Combine(destination.FullName, directoryName));
                        break;
                    default:
                        var subdirectory = destination.CreateSubdirectory("root");
                        sourceDirectoryInfo.MoveTo(Path.Combine(subdirectory.FullName, directoryName));
                        break;
                }
            }
        }

        private static void DisplayProgress(string info, float progress)
        {
            Debug.LogFormat("{0}...", info);
            if (!WindowUtils.IsHeadlessMode())
            {
                EditorUtility.DisplayProgressBar("Building App Bundle", info, progress);
            }
        }

        private static void DisplayBuildError(string errorType, string errorMessage)
        {
            if (!WindowUtils.IsHeadlessMode())
            {
                EditorUtility.ClearProgressBar();
            }

            PlayInstantBuilder.DisplayBuildError(string.Format("{0} failed: {1}", errorType, errorMessage));
        }
    }
}