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
using GooglePlayInstant.LoadingScreen;
using UnityEditor;
using UnityEditor.SceneManagement;
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

        public static LoadingScreen.LoadingScreen CurrentLoadingScreen { get; private set; }

        private const string CanvasName = "Loading Screen Canvas";

        private const string SaveErrorTitle = "Loading Screen Save Error";

        private const int ReferenceWidth = 1080;
        private const int ReferenceHeight = 1920;

        private static readonly string DefaultSceneFilePath = Path.Combine(SceneDirectoryPath, SceneName);

        /// <summary>
        /// Creates a scene in the current project that acts as a loading scene until assetbundles are downloaded from the CDN.
        /// Takes in an assetbundle URL, and a background image to display behind the loading bar.
        /// Replaces the current loading scene with a new one if it exists.
        /// </summary>
        public static void GenerateScene(string assetBundleUrl, Texture2D loadingScreenImage, string sceneFilePath)
        {
            // Removes the loading scene if it is present, otherwise does nothing.
            EditorSceneManager.CloseScene(SceneManager.GetSceneByName(Path.GetFileNameWithoutExtension(sceneFilePath)),
                true);

            var loadingScreenScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            PopulateScene(loadingScreenImage, assetBundleUrl);

            bool saveOk = EditorSceneManager.SaveScene(loadingScreenScene, sceneFilePath);

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
                SetMainSceneInBuild(sceneFilePath);
            }
        }

        // Visible for testing
        /// <summary>
        /// Adds the specified path to the build settings, if it isn't there, and marks it as included in the build.
        /// All other scenes are marked as excluded from the build.
        /// </summary>
        internal static void SetMainSceneInBuild(string pathToScene)
        {
            var buildScenes = EditorBuildSettings.scenes;
            var index = Array.FindIndex(buildScenes, (scene) => scene.path == pathToScene);

            //Disable all other scenes
            for (int i = 0; i < buildScenes.Length; i++)
            {
                buildScenes[i].enabled = i == index;
            }

            //If the scene isn't already in the list, add it and set it to enabled
            if (index < 0)
            {
                var appendedScenes = new EditorBuildSettingsScene[buildScenes.Length + 1];
                Array.Copy(buildScenes, appendedScenes, buildScenes.Length);
                appendedScenes[buildScenes.Length] = new EditorBuildSettingsScene(pathToScene, true);
                EditorBuildSettings.scenes = appendedScenes;
            }
            else
            {
                EditorBuildSettings.scenes = buildScenes;
            }
        }

        // Visible for testing
        internal static void PopulateScene(Texture backgroundTexture, string assetBundleUrl)
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

            CurrentLoadingScreen = loadingScreen;
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
            canvasScaler.matchWidthOrHeight = 0.5f;

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
    }
}