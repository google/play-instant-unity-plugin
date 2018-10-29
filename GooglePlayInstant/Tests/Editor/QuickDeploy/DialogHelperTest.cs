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
using NUnit.Framework;
using UnityEngine;

namespace GooglePlayInstant.Tests.Editor.QuickDeploy
{
    [TestFixture]
    public class DialogHelperTest
    {
        [Test]
        public void TestAbsoluteToAssetsRelativePath()
        {
            var sceneRelativePath = "Scenes/LoadingScene.unity";
            var scenePath = Path.Combine(Application.dataPath, sceneRelativePath);
            Assert.AreEqual("Assets/Scenes/LoadingScene.unity", DialogHelper.AbsoluteToAssetsRelativePath(scenePath));
        }

        [Test]
        public void TestAbsoluteToRelativePath_ValidPaths()
        {
            var assetsPath = "C:/Documents/Project/Assets";
            var scenePath = "C:/Documents/Project/Assets/Scenes/LoadingScene.unity";
            Assert.AreEqual("Assets/Scenes/LoadingScene.unity",
                DialogHelper.AbsoluteToRelativePath(scenePath, assetsPath));
        }

        [Test]
        public void TestAbsoluteToRelativePath_EqualPaths()
        {
            var assetsPath = "C:/Documents/Project/Assets/";
            Assert.AreEqual("Assets/", DialogHelper.AbsoluteToRelativePath(assetsPath, assetsPath));
        }

        [Test]
        public void TestAbsoluteToRelativePath_RedundantPaths()
        {
            var assetsPath = "C:/Documents/Project/Assets";
            var scenePath = "C:/Documents/Project/Assets/C:/Documents/Project/Assets/LoadingScene.unity";
            Assert.AreEqual("Assets/C:/Documents/Project/Assets/LoadingScene.unity",
                DialogHelper.AbsoluteToRelativePath(scenePath, assetsPath));
        }

        [Test]
        public void TestAbsoluteToRelativePath_SeparatePaths()
        {
            var assetsPath = "C:/Documents/Project/Assets/";
            var scenePath = "C:/SomeOtherFolder/Scenes/LoadingScene.unity";
            Assert.IsNull(DialogHelper.AbsoluteToRelativePath(scenePath, assetsPath));
        }

        [Test]
        public void TestAbsoluteToRelativePath_EmptyPaths()
        {
            Assert.IsNull(DialogHelper.AbsoluteToRelativePath("", ""));
        }
    }
}