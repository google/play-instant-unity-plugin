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
using GooglePlayInstant.LoadingScreen;
using UnityEngine;

namespace GooglePlayInstant.Editor.QuickDeploy
{
    /// <summary>
    /// Contains a set of operations for storing and retrieving quick deploy configurations.
    /// </summary>
    public class QuickDeployConfig
    {
        private static readonly string EditorConfigurationFilePath =
            Path.Combine("Library", "PlayInstantQuickDeployEditorConfig.json");

        private static readonly string ResourcesDirectoryPath =
            Path.Combine(LoadingScreenGenerator.SceneDirectoryPath, "Resources");

        private static readonly string EngineConfigurationFilePath =
            Path.Combine(ResourcesDirectoryPath, LoadingScreenConfig.EngineConfigurationFileName);

        /// <summary>
        /// The Editor Configuration singleton that should be used to read and modify Quick Deploy configuration.
        /// Modified values are persisted by calling SaveEditorConfiguration.
        /// </summary>
        private EditorConfiguration EditorConfig;

        /// <summary>
        /// The Engine Configuration singleton that should be used to read and modify Loading Screen configuration.
        /// Modified values are persisted by calling SaveEngineConfiguration.
        /// </summary>
        private LoadingScreenConfig.EngineConfiguration EngineConfig;

        // Copy of fields from EditorConfig and EngineConfig for holding unsaved values set in the UI.
        public string CloudCredentialsFileName;
        public string AssetBundleFileName;
        public string CloudStorageBucketName;
        public string CloudStorageObjectName;
        public string AssetBundleUrl;

        public void LoadConfiguration()
        {
            EditorConfig = LoadEditorConfiguration(EditorConfigurationFilePath);
            EngineConfig = LoadEngineConfiguration(EngineConfigurationFilePath);
            
            // Copy of fields from EditorConfig and EngineConfig for holding unsaved values set in the UI.
            CloudCredentialsFileName = EditorConfig.cloudCredentialsFileName;
            AssetBundleFileName = EditorConfig.assetBundleFileName;
            CloudStorageBucketName = EditorConfig.cloudStorageBucketName;
            CloudStorageObjectName = EditorConfig.cloudStorageObjectName;
            AssetBundleUrl = EngineConfig.assetBundleUrl;
        }

        /// <summary>
        /// Store configuration from the current quick deploy tab to persistent storage.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if tab shouldn't have input fields.</exception>
        public void SaveConfiguration(QuickDeployWindow.ToolBarSelectedButton currentTab)
        {
            switch (currentTab)
            {
                case QuickDeployWindow.ToolBarSelectedButton.CreateBundle:
                    SaveEditorConfiguration(QuickDeployWindow.ToolBarSelectedButton.CreateBundle, EditorConfig,
                        EditorConfigurationFilePath);
                    break;
                case QuickDeployWindow.ToolBarSelectedButton.DeployBundle:
                    SaveEditorConfiguration(QuickDeployWindow.ToolBarSelectedButton.DeployBundle, EditorConfig,
                        EditorConfigurationFilePath);
                    break;
                case QuickDeployWindow.ToolBarSelectedButton.LoadingScreen:
                    SaveEngineConfiguration(QuickDeployWindow.ToolBarSelectedButton.LoadingScreen, EngineConfig,
                        EngineConfigurationFilePath);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("currentTab", currentTab, "Can't save from this tab.");
            }
        }

        // Visible for testing
        internal void SaveEditorConfiguration(QuickDeployWindow.ToolBarSelectedButton currentTab,
            EditorConfiguration configuration, string editorConfigurationPath)
        {
            switch (currentTab)
            {
                case QuickDeployWindow.ToolBarSelectedButton.CreateBundle:
                    configuration.assetBundleFileName = AssetBundleFileName;
                    break;
                case QuickDeployWindow.ToolBarSelectedButton.DeployBundle:
                    configuration.cloudCredentialsFileName = CloudCredentialsFileName;
                    configuration.assetBundleFileName = AssetBundleFileName;
                    configuration.cloudStorageBucketName = CloudStorageBucketName;
                    configuration.cloudStorageObjectName = CloudStorageObjectName;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("currentTab", currentTab,
                        "Can't save editor configurations from this tab.");
            }

            // Shouldn't hurt to write to persistent storage as long as SaveEditorConfiguration(currentTab) is only called
            // when a major action happens.
            File.WriteAllText(editorConfigurationPath, JsonUtility.ToJson(configuration));
        }

        // Visible for testing
        internal void SaveEngineConfiguration(QuickDeployWindow.ToolBarSelectedButton currentTab,
            LoadingScreenConfig.EngineConfiguration configuration, string engineConfigurationPath)
        {
            switch (currentTab)
            {
                case QuickDeployWindow.ToolBarSelectedButton.LoadingScreen:
                    configuration.assetBundleUrl = AssetBundleUrl;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("currentTab", currentTab,
                        "Can't save engine configurations from this tab.");
            }

            Directory.CreateDirectory(ResourcesDirectoryPath);

            // Shouldn't hurt to write to persistent storage as long as SaveEngineConfiguration(currentTab) is only called
            // when a major action happens.
            File.WriteAllText(engineConfigurationPath, JsonUtility.ToJson(configuration));
        }

        /// <summary>
        /// De-serialize editor configuration file contents into EditorConfiguration instance if the file exists exists, otherwise
        /// return Configuration instance with empty fields.
        /// </summary>
        internal EditorConfiguration LoadEditorConfiguration(string editorConfigurationPath)
        {
            if (!File.Exists(editorConfigurationPath))
            {
                return new EditorConfiguration();
            }

            var configurationJson = File.ReadAllText(editorConfigurationPath);
            return JsonUtility.FromJson<EditorConfiguration>(configurationJson);
        }

        /// <summary>
        /// De-serialize engine configuration file contents into EngineConfiguration instance if the file exists exists, otherwise
        /// return Configuration instance with empty fields.
        /// </summary>
        internal LoadingScreenConfig.EngineConfiguration LoadEngineConfiguration(string engineConfigurationPath)
        {
            if (!File.Exists(engineConfigurationPath))
            {
                return new LoadingScreenConfig.EngineConfiguration();
            }

            var configurationJson = File.ReadAllText(engineConfigurationPath);
            return JsonUtility.FromJson<LoadingScreenConfig.EngineConfiguration>(configurationJson);
        }

        /// <summary>
        /// Returns true if an instance of the engine configuration exists.
        /// </summary>
        public static bool EngineConfigExists()
        {
            return File.Exists(EngineConfigurationFilePath);
        }

        /// <summary>
        /// Represents JSON contents of the quick deploy configuration file.
        /// </summary>
        [Serializable]
        public class EditorConfiguration
        {
            public string cloudCredentialsFileName;
            public string assetBundleFileName;
            public string cloudStorageBucketName;
            public string cloudStorageObjectName;
        }
    }
}