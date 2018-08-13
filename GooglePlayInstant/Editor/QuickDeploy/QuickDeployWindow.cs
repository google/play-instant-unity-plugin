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

using UnityEditor;
using UnityEngine;

namespace GooglePlayInstant.Editor.QuickDeploy
{
    public class QuickDeployWindow : EditorWindow
    {
        private static readonly string[] ToolbarButtonNames =
        {
            "Create Bundle", "Deploy Bundle", "Verify Bundle",
            "Loading Screen", "Build"
        };

        private static int _toolbarSelectedButtonIndex;

        // Keep track of the previous tab to remove focus if user moves to a different tab. (b/112536394)
        private static ToolBarSelectedButton _previousTab;

        public enum ToolBarSelectedButton
        {
            CreateBundle,
            DeployBundle,
            VerifyBundle,
            LoadingScreen,
            Build
        }

        private const int FieldMinWidth = 100;
        private const int ButtonWidth = 200;
        private const int LongButtonWidth = 300;
        private const int ShortButtonWidth = 100;

        private const string LoadingScreenErrorTitle = "Creating Loading Scene Error";
        private const string OkButtonText = "OK";

        // Local copy of the fields from QuickDeployConfig.Config to track unsaved changes.
        private string _assetBundleFileName;
        private string _cloudStorageBucketName;
        private string _cloudStorageFileName;
        private string _cloudCredentialsFileName;
        private string _assetBundleUrl;
        private string _apkFileName;

        public static void ShowWindow(ToolBarSelectedButton select)
        {
            GetWindow<QuickDeployWindow>("Quick Deploy");
            _toolbarSelectedButtonIndex = (int) select;
        }

        void Awake()
        {
            _assetBundleFileName = QuickDeployConfig.Config.assetBundleFileName;
            _cloudStorageBucketName = QuickDeployConfig.Config.cloudStorageBucketName;
            _cloudStorageFileName = QuickDeployConfig.Config.cloudStorageFileName;
            _cloudCredentialsFileName = QuickDeployConfig.Config.cloudCredentialsFileName;
            _assetBundleUrl = QuickDeployConfig.Config.assetBundleUrl;
            _apkFileName = QuickDeployConfig.Config.apkFileName;
        }

        void OnGUI()
        {
            _toolbarSelectedButtonIndex = GUILayout.Toolbar(_toolbarSelectedButtonIndex, ToolbarButtonNames);
            var currentTab = (ToolBarSelectedButton) _toolbarSelectedButtonIndex;
            UpdateGUIFocus(currentTab);
            switch (currentTab)
            {
                case ToolBarSelectedButton.CreateBundle:
                    AssetBundleBrowserClient.ReloadAndUpdateBrowserInfo();
                    OnGuiCreateBundleSelect();
                    break;
                case ToolBarSelectedButton.DeployBundle:
                    OnGuiDeployBundleSelect();
                    break;
                case ToolBarSelectedButton.VerifyBundle:
                    OnGuiVerifyBundleSelect();
                    break;
                case ToolBarSelectedButton.LoadingScreen:
                    OnGuiLoadingScreenSelect();
                    break;
                case ToolBarSelectedButton.Build:
                    OnGuiCreateBuildSelect();
                    break;
            }

            // Disable the Save button unless one of the fields has changed.
            GUI.enabled = IsAnyFieldChanged();

            if (GUILayout.Button("Save"))
            {
                QuickDeployConfig.SaveConfiguration(_assetBundleFileName, _cloudStorageBucketName,
                    _cloudStorageFileName, _cloudCredentialsFileName, _assetBundleUrl, _apkFileName);
            }

            GUI.enabled = true;
        }


        /// <summary>
        /// Unfocus the window if the user has just moved to a different quick deploy tab.
        /// </summary>
        /// <param name="currentTab">A ToolBarSelectedButton instance representing the current quick deploy tab.</param>
        /// <see cref="b/112536394"/>
        private static void UpdateGUIFocus(ToolBarSelectedButton currentTab)
        {
            if (currentTab != _previousTab)
            {
                _previousTab = currentTab;
                GUI.FocusControl(null);
            }
        }

        private void OnGuiCreateBundleSelect()
        {
            EditorGUILayout.LabelField("AssetBundle Creation", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Use the Unity Asset Bundle Browser to select your game's main scene " +
                                       "and bundle it (and its dependencies) into an AssetBundle file.",
                EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                string.Format("Asset Bundle Browser version: {0}",
                    AssetBundleBrowserClient.GetAssetBundleBrowserVersion()),
                EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();

            // Allow the developer to open the AssetBundles Browser if it is present, otherwise ask them to download it
            if (AssetBundleBrowserClient.AssetBundleBrowserIsPresent())
            {
                if (GUILayout.Button("Open Asset Bundle Browser", GUILayout.Width(ButtonWidth)))
                {
                    AssetBundleBrowserClient.DisplayAssetBundleBrowser();
                }
            }
            else
            {
                if (GUILayout.Button("Download Asset Bundle Browser from GitHub", GUILayout.Width(LongButtonWidth)))
                {
                    Application.OpenURL("https://github.com/Unity-Technologies/AssetBundles-Browser/releases");
                }

                EditorGUILayout.Space();
                if (GUILayout.Button("Open Asset Bundle Browser Documentation", GUILayout.Width(LongButtonWidth)))
                {
                    Application.OpenURL("https://docs.unity3d.com/Manual/AssetBundles-Browser.html");
                }

                EditorGUILayout.Space();
            }
        }

        private void OnGuiDeployBundleSelect()
        {
            EditorGUILayout.LabelField("AssetBundle Deployment", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Use the Google Cloud Storage to host the AssetBundle as a public " +
                                       "file. Or host the file on your own CDN.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();
            // TODO: Allow the user to browse to the asset bundle file without having to always manually enter the path 
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Asset Bundle File Path Name", GUILayout.MinWidth(FieldMinWidth));
            _assetBundleFileName = EditorGUILayout.TextField(_assetBundleFileName, GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Cloud Storage Bucket Name", GUILayout.MinWidth(FieldMinWidth));
            _cloudStorageBucketName =
                EditorGUILayout.TextField(_cloudStorageBucketName, GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Cloud Storage File Name", GUILayout.MinWidth(FieldMinWidth));
            _cloudStorageFileName = EditorGUILayout.TextField(_cloudStorageFileName, GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            // TODO: Allow the user to browse to credentials file without having to always manually enter the path
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Path to Google Cloud OAuth2 Credentials", GUILayout.MinWidth(FieldMinWidth));
            _cloudCredentialsFileName =
                EditorGUILayout.TextField(_cloudCredentialsFileName, GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();
            GUILayout.Button("Upload to Google Cloud Storage", GUILayout.Width(LongButtonWidth));
            EditorGUILayout.Space();
            GUILayout.Button("Open Google Cloud Storage Console", GUILayout.Width(LongButtonWidth));
        }

        private void OnGuiVerifyBundleSelect()
        {
            EditorGUILayout.LabelField("AssetBundle Verification", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Verifies that the file at the specified URL is available and reports " +
                                       "metadata including file version and compression type.",
                EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("AssetBundle URL", GUILayout.MinWidth(FieldMinWidth));
            _assetBundleUrl = EditorGUILayout.TextField(_assetBundleUrl, GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical();

            if (GUILayout.Button("Verify AssetBundle", GUILayout.Width(ButtonWidth)))
            {
                if (string.IsNullOrEmpty(QuickDeployConfig.Config.assetBundleUrl))
                {
                    Debug.LogError("AssetBundle URL text field cannot be empty.");
                }
                else
                {
                    AssetBundleVerifierWindow.ShowWindow(_assetBundleUrl);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void OnGuiLoadingScreenSelect()
        {
            EditorGUILayout.LabelField("Loading Screen", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("A loading screen scene displays a progress bar over the image " +
                                       "specified below while downloading and opening the main scene.",
                EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            if (GUILayout.Button("Choose Loading Image", GUILayout.Width(ButtonWidth)))
            {
                LoadingScreenGenerator.LoadingScreenImagePath =
                    EditorUtility.OpenFilePanel("Select Image", "", "png,jpg,jpeg,tif,tiff,gif,bmp");
            }

            EditorGUILayout.Space();

            var displayedPath = LoadingScreenGenerator.LoadingScreenImagePath ?? "no file specified";
            EditorGUILayout.LabelField(string.Format("Image file: {0}", displayedPath),
                GUILayout.MinWidth(FieldMinWidth));

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            if (GUILayout.Button("Create Loading Scene", GUILayout.Width(ButtonWidth)))
            {
                if (string.IsNullOrEmpty(_assetBundleUrl))
                {
                    LogError("AssetBundle URL text field cannot be null or empty.");
                }
                else
                {
                    LoadingScreenGenerator.GenerateLoadingScreenScene(_assetBundleUrl);
                }
            }
        }

        private static void LogError(string message)
        {
            Debug.LogErrorFormat("Build error: {0}", message);
            EditorUtility.DisplayDialog(LoadingScreenErrorTitle, message, OkButtonText);
        }

        private void OnGuiCreateBuildSelect()
        {
            EditorGUILayout.LabelField("Deployment", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Build the APK using the IL2CPP engine.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("APK File Name", GUILayout.MinWidth(FieldMinWidth));
            _apkFileName = EditorGUILayout.TextField(_apkFileName, GUILayout.MinWidth(FieldMinWidth));
            if (GUILayout.Button("Browse", GUILayout.Width(ShortButtonWidth)))
            {
                _apkFileName = EditorUtility.SaveFilePanel("Choose file name and location", "", "base.apk", "apk");
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            if (GUILayout.Button("Build Base APK", GUILayout.Width(ButtonWidth)))
            {
                QuickDeployApkBuilder.BuildQuickDeployInstantGameApk();
            }
        }

        private bool IsAnyFieldChanged()
        {
            return _assetBundleFileName != QuickDeployConfig.Config.assetBundleFileName ||
                   _cloudStorageBucketName != QuickDeployConfig.Config.cloudStorageBucketName ||
                   _cloudStorageFileName != QuickDeployConfig.Config.cloudStorageFileName ||
                   _cloudCredentialsFileName != QuickDeployConfig.Config.cloudCredentialsFileName ||
                   _assetBundleUrl != QuickDeployConfig.Config.assetBundleUrl ||
                   _apkFileName != QuickDeployConfig.Config.apkFileName;
        }
    }
}