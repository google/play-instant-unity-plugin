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

/// <summary>
/// A GenericLoadingScreenScript that contains engine code which is copied over from the plugin folder into the
/// client's "Assets" file. Before the copying occurs, the assetbundle url is inserted into the script at
/// __ASSETBUNDLEURL__and "Generic" is removed from the script's name.
/// </summary>
public class GenericLoadingScreenScript : MonoBehaviour
{
    private AssetBundle _bundle;

    private IEnumerator Start()
    {
        yield return StartCoroutine(GetAssetBundle());
        SceneManager.LoadScene(_bundle.GetAllScenePaths()[0]);
    }

    //TODO: Update function for unity 5.6 functionality
    private IEnumerator GetAssetBundle()
    {
#if UNITY_2018_2_OR_NEWER
        var www = UnityWebRequestAssetBundle.GetAssetBundle("__ASSETBUNDLEURL__");
#else
        var www = UnityWebRequest.GetAssetBundle("__ASSETBUNDLEURL__");
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