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
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// Downloads <a href="https://developer.android.com/studio/command-line/bundletool">bundletool</a>.
    /// </summary>
    public class BundletoolDownloadWindow : EditorWindow
    {
        private UnityWebRequest _downloadRequest;

        private void OnGUI()
        {
            GUI.enabled = _downloadRequest == null;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Bundletool is a command line java program used for creating Android App Bundles (.aab files). " +
                "Bundletool is also used to generate a set of APKs from an .aab file.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();
            CreateButton("Learn more",
                () => { Application.OpenURL("https://developer.android.com/studio/command-line/bundletool"); });

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(string.Format("Click \"Download\" to download bundletool version {0}.",
                Bundletool.BundletoolVersion), EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();
            CreateButton("Download", StartDownload);

            GUI.enabled = true;
        }

        private void Update()
        {
            if (_downloadRequest == null)
            {
                return;
            }

            if (_downloadRequest.isDone)
            {
                EditorUtility.ClearProgressBar();

#if UNITY_2017_1_OR_NEWER
                if (_downloadRequest.isHttpError || _downloadRequest.isNetworkError)
#else
                if (_downloadRequest.isError)
#endif
                {
                    var downloadRequestError = _downloadRequest.error;
                    _downloadRequest.Dispose();
                    _downloadRequest = null;

                    Debug.LogErrorFormat("Bundletool download error: {0}", downloadRequestError);
                    if (EditorUtility.DisplayDialog("Download Failed",
                        downloadRequestError + "\n\nClick \"OK\" to retry.", "OK", "Cancel"))
                    {
                        StartDownload();
                    }
                    else
                    {
                        Close();
                    }

                    return;
                }

                // Download succeeded. Copy the bytes if this version of Unity doesn't support DownloadHandlerFile.
                var bundletoolJarPath = Bundletool.GetBundletoolJarPath();
#if !UNITY_2017_2_OR_NEWER
                File.WriteAllBytes(bundletoolJarPath, _downloadRequest.downloadHandler.data);
#endif
                _downloadRequest.Dispose();
                _downloadRequest = null;

                Debug.LogFormat("Bundletool downloaded: {0}", bundletoolJarPath);
                var message = string.Format(
                    "Bundletool has been downloaded to your project's \"Library\" directory: {0}", bundletoolJarPath);
                if (EditorUtility.DisplayDialog("Download Complete", message, "OK"))
                {
                    Close();
                }

                return;
            }

            // Download is in progress.
            if (EditorUtility.DisplayCancelableProgressBar(
                "Downloading bundletool", null, _downloadRequest.downloadProgress))
            {
                EditorUtility.ClearProgressBar();
                _downloadRequest.Abort();
                _downloadRequest.Dispose();
                _downloadRequest = null;
                Debug.Log("Cancelled bundletool download.");
            }
        }

        private void OnDestroy()
        {
            if (_downloadRequest != null)
            {
                _downloadRequest.Dispose();
                _downloadRequest = null;
            }
        }

        private void StartDownload()
        {
            Debug.Log("Downloading bundletool...");
            var bundletoolUri = string.Format(
                "https://github.com/google/bundletool/releases/download/{0}/bundletool-all-{0}.jar",
                Bundletool.BundletoolVersion);
#if UNITY_2017_2_OR_NEWER
            var downloadHandler = new DownloadHandlerFile(Bundletool.GetBundletoolJarPath())
            {
                removeFileOnAbort = true
            };
            _downloadRequest = new UnityWebRequest(bundletoolUri, UnityWebRequest.kHttpVerbGET, downloadHandler, null);
            _downloadRequest.SendWebRequest();
#else
            _downloadRequest = UnityWebRequest.Get(bundletoolUri);
            _downloadRequest.Send();
#endif
        }

        private static void CreateButton(string text, Action action)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(text, GUILayout.Width(100)))
            {
                action();
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Displays this window, creating it if necessary.
        /// </summary>
        public static void ShowWindow()
        {
            GetWindow(typeof(BundletoolDownloadWindow), true, "Bundletool Download Required");
        }
    }
}