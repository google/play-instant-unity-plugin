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

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using GooglePlayInstant.LoadingScreen;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[assembly: InternalsVisibleTo("GooglePlayInstant.Tests.Editor.QuickDeploy")]

namespace GooglePlayInstant.Editor.QuickDeploy
{
    /// <summary>
    /// Class that generates Unity loading scenes for instant apps.
    /// </summary>
    public class LoadingScreenGenerator
    {
        public const string SceneName = "PlayInstantLoadingScreen.unity";

        public static readonly string SceneDirectoryPath =
            Path.Combine("Assets", "PlayInstantLoadingScreen");

        private const string CanvasName = "Loading Screen Canvas";

        private const string SaveErrorTitle = "Loading Screen Save Error";

        private const int ReferenceWidth = 1080;
        private const int ReferenceHeight = 1920;

        private static readonly string SceneFilePath =
            Path.Combine(SceneDirectoryPath, SceneName);

        /// <summary>
        /// Creates a scene in the current project that acts as a loading scene until assetbundles are
        /// downloaded from the CDN. Takes in a loadingScreenImagePath, a path to the image shown in the loading scene,
        /// and an assetbundle URL. Replaces the current loading scene with a new one if it exists.
        /// </summary>
        public static void GenerateScene(string assetBundleUrl, Texture2D loadingScreenImage)
        {
            if (string.IsNullOrEmpty(assetBundleUrl))
            {
                throw new ArgumentException("AssetBundle URL text field cannot be null or empty.");
            }

            // Removes the loading scene if it is present, otherwise does nothing.
            EditorSceneManager.CloseScene(SceneManager.GetSceneByName(Path.GetFileNameWithoutExtension(SceneName)),
                true);

            var loadingScreenScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            PopulateScene(loadingScreenImage, assetBundleUrl);

            bool saveOk = EditorSceneManager.SaveScene(loadingScreenScene, SceneFilePath);

            if (!saveOk)
            {
                // Not a fatal issue. User can attempt to resave this scene.
                var warningMessage = string.Format("Issue while saving scene {0}.",
                    SceneName);

                Debug.LogWarning(warningMessage);

                DialogHelper.DisplayMessage(SaveErrorTitle, warningMessage);
            }
            else
            {
                AssetDatabase.Refresh();
                EditorApplication.delayCall += () =>
                {
                    //TODO: move this to DialogHelper
                    if (EditorUtility.DisplayDialog("Change Scenes in Build",
                        "Would you like to replace any existing Scenes in Build with the loading screen scene?", "Yes",
                        "No"))
                    {
                        SetMainSceneInBuild(SceneFilePath);
                    }
                };
            }
        }

        public static bool LoadingScreenExists()
        {
            return SceneManager.GetSceneByName(Path.GetFileNameWithoutExtension(SceneName)).IsValid();
        }

        public static GameObject GetLoadingScreenCanvasObject()
        {
            return GameObject.Find(CanvasName);
        }

        // Visible for testing
        internal static void SetMainSceneInBuild(string pathToScene)
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(pathToScene, true)
            };
        }

        private static void PopulateScene(Texture backgroundTexture, string assetBundleUrl)
        {
            var loadingScreenGameObject = new GameObject("Loading Screen");

            var camera = GenerateCamera();
            camera.transform.SetParent(loadingScreenGameObject.transform, false);

            var canvasObject = GenerateCanvas(camera);
            canvasObject.transform.SetParent(loadingScreenGameObject.transform, false);

            var backgroundImage = GenerateBackground(backgroundTexture);
            backgroundImage.transform.SetParent(canvasObject.transform, false);

            var loadingScreen = loadingScreenGameObject.AddComponent<LoadingScreen.LoadingScreen>();
            loadingScreen.AssetBundleUrl = assetBundleUrl;
            loadingScreen.Background = backgroundImage;
            loadingScreen.LoadingBar = LoadingBarGenerator.GenerateLoadingBar();
            loadingScreen.LoadingBar.transform.SetParent(canvasObject.transform, false);
        }

        private static Camera GenerateCamera()
        {
            var cameraObject = new GameObject("UI Camera");

            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = ReferenceHeight / 2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.white;

            return camera;
        }

        private static GameObject GenerateCanvas(Camera camera)
        {
            var canvasObject = new GameObject(CanvasName);

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;

            var canvasScaler = canvasObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            canvasScaler.matchWidthOrHeight = 0f;

            return canvasObject;
        }

        private static RawImage GenerateBackground(Texture backgroundTexture)
        {
            var backgroundObject = new GameObject("Background");

            var backgroundImage = backgroundObject.AddComponent<RawImage>();
            backgroundImage.texture = backgroundTexture;

            var backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero; // Scale with parent.
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.sizeDelta = Vector2.zero;

            var backgroundAspectRatioFitter = backgroundObject.AddComponent<AspectRatioFitter>();
            backgroundAspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            if (backgroundImage.texture == null)
            {
                backgroundAspectRatioFitter.aspectRatio = ReferenceWidth / (float) ReferenceHeight;
            }
            else
            {
                backgroundAspectRatioFitter.aspectRatio =
                    backgroundImage.texture.width / (float) backgroundImage.texture.height;
            }

            return backgroundImage;
        }

        // Visible for testing
        internal static void UpdateBackgroundImage(Texture backgroundTexture)
        {
            var loadingScreen = GameObject.FindObjectOfType<LoadingScreen.LoadingScreen>();

            if (backgroundTexture != null)
                loadingScreen.Background.texture = backgroundTexture;
        }
    }
}