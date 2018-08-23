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
        public const string LoadingSceneName = "play-instant-loading-screen-scene";

        private const string LoadingScreenCanvasName = "Loading Screen Canvas";
        
        private const string LoadingScreenSaveErrorTitle = "Loading Screen Save Error";

        private static readonly string LoadingScreenScenePath =
            Path.Combine("Assets", "PlayInstantLoadingScreen");

        private static readonly string LoadingScreenResourcesPath = Path.Combine(LoadingScreenScenePath, "Resources");

        private static readonly string LoadingScreenJsonPath =
            Path.Combine(LoadingScreenResourcesPath, LoadingScreenJsonFileName);

        // Visible for testing
        internal const string LoadingScreenJsonFileName = "LoadingScreenConfig.json";
        

        /// <summary>
        /// Creates a scene in the current project that acts as a loading scene until assetbundles are
        /// downloaded from the CDN. Takes in a loadingScreenImagePath, a path to the image shown in the loading scene,
        /// and an assetbundle URL. Replaces the current loading scene with a new one if it exists.
        /// </summary>
        public static void GenerateLoadingScreenScene(string assetBundleUrl, string loadingScreenImagePath)
        {
            if (string.IsNullOrEmpty(assetBundleUrl))
            {
                throw new ArgumentException("AssetBundle URL text field cannot be null or empty.");
            }

            if (!File.Exists(loadingScreenImagePath))
            {
                throw new FileNotFoundException(string.Format("Loading screen image file cannot be found: {0}",
                    loadingScreenImagePath));
            }

            // Removes the loading scene if it is present, otherwise does nothing.
            EditorSceneManager.CloseScene(SceneManager.GetSceneByName(LoadingSceneName), true);

            var loadingScreenScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            Directory.CreateDirectory(LoadingScreenResourcesPath);

            GenerateLoadingScreenConfigFile(assetBundleUrl, LoadingScreenJsonPath);

            var loadingScreenGameObject = new GameObject(LoadingScreenCanvasName);

            AddLoadingScreenImageToScene(loadingScreenGameObject, loadingScreenImagePath);

            AddLoadingScreenScript(loadingScreenGameObject);

            LoadingBar.AddLoadingScreenBarComponent(loadingScreenGameObject);

            bool saveOk = EditorSceneManager.SaveScene(loadingScreenScene,
                Path.Combine(LoadingScreenScenePath, LoadingSceneName + ".unity"));

            if (!saveOk)
            {
                // Not a fatal issue. User can attempt to resave this scene.
                var warningMessage = string.Format("Issue while saving scene {0}.",
                    LoadingSceneName);
                
                Debug.LogWarning(warningMessage);
                
                DialogHelper.DisplayMessage(LoadingScreenSaveErrorTitle, warningMessage);
            }
        }

        // Visible for testing
        internal static void AddLoadingScreenScript(GameObject loadingScreenGameObject)
        {
            loadingScreenGameObject.AddComponent<LoadingScreenScript>();
        }


        // Visible for testing
        internal static void AddLoadingScreenImageToScene(GameObject loadingScreenGameObject,
            string pathToLoadingScreenImage)
        {
            loadingScreenGameObject.AddComponent<Canvas>();
            var loadingScreenCanvas = loadingScreenGameObject.GetComponent<Canvas>();
            loadingScreenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var loadingScreenImageData = File.ReadAllBytes(pathToLoadingScreenImage);

            var tex = new Texture2D(1, 1);

            var texLoaded = tex.LoadImage(loadingScreenImageData);

            if (!texLoaded)
            {
                throw new Exception("Failed to load image as a Texture2D.");
            }

            var loadingImageSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));

            loadingScreenGameObject.AddComponent<Image>();
            var loadingScreenImage = loadingScreenGameObject.GetComponent<Image>();
            loadingScreenImage.sprite = loadingImageSprite;
        }

        // Visible for testing
        internal static void GenerateLoadingScreenConfigFile(string assetBundleUrl, string targetLoadingScreenJsonPath)
        {
            var loadingScreenConfig =
                new LoadingScreenConfig {assetBundleUrl = assetBundleUrl};

            var loadingScreenConfigJson = EditorJsonUtility.ToJson(loadingScreenConfig);

            File.WriteAllText(targetLoadingScreenJsonPath, loadingScreenConfigJson);

            // Force asset to import synchronously so that testing can be completed immediately after generating a loading screen.
            AssetDatabase.ImportAsset(targetLoadingScreenJsonPath, ImportAssetOptions.ForceSynchronousImport);
        }
    }
}