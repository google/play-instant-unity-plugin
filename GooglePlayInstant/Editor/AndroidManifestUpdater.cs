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
using System.Xml.Linq;
using UnityEngine;
#if UNITY_2018_1_OR_NEWER
using UnityEditor;
using UnityEditor.Android;
#endif

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// This class has two modes of operation depending on the version of Unity.
    ///
    /// Pre-2018: Saves manifest changes to Assets/Plugins/Android/AndroidManifest.xml
    ///
    /// 2018.1+: Obtains the AndroidManifest.xml after it is fully merged but before the build occurs, and updates it
    /// according to whether this is a Play Instant build.
    /// </summary>
    public class AndroidManifestUpdater
#if UNITY_2018_1_OR_NEWER
        : IPostGenerateGradleAndroidProject
#endif
    {
#if UNITY_2018_1_OR_NEWER
        public int callbackOrder
        {
            get { return 100; }
        }

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            if (!PlayInstantBuildConfiguration.IsPlayInstantScriptingSymbolDefined())
            {
                return;
            }

            // Update the final merged AndroidManifest.xml prior to the gradle build.
            var manifestPath = Path.Combine(path, "src/main/AndroidManifest.xml");
            Debug.LogFormat("Updating manifest for Play Instant: {0}", manifestPath);

            Uri uri = null;
            var instantUrl = PlayInstantBuildConfiguration.GetInstantUrl();
            if (!string.IsNullOrEmpty(instantUrl))
            {
                uri = new Uri(instantUrl);
            }

            var doc = XDocument.Load(manifestPath);
            var errorMessage = AndroidManifestHelper.ConvertManifestToInstant(doc, uri);
            if (errorMessage != null)
            {
                var message = string.Format("Error updating AndroidManifest.xml: {0}", errorMessage);
                Debug.LogError(message);
                EditorUtility.DisplayDialog("Build Error", message, "OK");
                return;
            }

            doc.Save(manifestPath);
        }
#else
        private const string AndroidManifestAssetsDirectory = "Assets/Plugins/Android/";
        private const string AndroidManifestAssetsPath = AndroidManifestAssetsDirectory + "AndroidManifest.xml";
#endif

        public static string SwitchToInstant(Uri uri)
        {
#if !UNITY_2018_1_OR_NEWER
            XDocument doc;
            if (File.Exists(AndroidManifestAssetsPath))
            {
                Debug.LogFormat("Loading existing file {0}", AndroidManifestAssetsPath);
                doc = XDocument.Load(AndroidManifestAssetsPath);
            }
            else
            {
                Debug.Log("Creating new manifest file");
                doc = AndroidManifestHelper.CreateManifestXDocument();
            }

            var errorMessage = AndroidManifestHelper.ConvertManifestToInstant(doc, uri);
            if (errorMessage != null)
            {
                return errorMessage;
            }

            if (!Directory.Exists(AndroidManifestAssetsDirectory))
            {
                Directory.CreateDirectory(AndroidManifestAssetsDirectory);
            }

            doc.Save(AndroidManifestAssetsPath);

            Debug.LogFormat("Successfully updated {0}", AndroidManifestAssetsPath);
#endif
            return null;
        }

        public static void SwitchToInstalled()
        {
#if !UNITY_2018_1_OR_NEWER
            if (!File.Exists(AndroidManifestAssetsPath))
            {
                Debug.LogFormat("Nothing to do for {0} since file does not exist", AndroidManifestAssetsPath);
                return;
            }

            Debug.LogFormat("Loading existing file {0}", AndroidManifestAssetsPath);
            var doc = XDocument.Load(AndroidManifestAssetsPath);
            AndroidManifestHelper.ConvertManifestToInstalled(doc);
            doc.Save(AndroidManifestAssetsPath);
            Debug.LogFormat("Successfully updated {0}", AndroidManifestAssetsPath);
#endif
        }
    }
}