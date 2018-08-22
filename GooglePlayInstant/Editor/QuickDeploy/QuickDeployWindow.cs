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
            "Bundle Creation", "Bundle Deployment", "Loading Screen", "Build"
        };

        private static int _toolbarSelectedButtonIndex;

        // Keep track of the previous tab to remove focus if user moves to a different tab. (b/112536394)
        private static ToolBarSelectedButton _previousTab;

        public enum ToolBarSelectedButton
        {
            CreateBundle,
            DeployBundle,
            LoadingScreen,
            Build
        }

        // Style that provides a light box background.
        // Documentation: https://docs.unity3d.com/ScriptReference/GUISkin-textField.html
        private const string UserInputGuiStyle = "textfield";

        private const int WindowMinWidth = 475;
        private const int WindowMinHeight = 400;

        private const int FieldMinWidth = 100;
        private const int ShortButtonWidth = 100;
        private const int ToolbarHeight = 25;

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
            var window = GetWindow<QuickDeployWindow>(true, "Quick Deploy");
            window.minSize = new Vector2(WindowMinWidth, WindowMinHeight);
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

        void Update()
        {
            // Call Update() on AccessTokenGetter and on WwwRequestInProgress to trigger execution of pending tasks
            // if there are any.
            AccessTokenGetter.Update();
            WwwRequestInProgress.Update();
        }

        void OnGUI()
        {
            _toolbarSelectedButtonIndex = GUILayout.Toolbar(_toolbarSelectedButtonIndex, ToolbarButtonNames,
                GUILayout.MinHeight(ToolbarHeight));
            var currentTab = (ToolBarSelectedButton) _toolbarSelectedButtonIndex;
            UpdateGuiFocus(currentTab);
            switch (currentTab)
            {
                case ToolBarSelectedButton.CreateBundle:
                    AssetBundleBrowserClient.ReloadAndUpdateBrowserInfo();
                    OnGuiCreateBundleSelect();
                    break;
                case ToolBarSelectedButton.DeployBundle:
                    OnGuiDeployBundleSelect();
                    break;
                case ToolBarSelectedButton.LoadingScreen:
                    OnGuiLoadingScreenSelect();
                    break;
                case ToolBarSelectedButton.Build:
                    OnGuiCreateBuildSelect();
                    break;
            }
        }


        /// <summary>
        /// Unfocus the window if the user has just moved to a different quick deploy tab.
        /// </summary>
        /// <param name="currentTab">A ToolBarSelectedButton instance representing the current quick deploy tab.</param>
        /// <see cref="b/112536394"/>
        private static void UpdateGuiFocus(ToolBarSelectedButton currentTab)
        {
            if (currentTab != _previousTab)
            {
                _previousTab = currentTab;
                GUI.FocusControl(null);
            }
        }

        private void OnGuiCreateBundleSelect()
        {
            var descriptionTextStyle = CreateDescriptionTextStyle();

            EditorGUILayout.LabelField("Create AssetBundle", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(UserInputGuiStyle);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Use the Unity Asset Bundle Browser to select your game's main scene " +
                                       "and bundle it (and its dependencies) into an AssetBundle file.",
                descriptionTextStyle);
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                string.Format("Asset Bundle Browser version: {0}",
                    AssetBundleBrowserClient.GetAssetBundleBrowserVersion()),
                EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            // Allow the developer to open the AssetBundles Browser if it is present, otherwise ask them to download it
            if (AssetBundleBrowserClient.AssetBundleBrowserIsPresent())
            {
                if (GUILayout.Button("Open Asset Bundle Browser"))
                {
                    AssetBundleBrowserClient.DisplayAssetBundleBrowser();
                }
            }
            else
            {
                if (GUILayout.Button("Download Asset Bundle Browser from GitHub"))
                {
                    Application.OpenURL("https://github.com/Unity-Technologies/AssetBundles-Browser/releases");
                }

                EditorGUILayout.Space();
                EditorGUILayout.Space();

                EditorGUILayout.Space();
                if (GUILayout.Button("Open Asset Bundle Browser Documentation"))
                {
                    Application.OpenURL("https://docs.unity3d.com/Manual/AssetBundles-Browser.html");
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
        }

        private void OnGuiDeployBundleSelect()
        {
            //TODO: investigate sharing this code
            var descriptionTextStyle = CreateDescriptionTextStyle();

            EditorGUILayout.LabelField("Create Google Cloud Credentials", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(UserInputGuiStyle);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Quick Deploy requires valid credentials to upload the AssetBundle file.",
                descriptionTextStyle);
            EditorGUILayout.LabelField(
                "Open Google Cloud console to create an OAuth 2.0 client ID. Select Application Type \"Other\". " +
                "Download the JSON file containing the credentials.",
                descriptionTextStyle);
            EditorGUILayout.Space();

            if (GUILayout.Button("Open Google Cloud Console"))
            {
                Application.OpenURL("https://console.cloud.google.com/apis/credentials");
            }

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Configure AssetBundle Deployment", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(UserInputGuiStyle);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Specify path to credentials file created above and to AssetBundle file created with  " +
                "AssetBundle Browser. Choose bucket and object names to use for uploaded AssetBundle file.",
                descriptionTextStyle);
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Google Cloud Credentials File Path", GUILayout.MinWidth(FieldMinWidth));
            _cloudCredentialsFileName =
                EditorGUILayout.TextField(_cloudCredentialsFileName, GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Browse", GUILayout.Width(ShortButtonWidth)))
            {
                _cloudCredentialsFileName = EditorUtility.OpenFilePanel("Select cloud credentials file", "", "");
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("AssetBundle File Path", GUILayout.MinWidth(FieldMinWidth));
            _assetBundleFileName = EditorGUILayout.TextField(_assetBundleFileName, GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Browse", GUILayout.Width(ShortButtonWidth)))
            {
                _assetBundleFileName = EditorUtility.OpenFilePanel("Select AssetBundle file", "", "");
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Cloud Storage Bucket Name", GUILayout.MinWidth(FieldMinWidth));
            _cloudStorageBucketName =
                EditorGUILayout.TextField(_cloudStorageBucketName, GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Cloud Storage Object Name", GUILayout.MinWidth(FieldMinWidth));
            _cloudStorageFileName = EditorGUILayout.TextField(_cloudStorageFileName, GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.Space();
            if (GUILayout.Button("Upload to Google Cloud Storage"))
            {
                GcpClient.DeployConfiguredFile();
            }

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
        }

        private void OnGuiLoadingScreenSelect()
        {
            var descriptionTextStyle = CreateDescriptionTextStyle();
            var displayedPath = LoadingScreenGenerator.LoadingScreenImagePath ?? "";
            EditorGUILayout.LabelField("Set AssetBundle URL", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(UserInputGuiStyle);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Specify the URL that points to the deployed AssetBundle. The AssetBundle will be downloaded at game startup. ",
                descriptionTextStyle);
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("AssetBundle URL", GUILayout.MinWidth(FieldMinWidth));
            _assetBundleUrl = EditorGUILayout.TextField(_assetBundleUrl, GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();


            if (GUILayout.Button("Check AssetBundle"))
            {
                if (string.IsNullOrEmpty(QuickDeployConfig.Config.assetBundleUrl))
                {
                    Debug.LogError("AssetBundle URL text field cannot be empty.");
                }
                else
                {
                    var window = AssetBundleVerifierWindow.ShowWindow();
                    window.StartAssetBundleVerificationDownload(_assetBundleUrl);
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();


            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Select Loading Screen Image", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(UserInputGuiStyle);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Choose image to use as background for the loading scene.", descriptionTextStyle);
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Image File Path", GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.TextField(displayedPath, GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Browse", GUILayout.Width(ShortButtonWidth)))
            {
                LoadingScreenGenerator.LoadingScreenImagePath =
                    EditorUtility.OpenFilePanel("Select Image", "", "png,jpg,jpeg,tif,tiff,gif,bmp");
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            if (GUILayout.Button("Create Loading Scene"))
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

            EditorGUILayout.Space();

            EditorGUILayout.EndVertical();
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
            EditorGUILayout.BeginVertical(UserInputGuiStyle);
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("APK File Name", GUILayout.MinWidth(FieldMinWidth));
            _apkFileName = EditorGUILayout.TextField(_apkFileName, GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Browse", GUILayout.Width(ShortButtonWidth)))
            {
                _apkFileName = EditorUtility.SaveFilePanel("Choose file name and location", "", "base.apk", "apk");
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            if (GUILayout.Button("Build Base APK"))
            {
                QuickDeployApkBuilder.BuildQuickDeployInstantGameApk();
            }

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
        }

        private GUIStyle CreateDescriptionTextStyle()
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Italic,
                wordWrap = true
            };
        }
    }
}