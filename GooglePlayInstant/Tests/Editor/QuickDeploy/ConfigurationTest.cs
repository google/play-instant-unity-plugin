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

namespace GooglePlayInstant.Tests.Editor.QuickDeploy
{
    public class ConfigurationTest
    {
        private static readonly string TestConfigurationPath =
            Path.Combine("Assets", LoadingScreenConfig.EngineConfigurationFileName);

        // Dispose of temporarily created file.  
        [TearDown]
        public void Cleanup()
        {
            AssetDatabase.DeleteAsset(TestConfigurationPath);
        }

        [Test]
        public void TestSavingConfigOnCreateBundleWithString()
        {
            var quickDeployConfig = new QuickDeployConfig
            {
                AssetBundleFileName = "testbundle"
            };
            var inputConfig = new QuickDeployConfig.EditorConfiguration();

            quickDeployConfig.SaveEditorConfiguration(QuickDeployWindow.ToolBarSelectedButton.CreateBundle, inputConfig,
                TestConfigurationPath);

            var outputConfigurationJson = File.ReadAllText(TestConfigurationPath);
            var outputConfig =
                JsonUtility.FromJson<QuickDeployConfig.EditorConfiguration>(outputConfigurationJson);

            Assert.AreEqual(outputConfig.assetBundleFileName, quickDeployConfig.AssetBundleFileName);
        }

        [Test]
        public void TestSavingConfigOnCreateBundleWithEmptyString()
        {
            var quickDeployConfig = new QuickDeployConfig
            {
                AssetBundleFileName = ""
            };

            var inputConfig = new QuickDeployConfig.EditorConfiguration();

            quickDeployConfig.SaveEditorConfiguration(QuickDeployWindow.ToolBarSelectedButton.CreateBundle, inputConfig,
                TestConfigurationPath);

            var outputConfigurationJson = File.ReadAllText(TestConfigurationPath);
            var outputConfig =
                JsonUtility.FromJson<QuickDeployConfig.EditorConfiguration>(outputConfigurationJson);

            Assert.IsEmpty(outputConfig.assetBundleFileName);
        }

        [Test]
        public void TestSavingConfigOnLoadingScreenWithString()
        {
            var quickDeployConfig = new QuickDeployConfig
            {
                AssetBundleUrl = "testurl"
            };

            var inputConfig = new LoadingScreenConfig.EngineConfiguration();

            quickDeployConfig.SaveEngineConfiguration(QuickDeployWindow.ToolBarSelectedButton.LoadingScreen,
                inputConfig,
                TestConfigurationPath);

            var outputConfigurationJson = File.ReadAllText(TestConfigurationPath);
            var outputConfig =
                JsonUtility.FromJson<LoadingScreenConfig.EngineConfiguration>(outputConfigurationJson);

            Assert.AreEqual(outputConfig.assetBundleUrl, quickDeployConfig.AssetBundleUrl);
        }

        [Test]
        public void TestSavingConfigOnLoadingScreenWithEmptyString()
        {
            var quickDeployConfig = new QuickDeployConfig
            {
                AssetBundleUrl = ""
            };

            var inputConfig = new LoadingScreenConfig.EngineConfiguration();

            quickDeployConfig.SaveEngineConfiguration(QuickDeployWindow.ToolBarSelectedButton.LoadingScreen,
                inputConfig,
                TestConfigurationPath);

            var outputConfigurationJson = File.ReadAllText(TestConfigurationPath);
            var outputConfig =
                JsonUtility.FromJson<LoadingScreenConfig.EngineConfiguration>(outputConfigurationJson);

            Assert.IsEmpty(outputConfig.assetBundleUrl);
        }

        [Test]
        public void TestLoadingEditorConfiguration()
        {
            var quickDeployConfig = new QuickDeployConfig();

            var inputConfig = new QuickDeployConfig.EditorConfiguration
            {
                assetBundleFileName = "testbundle"
            };

            File.WriteAllText(TestConfigurationPath, JsonUtility.ToJson(inputConfig));

            var outputConfig = quickDeployConfig.LoadEditorConfiguration(TestConfigurationPath);

            Assert.AreEqual(inputConfig.assetBundleFileName, outputConfig.assetBundleFileName);
        }

        [Test]
        public void TestLoadingEngineConfiguration()
        {
            var quickDeployConfig = new QuickDeployConfig();

            var inputConfig = new LoadingScreenConfig.EngineConfiguration {assetBundleUrl = "testurl"};

            File.WriteAllText(TestConfigurationPath, JsonUtility.ToJson(inputConfig));

            var outputConfig = quickDeployConfig.LoadEngineConfiguration(TestConfigurationPath);

            Assert.AreEqual(inputConfig.assetBundleUrl, outputConfig.assetBundleUrl);
        }
    }
}