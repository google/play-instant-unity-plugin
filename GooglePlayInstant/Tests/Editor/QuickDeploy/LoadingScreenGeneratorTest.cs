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
using UnityEditor.SceneManagement;
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
        [Test]
        public void TestSetMainSceneInBuild()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            LoadingScreenGenerator.SetMainSceneInBuild(scene.path);

            Assert.AreEqual(EditorBuildSettings.scenes.Length, 1,
                "There should be only one scene in Build Settings.");

            Assert.AreEqual(EditorBuildSettings.scenes[0].path, scene.path,
                "The new scene built should be identical to the one in Build Settings.");
        }

        [Test]
        public void TestGenerateScene()
        {
            LoadingScreenGenerator.GenerateScene("", null, "");
            Assert.IsNotNull(Object.FindObjectOfType<LoadingScreen.LoadingScreen>(),
                "A LoadingScreen component should be present in the generated scene");
        }
    }
}