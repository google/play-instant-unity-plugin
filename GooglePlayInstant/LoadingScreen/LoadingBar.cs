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
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GooglePlayInstant.LoadingScreen
{
    /// <summary>
    /// Class that encapsulates the creation and update of a Loading Bar component in the editor and during the game's
    /// runtime.
    /// </summary>
    public static class LoadingBar
    {
        // TODO: revisit these arbitrarily chosen values
        /// <summary>
        /// Percentage of the loading bar allocated to the the asset bundle downloading process.
        /// </summary>
        public const float AssetBundleDownloadMaxWidthPercentage = .8f;

        /// <summary>
        /// Percentage of the loading bar allocated to the the scene loading process.
        /// </summary>
        public const float SceneLoadingMaxWidthPercentage = 1 - AssetBundleDownloadMaxWidthPercentage;

        // Loading bar fill's padding against the loading bar outline.
        private const int LoadingBarFillPadding = 17;

        private const int LoadingBarHeight = 30;

        // Loading bar width as a percentage canvas object's automatic size
        private const float LoadingBarWidthPercentage = .5f;

        // Loading bar y axis placement as a percentage of canvas object's automatic y value
        private const float LoadingBarYAxisPercentage = 2f;

        // Names for the gameobject components
        private const string LoadingBarGameObjectName = "Loading Bar";
        private const string LoadingBarOutlineGameObjectName = "Loading Bar Outline";
        private const string LoadingBarFillGameObjectName = "Loading Bar Fill";

#if UNITY_EDITOR
        // TODO: fix persistance of loading bar
        /// <summary>
        /// Creates a loading bar component on the specified loading screen game object's bottom half. Consists of
        /// a white rounded border, with a colored loading bar fill in the middle.
        /// </summary>
        public static void AddLoadingScreenBarComponent(GameObject loadingScreenGameObject)
        {
            var loadingBarGameObject = new GameObject(LoadingBarGameObjectName);
            loadingBarGameObject.AddComponent<RectTransform>();
            loadingBarGameObject.transform.SetParent(loadingScreenGameObject.transform);

            var loadingBarRectTransform = loadingBarGameObject.GetComponent<RectTransform>();

            var loadingScreenRectTransform = loadingScreenGameObject.GetComponent<RectTransform>();

            // Set the size of the loading bar. LoadingBarWidthPercentage gives loading bar padding from the edges
            // of the viewing device
            loadingBarRectTransform.sizeDelta =
                new Vector2(loadingScreenRectTransform.sizeDelta.x * LoadingBarWidthPercentage,
                    LoadingBarHeight);

            // Set the position of the loading bar
            loadingBarRectTransform.position =
                new Vector2(loadingScreenRectTransform.position.x,
                    loadingScreenRectTransform.position.y -
                    LoadingBarYAxisPercentage * loadingScreenRectTransform.position.y);

            SetLoadingBarOutline(loadingBarGameObject);
            SetLoadingBarFill(loadingBarGameObject);
        }

        // TODO: check for compatibilty with unity 5.6+
        private static void SetLoadingBarOutline(GameObject loadingBarGameObject)
        {
            var loadingBarOutlineGameObject = new GameObject(LoadingBarOutlineGameObjectName);
            loadingBarOutlineGameObject.transform.SetParent(loadingBarGameObject.transform);

            loadingBarOutlineGameObject.AddComponent<Image>();

            var loadingBarOutlineImage = loadingBarOutlineGameObject.GetComponent<Image>();
            loadingBarOutlineImage.sprite =
                AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd");

            loadingBarOutlineImage.type = Image.Type.Sliced;
            loadingBarOutlineImage.fillCenter = false;

            // Set size of component
            var loadingBarOutlineRectTransform = loadingBarOutlineGameObject.GetComponent<RectTransform>();
            var loadingBarRectTransform = loadingBarGameObject.GetComponent<RectTransform>();
            loadingBarOutlineRectTransform.sizeDelta = loadingBarRectTransform.sizeDelta;

            // Position outline component
            loadingBarOutlineGameObject.transform.position = loadingBarGameObject.transform.position;
        }

        private static void SetLoadingBarFill(GameObject loadingBarGameObject)
        {
            var loadingBarFillGameObject = new GameObject(LoadingBarFillGameObjectName);
            loadingBarFillGameObject.transform.SetParent(loadingBarGameObject.transform);

            loadingBarFillGameObject.AddComponent<Image>();
            var loadingBarFillImage = loadingBarFillGameObject.GetComponent<Image>();
            loadingBarFillImage.color = Color.green;

            // Set size of component
            var loadingBarFillRectTransform = loadingBarFillGameObject.GetComponent<RectTransform>();

            var loadingBarRectTransform = loadingBarGameObject.GetComponent<RectTransform>();
            loadingBarFillRectTransform.sizeDelta = new Vector2(0,
                loadingBarRectTransform.sizeDelta.y - LoadingBarFillPadding);

            // Position fill component
            loadingBarFillGameObject.transform.position = loadingBarGameObject.transform.position;
        }
#endif

        /// <summary>
        /// Updates a loading bar by the progress made by an asynchronous operation up to a specific percentage of
        /// the loading bar. 
        /// </summary>
        public static IEnumerator UpdateLoadingBar(AsyncOperation operation, float percentageOfLoadingBar)
        {
            var loadingBarRectTransform = GameObject.Find(LoadingBarGameObjectName).GetComponent<RectTransform>();

            var loadingBarFillRectTransform =
                GameObject.Find(LoadingBarFillGameObjectName).GetComponent<RectTransform>();

            // Total amount of space the loading bar can occupy
            var loadingBarFillMaxWidth = loadingBarRectTransform.sizeDelta.x - LoadingBarFillPadding;

            // Percentage of space that is allocated for this async operation
            var loadingMaxWidth = loadingBarFillMaxWidth * percentageOfLoadingBar;

            // Current width of the loading bar
            var currentLoadingBarFill = loadingBarFillRectTransform.sizeDelta.x;

            var loadingIsDone = false;

            while (!loadingIsDone)
            {
                loadingBarFillRectTransform.sizeDelta = new Vector2(
                    currentLoadingBarFill + loadingMaxWidth * operation.progress,
                    loadingBarFillRectTransform.sizeDelta.y);

                // Changing the width of the rectangle makes it shorter (or larger) on both sides--thus requiring
                // the rectangle's x position to be moved left by half the amount it's been shortened.
                loadingBarFillRectTransform.position = new Vector2(
                    loadingBarRectTransform.position.x -
                    (loadingBarFillMaxWidth - loadingBarFillRectTransform.sizeDelta.x) / 2f,
                    loadingBarFillRectTransform.position.y);

                if (operation.isDone)
                {
                    loadingIsDone = true;
                }

                yield return null;
            }
        }
    }
}