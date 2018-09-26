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

using UnityEngine;
using UnityEngine.UI;

namespace GooglePlayInstant.LoadingScreen
{
    /// <summary>
    /// Class that encapsulates the creation of the LoadingBar component
    /// </summary>
    public static class LoadingBarGenerator
    {
        // Loading bar size as a proportion of the screen size. Adjust if needed.
        private const float LoadingBarWidthProportion = 0.7f;
        private const float LoadingBarHeightProportion = 0.02f;

        // Loading bar placement as a proportion of the screen size relative to the bottom left corner. Adjust if needed.
        private const float LoadingBarPositionX = 0.5f;
        private const float LoadingBarPositionY = 0.3f;

        // Names for the gameobject components
        private const string RootName = "Loading Bar";
        private const string OutlineName = "Outline";
        private const string BackgroundName = "Background";
        private const string FillName = "Fill";
        private const string ProgressName = "Progress";

        public static LoadingBar GenerateLoadingBar()
        {
            var progressHolderObject = GenerateUiObject(ProgressName);

            var loadingBarObject = GenerateUiObject(RootName);

            var loadingBar = loadingBarObject.AddComponent<LoadingBar>();
            loadingBar.Outline = GenerateImage(loadingBarObject, OutlineName, Color.black);
            loadingBar.Background = GenerateImage(loadingBarObject, BackgroundName, Color.white);
            loadingBar.ProgressFill = GenerateImage(progressHolderObject, FillName, Color.grey);

            loadingBar.ProgressHolder = progressHolderObject.GetComponent<RectTransform>();
            loadingBar.ProgressHolder.transform.SetParent(loadingBar.transform, false);
            SetAnchorsToScaleWithParent(loadingBar.ProgressHolder);

            var loadingBarRectTransform = loadingBarObject.GetComponent<RectTransform>();
            loadingBarRectTransform.anchorMin = new Vector2(LoadingBarPositionX - LoadingBarWidthProportion / 2f,
                LoadingBarPositionY - LoadingBarHeightProportion / 2f);
            loadingBarRectTransform.anchorMax = new Vector2(LoadingBarPositionX + LoadingBarWidthProportion / 2f,
                LoadingBarPositionY + LoadingBarHeightProportion / 2f);
            loadingBarRectTransform.sizeDelta = Vector2.zero;

            return loadingBar;
        }

        private static RectTransform GenerateImage(GameObject parent, string name, Color color)
        {
            var imageObject = GenerateUiObject(name);
            imageObject.transform.SetParent(parent.transform, false);

            var image = imageObject.AddComponent<Image>();
            image.color = color;

            var rectTransform = imageObject.GetComponent<RectTransform>();

            return rectTransform;
        }

        //Creates a new GameObject with a RectTransform instead of a normal Transform
        private static GameObject GenerateUiObject(string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rectTransform = gameObject.GetComponent<RectTransform>();
            SetAnchorsToScaleWithParent(rectTransform);

            return gameObject;
        }

        private static void SetAnchorsToScaleWithParent(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
        }
    }
}