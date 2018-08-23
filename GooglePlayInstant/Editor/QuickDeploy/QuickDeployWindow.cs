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


        private string _loadingScreenImagePath;

        // Titles for errors that occur
        private const string AssetBundleBrowserErrorTitle = "AssetBundle Browser Error";
        private const string AssetBundleDeploymentErrorTitle = "AssetBundle Deployment Error";
        private const string AssetBundleCheckerErrorTitle = "AssetBundle Checker Error";
        private const string LoadingScreenCreationErrorTitle = "Loading Screen Creation Error";


        public static void ShowWindow(ToolBarSelectedButton select)
        {
            var window = GetWindow<QuickDeployWindow>(true, "Quick Deploy");

            window.minSize = new Vector2(WindowMinWidth, WindowMinHeight);
            _toolbarSelectedButtonIndex = (int) select;
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
                    try
                    {
                        AssetBundleBrowserClient.DisplayAssetBundleBrowser();
                    }
                    catch (Exception ex)
                    {
                        DialogHelper.DisplayMessage(AssetBundleBrowserErrorTitle,
                            ex.Message);

                        throw;
                    }
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
            QuickDeployConfig.CloudCredentialsFileName =
                EditorGUILayout.TextField(QuickDeployConfig.CloudCredentialsFileName,
                    GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Browse", GUILayout.Width(ShortButtonWidth)))
            {
                QuickDeployConfig.CloudCredentialsFileName =
                    EditorUtility.OpenFilePanel("Select cloud credentials file", "", "");
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("AssetBundle File Path", GUILayout.MinWidth(FieldMinWidth));
            QuickDeployConfig.AssetBundleFileName = EditorGUILayout.TextField(QuickDeployConfig.AssetBundleFileName,
                GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Browse", GUILayout.Width(ShortButtonWidth)))
            {
                QuickDeployConfig.AssetBundleFileName = EditorUtility.OpenFilePanel("Select AssetBundle file", "", "");
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Cloud Storage Bucket Name", GUILayout.MinWidth(FieldMinWidth));
            QuickDeployConfig.CloudStorageBucketName =
                EditorGUILayout.TextField(QuickDeployConfig.CloudStorageBucketName, GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Cloud Storage Object Name", GUILayout.MinWidth(FieldMinWidth));
            QuickDeployConfig.CloudStorageObjectName =
                EditorGUILayout.TextField(QuickDeployConfig.CloudStorageObjectName, GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.Space();
            if (GUILayout.Button("Upload to Google Cloud Storage"))
            {
                try
                {
                    QuickDeployConfig.SaveConfiguration(ToolBarSelectedButton.DeployBundle);
                    GcpClient.DeployConfiguredFile();
                }
                catch (Exception ex)
                {
                    DialogHelper.DisplayMessage(AssetBundleDeploymentErrorTitle, ex.Message);

                    throw;
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.EndVertical();
        }

        private void OnGuiLoadingScreenSelect()
        {
            var descriptionTextStyle = CreateDescriptionTextStyle();

            EditorGUILayout.LabelField("Set AssetBundle URL", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(UserInputGuiStyle);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Specify the URL that points to the deployed AssetBundle. The AssetBundle will be downloaded at game startup. ",
                descriptionTextStyle);
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("AssetBundle URL", GUILayout.MinWidth(FieldMinWidth));
            QuickDeployConfig.AssetBundleUrl =
                EditorGUILayout.TextField(QuickDeployConfig.AssetBundleUrl, GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            if (GUILayout.Button("Check AssetBundle"))
            {
                var window = AssetBundleVerifierWindow.ShowWindow();

                try
                {
                    QuickDeployConfig.SaveConfiguration(ToolBarSelectedButton.LoadingScreen);
                    window.StartAssetBundleVerificationDownload(QuickDeployConfig.AssetBundleUrl);
                }
                catch (Exception ex)
                {
                    DialogHelper.DisplayMessage(AssetBundleCheckerErrorTitle, ex.Message);

                    window.Close();

                    throw;
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

            _loadingScreenImagePath =
                EditorGUILayout.TextField(_loadingScreenImagePath, GUILayout.MinWidth(FieldMinWidth));

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Browse", GUILayout.Width(ShortButtonWidth)))
            {
                _loadingScreenImagePath =
                    EditorUtility.OpenFilePanel("Select Image", "", "png,jpg,jpeg,tif,tiff,gif,bmp");
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            if (GUILayout.Button("Create Loading Scene"))
            {
                try
                {
                    QuickDeployConfig.SaveConfiguration(ToolBarSelectedButton.LoadingScreen);
                    LoadingScreenGenerator.GenerateLoadingScreenScene(QuickDeployConfig.AssetBundleUrl,
                        _loadingScreenImagePath);
                }
                catch (Exception ex)
                {
                    DialogHelper.DisplayMessage(LoadingScreenCreationErrorTitle, ex.Message);
                    throw;
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.EndVertical();
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
            QuickDeployConfig.ApkFileName =
                EditorGUILayout.TextField(QuickDeployConfig.ApkFileName, GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Browse", GUILayout.Width(ShortButtonWidth)))
            {
                QuickDeployConfig.ApkFileName =
                    EditorUtility.SaveFilePanel("Choose file name and location", "", "base.apk", "apk");
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            if (GUILayout.Button("Build Base APK"))
            {
                QuickDeployConfig.SaveConfiguration(ToolBarSelectedButton.Build);
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