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
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

namespace GooglePlayInstant.LoadingScreen
{
    /// <summary>
    /// Class that encapsulates the creation of the LoadingBar component
    /// </summary>
    public static class LoadingBarGenerator
    {
        // Loading bar fill's padding against the loading bar outline.
        private const int LoadingBarFillPadding = 17;

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
            var progressHolderObject = new GameObject(ProgressName);
           
            var loadingBarObject = new GameObject(RootName);
            var loadingBar = loadingBarObject.AddComponent<LoadingBar>();
            loadingBar.Outline = GenerateImage(OutlineName, Color.black);
            loadingBar.Background = GenerateImage(BackgroundName, Color.white);
            loadingBar.ProgressFill = GenerateImage(FillName, Color.grey);
            loadingBar.ProgressHolder = progressHolderObject.AddComponent<RectTransform>();
            
            loadingBar.Outline.SetParent(loadingBar.transform, false);
            loadingBar.Background.transform.SetParent(loadingBar.transform, false);
            loadingBar.ProgressFill.transform.SetParent(progressHolderObject.transform, false);
            loadingBar.ProgressHolder.transform.SetParent(loadingBar.transform, false);
            
            //loadingBar.
            
            return loadingBar;
        }

        private static RectTransform GenerateImage(string name, Color color)
        {
            var imageObject = new GameObject(name);
            
            var image = imageObject.AddComponent<Image>();
            image.color = color;
            
            var rectTransform = imageObject.AddComponent<RectTransform>();
            return rectTransform;
        }
    }
}