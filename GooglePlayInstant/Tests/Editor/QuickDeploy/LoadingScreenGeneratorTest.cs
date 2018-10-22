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

using GooglePlayInstant.Editor.QuickDeploy;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using Object = UnityEngine.Object;

namespace GooglePlayInstant.Tests.Editor.QuickDeploy
{
    [TestFixture]
    public class LoadingScreenGeneratorTest
    {
        [Test]
        public void TestSetMainSceneInBuild()
        {
            var emptyScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            LoadingScreenGenerator.SetMainSceneInBuild(emptyScene.path);

            var scenesWithOurPath = Array.FindAll(EditorBuildSettings.scenes, (scene) => scene.path == emptyScene.path);
            Assert.AreEqual(scenesWithOurPath.Length, 1,
                "The scene should be present in Build Settings once and only once");

            Assert.IsTrue(scenesWithOurPath[0].enabled,
                "The scene should be enabled");

            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.path == emptyScene.path) continue;

                Assert.IsFalse(scene.enabled,
                    "All other scenes should be disabled");
            }
        }

        [Test]
        public void TestPopulateScene()
        {
            LoadingScreenGenerator.PopulateScene(null, "https://www.validAssetBundleUrl.com");
            Assert.IsNotNull(Object.FindObjectOfType<LoadingScreen.LoadingScreen>(),
                "A LoadingScreen component should be present in the populated scene");
        }

        [Test]
        public void TestFindReplayButtonSprite()
        {
            Assert.IsNotNull(LoadingScreenGenerator.FindReplayButtonSprite());
        }
    }
}