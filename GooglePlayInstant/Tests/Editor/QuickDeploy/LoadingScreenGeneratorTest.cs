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

using System.IO;
using GooglePlayInstant.Editor.QuickDeploy;
using GooglePlayInstant.LoadingScreen;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GooglePlayInstant.Tests.Editor.QuickDeploy
{
    /// <summary>
    /// Contains unit tests for LoadingScreenGenerator methods.
    /// </summary>
    [TestFixture]
    public class LoadingScreenGeneratorTest
    {
        private const string TestGameObjectName = "Testing Object";

        private static readonly string TestLoadingScreenJsonPath =
            Path.Combine("Assets", LoadingScreenGenerator.LoadingScreenJsonFileName);

        // Dispose of temporarily created file.  
        [TearDown]
        public void Cleanup()
        {
            AssetDatabase.DeleteAsset(TestLoadingScreenJsonPath);
        }

        [Test]
        public void TestAddLoadingScreenScript()
        {
            var loadingScreenGameObject = new GameObject(TestGameObjectName);
            LoadingScreenGenerator.AddLoadingScreenScript(loadingScreenGameObject);
            Assert.IsNotNull(loadingScreenGameObject.GetComponent<LoadingScreenScript>(),
                "A script should be attached to the loading screen object.");
        }

        [Test]
        public void TestAddLoadingScreenImage()
        {
            const string testImage = "example.png";

            // Create an empty test file by immediately closing the FileStream returned by File.Create().
            using (File.Create(testImage)) ;

            var loadingScreenGameObject = new GameObject(TestGameObjectName);

            LoadingScreenGenerator.AddLoadingScreenImageToScene(loadingScreenGameObject, testImage);

            Assert.IsNotNull(loadingScreenGameObject.GetComponent<Canvas>(),
                "A canvas component should have been added to the loading screen game object.");
            Assert.IsNotNull(loadingScreenGameObject.GetComponent<Image>(),
                "An image component should have been added to the loading screen game object.");
        }

        [Test]
        public void TestGenerateLoadingScreenConfigFileWithString()
        {
            const string testUrl = "test.co";

            LoadingScreenGenerator.GenerateLoadingScreenConfigFile(testUrl, TestLoadingScreenJsonPath);

            var loadingScreenConfigJson =
                AssetDatabase.LoadAssetAtPath(TestLoadingScreenJsonPath, typeof(TextAsset)).ToString();

            var loadingScreenConfig = JsonUtility.FromJson<LoadingScreenConfig>(loadingScreenConfigJson);

            Assert.AreEqual(testUrl, loadingScreenConfig.assetBundleUrl);
        }

        [Test]
        public void TestGenerateLoadingScreenConfigFileWithEmptyString()
        {
            LoadingScreenGenerator.GenerateLoadingScreenConfigFile("", TestLoadingScreenJsonPath);

            var loadingScreenConfigJson =
                AssetDatabase.LoadAssetAtPath(TestLoadingScreenJsonPath, typeof(TextAsset)).ToString();

            var loadingScreenConfig = JsonUtility.FromJson<LoadingScreenConfig>(loadingScreenConfigJson);

            Assert.IsEmpty(loadingScreenConfig.assetBundleUrl);
        }
    }
}