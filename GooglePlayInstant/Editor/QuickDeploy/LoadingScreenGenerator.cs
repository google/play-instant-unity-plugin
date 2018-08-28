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
        public const string SceneName = "play-instant-loading-screen-scene.unity";

        private const string CanvasName = "Loading Screen Canvas";

        private const string SaveErrorTitle = "Loading Screen Save Error";

        private static readonly string SceneDirectoryPath =
            Path.Combine("Assets", "PlayInstantLoadingScreen");

        private static readonly string SceneFilePath =
            Path.Combine(SceneDirectoryPath, SceneName);

        private static readonly string ResourcesDirectoryPath = Path.Combine(SceneDirectoryPath, "Resources");

        private static readonly string JsonFilePath =
            Path.Combine(ResourcesDirectoryPath, JsonFileName);

        // Visible for testing
        internal const string JsonFileName = "LoadingScreenConfig.json";


        /// <summary>
        /// Creates a scene in the current project that acts as a loading scene until assetbundles are
        /// downloaded from the CDN. Takes in a loadingScreenImagePath, a path to the image shown in the loading scene,
        /// and an assetbundle URL. Replaces the current loading scene with a new one if it exists.
        /// </summary>
        public static void GenerateScene(string assetBundleUrl, string loadingScreenImagePath)
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
            EditorSceneManager.CloseScene(SceneManager.GetSceneByName(Path.GetFileNameWithoutExtension(SceneName)), true);

            var loadingScreenScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            Directory.CreateDirectory(ResourcesDirectoryPath);

            GenerateConfigFile(assetBundleUrl, JsonFilePath);

            var loadingScreenGameObject = new GameObject(CanvasName);

            AddImageToScene(loadingScreenGameObject, loadingScreenImagePath);

            AddScript(loadingScreenGameObject);

            LoadingBar.AddComponent(loadingScreenGameObject);

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
                //TODO: investigate GUI Layout errors that occur when moving this to DialogHelper
                if (EditorUtility.DisplayDialog("Change Scenes in Build",
                    "Would you like to replace any existing Scenes in Build with the loading screen scene?", "Yes", "No"))
                {
                    SetMainSceneInBuild(SceneFilePath);
                }

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

        // Visible for testing
        internal static void AddScript(GameObject loadingScreenGameObject)
        {
            loadingScreenGameObject.AddComponent<LoadingScreenScript>();
        }


        // Visible for testing
        internal static void AddImageToScene(GameObject loadingScreenGameObject,
            string pathToLoadingScreenImage)
        {
            if (loadingScreenGameObject.GetComponent<Canvas>() == null)
            {
                // First time creating a loading screen, configure nested game objects appropriately.
                var loadingScreenCanvas = loadingScreenGameObject.AddComponent<Canvas>();
                
                loadingScreenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                
                loadingScreenGameObject.AddComponent<Image>();
            }

            var loadingScreenImageData = File.ReadAllBytes(pathToLoadingScreenImage);

            var tex = new Texture2D(1, 1);

            var texLoaded = tex.LoadImage(loadingScreenImageData);

            if (!texLoaded)
            {
                throw new Exception("Failed to load image as a Texture2D.");
            }

            var loadingImageSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));

            var loadingScreenImage = loadingScreenGameObject.GetComponent<Image>();
            loadingScreenImage.sprite = loadingImageSprite;
        }

        // Visible for testing
        internal static void GenerateConfigFile(string assetBundleUrl, string targetLoadingScreenJsonPath)
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