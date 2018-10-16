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

        [Tooltip("The button displayed when a download error occurs. Restarts the download when clicked.")]
        public Button RetryButton;

        private const int MaxAttemptCount = 3;
        private AssetBundle _bundle;
        private int _assetBundleRetrievalAttemptCount;
        private float _maxLoadingBarProgress;
        private bool _downloading;

        private void Start()
        {
            RetryButton.onClick.AddListener(AttemptAssetBundleDownload);
            AttemptAssetBundleDownload();
        }

        public void AttemptAssetBundleDownload()
        {
            if (_downloading)
            {
                return;
            }

            HideRetryButton();
            _assetBundleRetrievalAttemptCount = 0;
            _maxLoadingBarProgress = 0f;
            StartCoroutine(AttemptAssetBundleDownloadCo());
        }

        private IEnumerator AttemptAssetBundleDownloadCo()
        {
            _downloading = true;

            while (_bundle == null && _assetBundleRetrievalAttemptCount < MaxAttemptCount)
            {
                yield return GetAssetBundle(AssetBundleUrl);
            }

            if (_bundle == null)
            {
                ShowRetryButton();
                _downloading = false;
                yield break;
            }

            var sceneLoadOperation = SceneManager.LoadSceneAsync(_bundle.GetAllScenePaths()[0]);
            var installStartFill = Mathf.Max(LoadingBar.AssetBundleDownloadToInstallRatio, _maxLoadingBarProgress);
            yield return LoadingBar.FillUntilDone(sceneLoadOperation, installStartFill, 1f, false);

            _downloading = false;
        }

        private IEnumerator GetAssetBundle(string assetBundleUrl)
        {
            UnityWebRequest webRequest;
            var downloadOperation = StartAssetBundleDownload(assetBundleUrl, out webRequest);

            yield return LoadingBar.FillUntilDone(downloadOperation,
                _maxLoadingBarProgress, LoadingBar.AssetBundleDownloadToInstallRatio, true);

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
                _maxLoadingBarProgress = LoadingBar.Progress;
                Debug.LogFormat("Attempt #{0} at downloading AssetBundle...", _assetBundleRetrievalAttemptCount);
                yield return new WaitForSeconds(0.5f);
                yield return GetAssetBundle(assetBundleUrl);
            }
        }

        private void ShowRetryButton()
        {
            LoadingBar.gameObject.SetActive(false);
            RetryButton.gameObject.SetActive(true);
        }

        private void HideRetryButton()
        {
            LoadingBar.gameObject.SetActive(true);
            RetryButton.gameObject.SetActive(false);
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