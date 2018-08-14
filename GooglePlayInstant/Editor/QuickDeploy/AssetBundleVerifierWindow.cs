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
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

[assembly: InternalsVisibleTo("GooglePlayInstant.Tests.Editor.QuickDeploy")]

namespace GooglePlayInstant.Editor.QuickDeploy
{
    /// <summary>
    /// Window that verifies AssetBundles from given URLs.
    /// </summary>
    public class AssetBundleVerifierWindow : EditorWindow
    {
        public enum AssetBundleVerifyState
        {
            InProgress,
            DestinationError,
            WebRequestError,
            BundleError,
            DownloadSuccess
        }
        
        private const int FieldMinWidth = 170;
        internal const string WebRequestErrorFormatMessage = "Problem retrieving AssetBundle from {0}: {1}";
        
        private long _responseCode;
        
        private string _mainScene;
        private double _numOfMegabytes;
        private UnityWebRequest _webRequest;
        private AssetBundle _bundle;
        
        // Visible for testing
        internal bool AssetBundleDownloadIsSuccessful;
        internal string AssetBundleUrl;
        internal string ErrorDescription;

        public AssetBundleVerifyState State;

        /// <summary>
        /// Creates a dialog box that details the success or failure of an AssetBundle retrieval from a given assetBundleUrl.
        /// </summary>
        public static AssetBundleVerifierWindow ShowWindow()
        {
            return (AssetBundleVerifierWindow) GetWindow(typeof(AssetBundleVerifierWindow), true, "Play Instant AssetBundle Verify");
        }

        public void StartAssetBundleVerificationDownload(string assetBundleUrl)
        {
            AssetBundleUrl = assetBundleUrl;
#if UNITY_2018_1_OR_NEWER
            _webRequest = UnityWebRequestAssetBundle.GetAssetBundle(AssetBundleUrl);
            _webRequest.SendWebRequest();
#elif UNITY_2017_1_OR_NEWER
            _webRequest = UnityWebRequest.GetAssetBundle(_assetBundleUrl);
            _webRequest.SendWebRequest();
#else
            _webRequest = UnityWebRequest.GetAssetBundle(_assetBundleUrl);
            _webRequest.Send();
#endif
        }

        // Visible for testing
        internal void HandleAssetBundleVerifyState(AssetBundleVerifyState state, UnityWebRequest webRequest)
        {
            // InProgress state should not be handled.
            switch (state)
            {
                case AssetBundleVerifyState.DestinationError:
                    AssetBundleDownloadIsSuccessful = false;
                    ErrorDescription = "Cannot connect to destination host. See Console log for details.";
                    // No need to log since debugging information in this case is logged from thrown exception.
                    break;
                case AssetBundleVerifyState.WebRequestError:
                    AssetBundleDownloadIsSuccessful = false;
                    ErrorDescription = webRequest.error;
                    Debug.LogErrorFormat(WebRequestErrorFormatMessage, AssetBundleUrl,
                        ErrorDescription);
                    break;
                case AssetBundleVerifyState.BundleError:
                    AssetBundleDownloadIsSuccessful = false;
                    ErrorDescription = "Error extracting AssetBundle. See Console log for details.";
                    // No need to log since debugging information in this case is automatically logged by Unity.
                    break;
                case AssetBundleVerifyState.DownloadSuccess:
                    AssetBundleDownloadIsSuccessful = true;
                    _numOfMegabytes = ConvertBytesToMegabytes(webRequest.downloadedBytes);
                    var scenes = _bundle.GetAllScenePaths();
                    _mainScene = (scenes.Length == 0) ? "No scenes in AssetBundle" : scenes[0];
                    // Free memory used by the AssetBundle since it will not be in use by the Editor. Set to true to destroy
                    // all objects that were loaded from this bundle.
                    _bundle.Unload(true);
                    break;
                default:
                    throw new NotImplementedException(string.Format("Unexpected state {0}", state));
            }
        }

        private AssetBundleVerifyState GetAssetBundleVerifyStateInfoFromDownload()
        {
            if (!_webRequest.isDone)
            {
                return AssetBundleVerifyState.InProgress;
            }

            try
            {
                _bundle = DownloadHandlerAssetBundle.GetContent(_webRequest);
            }
            catch (InvalidOperationException e)
            {
                Debug.LogErrorFormat("Failed to obtain AssetBundle content: {0}", e);
                return AssetBundleVerifyState.DestinationError;
            }

            _responseCode = _webRequest.responseCode;

#if UNITY_2017_1_OR_NEWER
            if (_webRequest.isHttpError || _webRequest.isNetworkError)
#else
            if (_webRequest.isError)
#endif
            {
                return AssetBundleVerifyState.WebRequestError;
            }

            if (_bundle == null)
            {
                return AssetBundleVerifyState.BundleError;
            }

            return AssetBundleVerifyState.DownloadSuccess;
        }

        private static double ConvertBytesToMegabytes(ulong bytes)
        {
            return bytes / 1024f / 1024f;
        }

        private void Update()
        {
            if (_webRequest == null)
            {
                return;
            }

            State = GetAssetBundleVerifyStateInfoFromDownload();

            if (State == AssetBundleVerifyState.InProgress)
            {
                if (EditorUtility.DisplayCancelableProgressBar("AssetBundle Download", "",
                    _webRequest.downloadProgress))
                {
                    _webRequest.Abort();
                    _webRequest.Dispose();
                    _webRequest = null;

                    Debug.Log("Download process was cancelled.");
                }

                return;
            }

            // Performs download operation only once when webrequest is completed.
            EditorUtility.ClearProgressBar();
            HandleAssetBundleVerifyState(State, _webRequest);
            Repaint();

            // Turn request to null to signal ready for next call
            _webRequest.Dispose();
            _webRequest = null;
        }

        private void OnGUI()
        {
            if (State == AssetBundleVerifyState.InProgress)
            {
                EditorGUILayout.LabelField("Loading...");
                return;
            }

            AddVerifyComponentInfo("AssetBundle Download Status:",
                AssetBundleDownloadIsSuccessful ? "SUCCESS" : "FAILED");

            AddVerifyComponentInfo("AssetBundle URL:",
                string.IsNullOrEmpty(AssetBundleUrl) ? "N/A" : AssetBundleUrl);

            AddVerifyComponentInfo("HTTP Status Code:", _responseCode == 0 ? "N/A" : _responseCode.ToString());

            AddVerifyComponentInfo("Error Description:",
                AssetBundleDownloadIsSuccessful ? "N/A" : ErrorDescription);

            AddVerifyComponentInfo("Main Scene:", AssetBundleDownloadIsSuccessful ? _mainScene : "N/A");

            AddVerifyComponentInfo("Size (MB):",
                AssetBundleDownloadIsSuccessful ? _numOfMegabytes.ToString("#.####") : "N/A");

            if (GUILayout.Button("Refresh"))
            {
                StartAssetBundleVerificationDownload(AssetBundleUrl);
            }
        }

        private static void AddVerifyComponentInfo(string title, string response)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(title, GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.LabelField(response, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }
    }
}