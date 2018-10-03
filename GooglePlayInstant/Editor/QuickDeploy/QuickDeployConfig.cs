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
    /// Used to preserve user input data when quick deploy windows are reloaded or closed.
    /// </summary>
    public class QuickDeployConfig
    {
        internal static readonly string EditorConfigurationFilePath =
            Path.Combine("Library", "PlayInstantQuickDeployEditorConfig.json");

        /// <summary>
        /// The Editor Configuration singleton that should be used to read and modify Quick Deploy configuration.
        /// Modified values are persisted by calling SaveEditorConfiguration.
        /// </summary>
        private EditorConfiguration _editorConfig;

        // Copy of fields from EditorConfig for holding unsaved values set in the UI.
        public string AssetBundleUrl;
        public string AssetBundleFileName;
        public string LoadingSceneFileName;
        public Texture2D LoadingBackgroundImage;
        public PlayInstantSceneTreeView.State AssetBundleScenes;

        public void LoadConfiguration()
        {
            _editorConfig = LoadEditorConfiguration(EditorConfigurationFilePath);

            // Copy of fields from EditorConfig for holding unsaved values set in the UI.
            AssetBundleUrl = _editorConfig.assetBundleUrl;
            AssetBundleFileName = _editorConfig.assetBundleFileName;
            AssetBundleScenes = _editorConfig.assetBundleScenes;
            LoadingSceneFileName = _editorConfig.loadingSceneFileName;
            LoadingBackgroundImage = _editorConfig.loadingBackgroundImage;
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
                    SaveEditorConfiguration(currentTab, _editorConfig, EditorConfigurationFilePath);
                    break;
                case QuickDeployWindow.ToolBarSelectedButton.LoadingScreen:
                    SaveEditorConfiguration(currentTab, _editorConfig, EditorConfigurationFilePath);
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
                    configuration.assetBundleScenes = AssetBundleScenes;
                    break;
                case QuickDeployWindow.ToolBarSelectedButton.LoadingScreen:
                    configuration.assetBundleUrl = AssetBundleUrl;
                    configuration.loadingBackgroundImage = LoadingBackgroundImage;
                    configuration.loadingSceneFileName = LoadingSceneFileName;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("currentTab", currentTab,
                        "Can't save editor configurations from this tab.");
            }

            // Shouldn't hurt to write to persistent storage as long as SaveEditorConfiguration(currentTab) is only called
            // when a major action happens.
            File.WriteAllText(editorConfigurationPath, JsonUtility.ToJson(configuration));
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
        /// Represents JSON contents of the quick deploy configuration file.
        /// </summary>
        [Serializable]
        public class EditorConfiguration
        {
            public string assetBundleUrl;
            public string assetBundleFileName = Path.Combine("Assets", "MainBundle");
            public string loadingSceneFileName = Path.Combine("Assets", "PlayInstantLoadingScreen.unity");
            public Texture2D loadingBackgroundImage;
            public PlayInstantSceneTreeView.State assetBundleScenes;
        }
    }
}