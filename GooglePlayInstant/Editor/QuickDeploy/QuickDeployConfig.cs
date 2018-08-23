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
using UnityEngine;

namespace GooglePlayInstant.Editor.QuickDeploy
{
    /// <summary>
    /// Contains a set of operations for storing and retrieving quick deploy configurations.
    /// </summary>
    public static class QuickDeployConfig
    {
        private static readonly string ConfigurationFilePath =
            Path.Combine("Library", "PlayInstantQuickDeployConfig.json");

        /// <summary>
        /// The Configuration singleton that should be used to read and modify Quick Deploy configuration.
        /// Modified values are persisted by calling SaveConfiguration.
        /// </summary>
        private static readonly Configuration _config = LoadConfiguration();

        public static string CloudCredentialsFileName = _config.cloudCredentialsFileName;
        public static string AssetBundleFileName = _config.assetBundleFileName;
        public static string CloudStorageBucketName = _config.cloudStorageBucketName;
        public static string CloudStorageObjectName = _config.cloudStorageObjectName;
        public static string AssetBundleUrl = _config.assetBundleUrl;
        public static string ApkFileName = _config.apkFileName;


        /// <summary>
        /// Store configuration from the current quick deploy tab to persistent storage.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if tab shouldn't have input fields.</exception>
        public static void SaveConfiguration(QuickDeployWindow.ToolBarSelectedButton currentTab)
        {
            switch (currentTab)
            {
                case QuickDeployWindow.ToolBarSelectedButton.DeployBundle:
                    _config.cloudCredentialsFileName = CloudCredentialsFileName;
                    _config.assetBundleFileName = AssetBundleFileName;
                    _config.cloudStorageBucketName = CloudStorageBucketName;
                    _config.cloudStorageObjectName = CloudStorageObjectName;
                    break;
                case QuickDeployWindow.ToolBarSelectedButton.LoadingScreen:
                    _config.assetBundleUrl = AssetBundleUrl;
                    break;
                case QuickDeployWindow.ToolBarSelectedButton.Build:
                    _config.apkFileName = ApkFileName;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("currentTab", currentTab, "Can't save from this tab.");
            }

            // Shouldn't hurt to write to persistent storage as long as SaveConfiguration(currentTab) is only called
            // when a major action happens.
            File.WriteAllText(ConfigurationFilePath, JsonUtility.ToJson(_config));
        }

        /// <summary>
        /// De-serialize configuration file contents into Configuration instance if the file exists exists, otherwise
        /// return Configuration instance with empty fields.
        /// </summary>
        private static Configuration LoadConfiguration()
        {
            if (!File.Exists(ConfigurationFilePath))
            {
                return new Configuration();
            }

            var configurationJson = File.ReadAllText(ConfigurationFilePath);
            return JsonUtility.FromJson<Configuration>(configurationJson);
        }

        /// <summary>
        /// Represents JSON contents of the quick deploy configuration file.
        /// </summary>
        [Serializable]
        private class Configuration
        {
            public string cloudCredentialsFileName;
            public string assetBundleFileName;
            public string cloudStorageBucketName;
            public string cloudStorageObjectName;
            public string assetBundleUrl;
            public string apkFileName;
        }
    }
}