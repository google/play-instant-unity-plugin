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

namespace GooglePlayInstant.LoadingScreen
{
    /// <summary>
    /// Class that represents the contents of the Loading Screen configuration json file, which notifies the loading scene
    /// what URL to download the quick deploy application's main scene AssetBundle from.
    /// </summary>
    public class LoadingScreenConfig
    {
        /// <summary>
        /// Name of the json file that contains Engine configurations
        /// </summary>
        public const string EngineConfigurationFileName = "PlayInstantQuickDeployEngineConfig.json";
        
        [Serializable]
        public class EngineConfiguration
        {
            /// <summary>
            /// URL of where the game's main AssetBundle is downloaded, and where the first scene is loaded from.
            /// </summary>
            public string assetBundleUrl;
        }
        
    }
}