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
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

namespace GooglePlayInstant.LoadingScreen
{
    /// <summary>
    /// Presents download progress to the user
    /// </summary>
    [ExecuteInEditMode]
    public class LoadingBar : MonoBehaviour
    {
        public float OutlineWidth = 10f;
        public float InnerBorderWidth = 10f;

        [Tooltip("If true, this object's RectTransform will update to adjust the outline and border width")]
        public bool ResizeAutomatically = true;

        [Range(0f, 1f)] public float Progress = 0.25f;

        public RectTransform Background;
        public RectTransform Outline;
        public RectTransform ProgressHolder;
        public RectTransform ProgressFill;

        [Tooltip("Percentage of the loading bar allocated to the asset bundle downloading process.")]
        public float AssetBundleDownloadMaxWidthPercentage = .8f;

        private RectTransform _rectTransform;

        /// <summary>
        /// Percentage of the loading bar allocated to the the scene loading process.
        /// </summary>
        public float SceneLoadingMaxWidthPercentage
        {
            get { return 1f - AssetBundleDownloadMaxWidthPercentage; }
        }

        private void Start()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void Update()
        {
            if (ResizeAutomatically)
            {
                ApplyBorderWidth();
                SetProgress(Progress);
            }
        }

        // TODO: Make sure this scales correctly in landscape
        public void ApplyBorderWidth()
        {
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();

            Outline.anchorMin = Vector3.zero;
            Outline.anchorMax = Vector3.one;
            Outline.sizeDelta = Vector2.one * (OutlineWidth + InnerBorderWidth);

            Background.anchorMin = Vector3.zero;
            Background.anchorMax = Vector3.one;
            Background.sizeDelta = Vector2.one * (InnerBorderWidth);
        }

        public void SetProgress(float proportionOfLoadingBar)
        {
            Progress = proportionOfLoadingBar;

            if (ProgressFill != null)
                ProgressFill.anchorMax = new Vector2(proportionOfLoadingBar, ProgressFill.anchorMax.y);
        }

        /// <summary>
        /// Updates a loading bar by the progress made by an asynchronous operation up to a specific percentage of
        /// the loading bar. 
        /// </summary>
        public IEnumerator FillUntilDone(AsyncOperation operation, float percentageOfLoadingBar)
        {
            var isDone = false;
            while (!isDone)
            {
                SetProgress(operation.progress);

                if (operation.isDone)
                {
                    isDone = true;
                }

                yield return null;
            }
        }
    }
}