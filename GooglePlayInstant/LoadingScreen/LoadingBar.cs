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

        // Loading bar size as a proportion of the screen size. Adjust if needed.
        private const float LoadingBarWidthProportion = 0.7f;
        private const float LoadingBarHeightProportion = 0.02f;

        // Loading bar placement as a proportion of the screen size relative to the bottom left corner. Adjust if needed.
        private const float LoadingBarPositionX = 0.5f;
        private const float LoadingBarPositionY = 0.3f;

        // Names for the gameobject components
        private const string LoadingBarGameObjectName = "Loading Bar";
        private const string LoadingBarOutlineGameObjectName = "Loading Bar Outline";
        private const string LoadingBarFillGameObjectName = "Loading Bar Fill";

#if UNITY_EDITOR
        /// <summary>
        /// Creates a loading bar component on the specified loading screen game object's bottom half. Consists of
        /// a white rounded border, with a colored loading bar fill in the middle.
        /// </summary>
        public static void AddLoadingScreenBarComponent(GameObject loadingScreenGameObject)
        {
            var loadingBarGameObject = new GameObject(LoadingBarGameObjectName);
            loadingBarGameObject.AddComponent<RectTransform>();
            loadingBarGameObject.transform.SetParent(loadingScreenGameObject.transform);

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

            // Position fill component
            loadingBarFillGameObject.transform.position = loadingBarGameObject.transform.position;
        }
#endif

        /// <summary>
        /// Sets the position and size of the loading bar as a function of the screen heighta and width. 
        /// </summary>
        public static void UpdateSizeAndPostition()
        {
            var loadingBarRectTransform = GameObject.Find(LoadingBarGameObjectName).GetComponent<RectTransform>();
            var loadingBarFillRectTransform =
                GameObject.Find(LoadingBarFillGameObjectName).GetComponent<RectTransform>();
            var loadingBarOutlineRectTransform =
                GameObject.Find(LoadingBarOutlineGameObjectName).GetComponent<RectTransform>();

            loadingBarRectTransform.position =
                new Vector2(Screen.width * LoadingBarPositionX, Screen.height * LoadingBarPositionY);

            loadingBarRectTransform.sizeDelta = new Vector2(Screen.width * LoadingBarWidthProportion,
                Screen.height * LoadingBarHeightProportion);

            loadingBarOutlineRectTransform.sizeDelta = loadingBarRectTransform.sizeDelta;
            loadingBarFillRectTransform.sizeDelta =
                new Vector2(0.0f, loadingBarRectTransform.sizeDelta.y - LoadingBarFillPadding);
        }

        /// <summary>
        /// Resets the loading bar back to 0 percent.
        /// </summary>
        public static void Reset()
        {
            var loadingBarFillRectTransform =
                GameObject.Find(LoadingBarFillGameObjectName).GetComponent<RectTransform>();

            loadingBarFillRectTransform.sizeDelta =
                new Vector2(0.0f, loadingBarFillRectTransform.sizeDelta.y);
        }


        /// <summary>
        /// Updates a loading bar by the progress made by an asynchronous operation up to a specific percentage of
        /// the loading bar. 
        /// </summary>
        public static IEnumerator Update(AsyncOperation operation, float percentageOfLoadingBar)
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