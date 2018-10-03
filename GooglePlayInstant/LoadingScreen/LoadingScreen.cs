﻿// Copyright 2018 Google LLC
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

using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GooglePlayInstant.LoadingScreen
{
    /// <summary>
    /// Downloads the AssetBundle available at AssetBundleUrl and updates LoadingBar with its progress
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        [Tooltip("The url used to fetch the AssetBundle on Start")]
        public string AssetBundleUrl;

        [Tooltip("The LoadingBar used to indicated download and install progress")]
        public LoadingBar LoadingBar;

        public RawImage Background;

        private const int MaxAttemptCount = 3;
        private AssetBundle _bundle;
        private int _assetBundleRetrievalAttemptCount;

        private IEnumerator Start()
        {
            while (_bundle == null && _assetBundleRetrievalAttemptCount < MaxAttemptCount)
            {
                yield return GetAssetBundle(AssetBundleUrl);
            }

            if (_bundle == null)
            {
                // TODO: Develop UI for when AssetBundle download fails, e.g. a user prompt with a retry button.
                Debug.LogErrorFormat("Failed to download AssetBundle after {0} attempts.", MaxAttemptCount);
                yield break;
            }

            var sceneLoadOperation = SceneManager.LoadSceneAsync(_bundle.GetAllScenePaths()[0]);
            yield return LoadingBar.FillUntilDone(sceneLoadOperation, LoadingBar.AssetBundleDownloadMaxProportion, 1f);
        }

        private IEnumerator GetAssetBundle(string assetBundleUrl)
        {
            UnityWebRequest webRequest;
            var downloadOperation = StartAssetBundleDownload(assetBundleUrl, out webRequest);

            yield return LoadingBar.FillUntilDone(downloadOperation,
                0f, LoadingBar.AssetBundleDownloadMaxProportion);

            if (IsFailedRequest(webRequest))
            {
                yield return HandleRequestFailed(assetBundleUrl, webRequest);
            }
            else
            {
                _bundle = DownloadHandlerAssetBundle.GetContent(webRequest);
            }
        }

        private IEnumerator HandleRequestFailed(string assetBundleUrl, UnityWebRequest webRequest)
        {
            if (_assetBundleRetrievalAttemptCount < MaxAttemptCount)
            {
                _assetBundleRetrievalAttemptCount++;
                Debug.LogFormat("Attempt #{0} at downloading AssetBundle...", _assetBundleRetrievalAttemptCount);
                yield return new WaitForSeconds(2);

                //TODO: revisit this methodology of setting the loading bar
                LoadingBar.SetProgress(0f);

                yield return GetAssetBundle(assetBundleUrl);
            }
            else
            {
                Debug.LogErrorFormat("Error downloading asset bundle: {0}", webRequest.error);
            }
        }

        private static bool IsFailedRequest(UnityWebRequest webRequest)
        {
#if UNITY_2017_1_OR_NEWER
            return webRequest.isHttpError || webRequest.isNetworkError;
#else
            return webRequest.isError;
#endif
        }

        private static AsyncOperation StartAssetBundleDownload(string assetBundleUrl, out UnityWebRequest webRequest)
        {
#if UNITY_2018_1_OR_NEWER
            webRequest = UnityWebRequestAssetBundle.GetAssetBundle(assetBundleUrl);
#else
            webRequest = UnityWebRequest.GetAssetBundle(assetBundleUrl);
#endif

#if UNITY_2017_2_OR_NEWER
            var assetBundleDownloadOperation = webRequest.SendWebRequest();
#else
            var assetBundleDownloadOperation = webRequest.Send();
#endif

            return assetBundleDownloadOperation;
        }
    }
}