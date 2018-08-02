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
    /// Contains set of operations for storing and retrieving quick deploy configurations.
    /// </summary>
    public static class QuickDeployConfig
    {
        private static readonly string ConfigurationFilePath =
            Path.Combine("Library", "PlayInstantQuickDeployConfig.json");

        /// <summary>
        /// The Configuration singleton that should be used to read and modify Quick Deploy configuration.
        /// Modified values are persisted by calling SaveConfiguration.
        /// </summary>
        public static readonly Configuration Config = LoadConfiguration();

        // TODO: call this method
        /// <summary>
        /// Commit the current state of quick deploy configurations to persistent storage.
        /// </summary>
        public static void SaveConfiguration()
        {
            File.WriteAllText(ConfigurationFilePath, JsonUtility.ToJson(Config));
        }

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
        /// Represents the contents of the quick deploy configuration file.
        /// </summary>
        [Serializable]
        public class Configuration
        {
            public string assetBundleFileName;
            public string cloudStorageBucketName;
            public string cloudStorageFileName;
            public string cloudCredentialsFileName;
            public string assetBundleUrl;
            public string apkFileName;
        }
    }
}