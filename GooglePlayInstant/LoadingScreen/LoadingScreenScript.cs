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
        private AssetBundle _bundle;

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

            yield return StartCoroutine(GetAssetBundle(loadingScreenConfig.assetBundleUrl));

            if (_bundle == null)
            {
                Debug.LogError("AssetBundle failed to be downloaded.");
            }
            else
            {
                SceneManager.LoadScene(_bundle.GetAllScenePaths()[0]);
            }
        }

        //TODO: Update function for unity 5.6 functionality
        private IEnumerator GetAssetBundle(string assetBundleUrl)
        {
#if UNITY_2018_2_OR_NEWER
            var www = UnityWebRequestAssetBundle.GetAssetBundle(assetBundleUrl);
#else
            var www = UnityWebRequest.GetAssetBundle(assetBundleUrl);
#endif
            yield return www.SendWebRequest();

            // TODO: implement retry logic
            if (www.isNetworkError || www.isHttpError)
            {
                Debug.LogErrorFormat("Error downloading asset bundle: {0}", www.error);
            }
            else
            {
                _bundle = DownloadHandlerAssetBundle.GetContent(www);
            }
        }
    }
}