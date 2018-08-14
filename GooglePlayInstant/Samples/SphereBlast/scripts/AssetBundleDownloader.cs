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
using System.Collections.Generic;
using UnityEngine;

public class AssetBundleDownloader : MonoBehaviour {

	// Use this for initialization
	public string assetBundleURL;

	IEnumerator Start () {
		// Cleans local cache of asset bundles. 
		// This here for download validation purposes
		// and should be removed from published builds
		Caching.ClearCache ();
		// Download and load required scenes
		yield return StartCoroutine (
			DownloadAsset (assetBundleURL, true));
	}

	IEnumerator DownloadAsset (string sceneURL, bool loadScene) {
		// Downloads and loads scenes
		WWW bundleWWW = WWW.LoadFromCacheOrDownload (sceneURL, 0);
		yield return bundleWWW;
		var assetBundle = bundleWWW.assetBundle;
		if (loadScene) {
			if (assetBundle.isStreamedSceneAssetBundle) {
				string[] scenePaths = assetBundle.GetAllScenePaths ();
				string sceneName =
					System.IO.Path.GetFileNameWithoutExtension (scenePaths[0]);
				UnityEngine.SceneManagement.SceneManager.LoadScene (sceneName);
			}
		}
	}
}