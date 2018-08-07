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
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace GooglePlayInstant.LoadingScreen
{
    /// <summary>
    /// Script that starts on the loading screen. It works to download the game's AssetBundle from a specified url and
    /// loads the first scene to start the game.
    /// </summary>
    public class LoadingScreenScript : MonoBehaviour
    {
        private const int MaxAttemptCount = 3;
        private AssetBundle _bundle;
        private int _assetBundleRetrievalAttemptCount;


        private IEnumerator Start()
        {
            var loadingScreenConfigJsonTextAsset =
                Resources.Load<TextAsset>("LoadingScreenConfig");

            if (loadingScreenConfigJsonTextAsset == null)
            {
                throw new FileNotFoundException("LoadingScreenConfig.json missing in Resources folder.");
            }

            var loadingScreenConfigJson = loadingScreenConfigJsonTextAsset.ToString();

            var loadingScreenConfig = JsonUtility.FromJson<LoadingScreenConfig>(loadingScreenConfigJson);

            yield return GetAssetBundle(loadingScreenConfig.assetBundleUrl);

            if (_bundle == null)
            {
                Debug.LogError("AssetBundle failed to be downloaded.");
            }
            else
            {
                var sceneLoadOperation = SceneManager.LoadSceneAsync(_bundle.GetAllScenePaths()[0]);
                yield return LoadingBar.UpdateLoadingBar(sceneLoadOperation, LoadingBar.SceneLoadingMaxWidthPercentage);
            }
        }

        private IEnumerator GetAssetBundle(string assetBundleUrl)
        {
#if UNITY_2018_1_OR_NEWER
            var webRequest = UnityWebRequestAssetBundle.GetAssetBundle(assetBundleUrl);
            var assetbundleDownloadOperation = webRequest.SendWebRequest();
#elif UNITY_2017_1_OR_NEWER
            var webRequest = UnityWebRequest.GetAssetBundle(assetBundleUrl);
            var assetbundleDownloadOperation = webRequest.SendWebRequest();
#else
            var webRequest = UnityWebRequest.GetAssetBundle(assetBundleUrl);
            var assetbundleDownloadOperation = webRequest.Send();
#endif
            yield return LoadingBar.UpdateLoadingBar(assetbundleDownloadOperation,
                LoadingBar.AssetBundleDownloadMaxWidthPercentage);

#if UNITY_2017_1_OR_NEWER
            if (webRequest.isHttpError || webRequest.isNetworkError)
#else
            if (webRequest.isError)
#endif
            {
                if (_assetBundleRetrievalAttemptCount < MaxAttemptCount)
                {
                    _assetBundleRetrievalAttemptCount++;
                    Debug.LogFormat("Attempt #{0} at downloading AssetBundle...", _assetBundleRetrievalAttemptCount);
                    yield return new WaitForSeconds(2);
                    yield return GetAssetBundle(assetBundleUrl);
                }
                else
                {
                    Debug.LogErrorFormat("Error downloading asset bundle: {0}", webRequest.error);
                }
            }
            else
            {
                _bundle = DownloadHandlerAssetBundle.GetContent(webRequest);
            }
        }
    }
}