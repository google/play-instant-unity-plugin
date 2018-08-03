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
using UnityEngine.Networking;

namespace GooglePlayInstant.Editor.QuickDeploy
{
    /// <summary>
    /// Window that verifies AssetBundles from given URLs.
    /// </summary>
    public class AssetBundleVerifierWindow : EditorWindow
    {
        private const int FieldMinWidth = 170;

        private bool _assetBundleDownloadIsSuccessful;
        private string _assetBundleUrl;
        private long _responseCode;
        private string _errorDescription;
        private string _mainScene;
        private double _numOfMegabytes;
        private UnityWebRequest _webRequest;

        /// <summary>
        /// Creates a dialog box that details the success or failure of an AssetBundle retrieval from a given assetBundleUrl.
        /// </summary>
        public static void ShowWindow()
        {
            // Set AssetBundle url in a private variable so that information displayed in window is consistent with
            // the url that this was called on. 
            var window = (AssetBundleVerifierWindow) GetWindow(typeof(AssetBundleVerifierWindow), true,
                "Play Instant AssetBundle Verify");
            window._assetBundleUrl = QuickDeployConfig.Config.assetBundleUrl;

            window.StartAssetBundleVerificationDownload();
        }

        //TODO: Support Unity 5.6.0+
        private void StartAssetBundleVerificationDownload()
        {
            _webRequest = UnityWebRequestAssetBundle.GetAssetBundle(_assetBundleUrl);
            _webRequest.SendWebRequest();
        }

        //TODO: Support Unity 5.6.0+
        private void GetAssetBundleInfoFromDownload()
        {
            var bundle = DownloadHandlerAssetBundle.GetContent(_webRequest);

            _responseCode = _webRequest.responseCode;

            if (_webRequest.isNetworkError || _webRequest.isHttpError)
            {
                _assetBundleDownloadIsSuccessful = false;
                _errorDescription = _webRequest.error;
                Debug.LogErrorFormat("Problem retrieving AssetBundle from {0}: {1}", _assetBundleUrl,
                    _errorDescription);
            }
            else if (bundle == null)
            {
                _assetBundleDownloadIsSuccessful = false;
                _errorDescription = "Error extracting AssetBundle. See Console log for details.";
                // No need to log since debugging information in this case is automatically logged by Unity.
            }
            else
            {
                _assetBundleDownloadIsSuccessful = true;
                _numOfMegabytes = ConvertBytesToMegabytes(_webRequest.downloadedBytes);

                var scenes = bundle.GetAllScenePaths();
                _mainScene = (scenes.Length == 0) ? "No scenes in AssetBundle" : scenes[0];

                // Free memory used by the AssetBundle since it will not be in use by the Editor. Set to true to destroy
                // all objects that were loaded from this bundle.
                bundle.Unload(true);
            }
        }

        private double ConvertBytesToMegabytes(ulong bytes)
        {
            return bytes / 1024f / 1024f;
        }

        private void Update()
        {
            if (_webRequest == null || !_webRequest.isDone)
            {
                return;
            }

            // Performs download operation only once when webrequest is completed.
            GetAssetBundleInfoFromDownload();
            Repaint();

            // Turn request to null to signal ready for next call
            _webRequest.Dispose();
            _webRequest = null;
        }

        // TODO: fix crashing associated with using DisplayProgressBar()
        private void OnGUI()
        {
            if (_webRequest != null && !_webRequest.isDone)
            {
                EditorUtility.DisplayProgressBar("AssetBundle Download", "",
                    _webRequest.downloadProgress);
            }
            else
            {
                EditorUtility.ClearProgressBar();

                AddVerifyComponentInfo("AssetBundle Download Status:",
                    _assetBundleDownloadIsSuccessful ? "SUCCESS" : "FAILED");

                AddVerifyComponentInfo("AssetBundle URL:",
                    string.IsNullOrEmpty(_assetBundleUrl) ? "N/A" : _assetBundleUrl);

                AddVerifyComponentInfo("HTTP Status Code:", _responseCode == 0 ? "N/A" : _responseCode.ToString());

                AddVerifyComponentInfo("Error Description:",
                    _assetBundleDownloadIsSuccessful ? "N/A" : _errorDescription);

                AddVerifyComponentInfo("Main Scene:", _assetBundleDownloadIsSuccessful ? _mainScene : "N/A");

                AddVerifyComponentInfo("Size (MB):",
                    _assetBundleDownloadIsSuccessful ? _numOfMegabytes.ToString("#.####") : "N/A");

                if (GUILayout.Button("Refresh"))
                {
                    StartAssetBundleVerificationDownload();
                }
            }
        }

        private void AddVerifyComponentInfo(string title, string response)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(title, GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.LabelField(response, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }
    }
}