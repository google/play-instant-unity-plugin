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
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace GooglePlayInstant.Editor.QuickDeploy
{
    public class QuickDeployWindow : EditorWindow
    {
        /// <summary>
        /// Saved configurations from a previous session.
        /// </summary>
        public static readonly QuickDeployConfig Config = new QuickDeployConfig();
        
        private static readonly string[] ToolbarButtonNames =
        {
            "Overview", "Bundle Creation", "Bundle Deployment", "Loading Screen"
        };

        private static int _toolbarSelectedButtonIndex;

        // Keep track of the previous tab to remove focus if user moves to a different tab. (b/112536394)
        private static ToolBarSelectedButton _previousTab;

        public enum ToolBarSelectedButton
        {
            Overview,
            CreateBundle,
            DeployBundle,
            LoadingScreen
        }

        // Style that provides a light box background.
        // Documentation: https://docs.unity3d.com/ScriptReference/GUISkin-textField.html
        private const string UserInputGuiStyle = "textfield";

        private const int WindowMinWidth = 475;
        private const int WindowMinHeight = 400;

        private const int SceneViewDeltaFromTop = 230;

        private const int FieldMinWidth = 100;
        private const int ShortButtonWidth = 100;
        private const int ToolbarHeight = 25;

        private string _loadingScreenImagePath;

        // Titles for errors that occur
        private const string AssetBundleBuildErrorTitle = "AssetBundle Build Error";
        private const string AssetBundleDeploymentErrorTitle = "AssetBundle Deployment Error";
        private const string AssetBundleCheckerErrorTitle = "AssetBundle Checker Error";
        private const string LoadingScreenCreationErrorTitle = "Loading Screen Creation Error";
        private const string LoadingScreenUpdateErrorTitle = "Loading Screen Update Error";

        private PlayInstantSceneTreeView _playInstantSceneTreeTreeView;
        private TreeViewState _treeViewState;

        public static void ShowWindow(ToolBarSelectedButton select)
        {
            var window = GetWindow<QuickDeployWindow>(true, "Quick Deploy");

            window.minSize = new Vector2(WindowMinWidth, WindowMinHeight);
            _toolbarSelectedButtonIndex = (int) select;
            
            Config.LoadConfiguration();
        }

        void OnEnable()
        {
            _treeViewState = new TreeViewState();

            _playInstantSceneTreeTreeView = new PlayInstantSceneTreeView(_treeViewState);
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
                case ToolBarSelectedButton.Overview:
                    OnGuiOverviewSelect();
                    break;
                case ToolBarSelectedButton.CreateBundle:
                    OnGuiCreateBundleSelect();
                    break;
                case ToolBarSelectedButton.DeployBundle:
                    OnGuiDeployBundleSelect();
                    break;
                case ToolBarSelectedButton.LoadingScreen:
                    OnGuiLoadingScreenSelect();
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

        private void OnGuiOverviewSelect()
        {
            var descriptionTextStyle = CreateDescriptionTextStyle();
            EditorGUILayout.LabelField("About Quick Deploy", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(UserInputGuiStyle);
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Quick Deploy can significantly reduce the size of a Unity-based instant app " +
                                       "by packaging some assets in an AssetBundle that is retrieved from a server " +
                                       "during app startup.", descriptionTextStyle);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                "Use the \"Bundle Creation\" tab to build an AssetBundle containing the game's " +
                "main scene. Then upload the AssetBundle to Google Cloud Storage via the " +
                "\"Bundle Deployment\" tab. Finally, use the \"Loading Screen\" tab to select an " +
                "image to display on the loading screen and the URL that points to the uploaded " +
                "AssetBundle.", descriptionTextStyle);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(string.Format("Use Google Play Instant's \"{0}\" window to customize the " +
                                                     "scenes included in the instant app. Then use the \"{1}\" menu " +
                                                     "option to test the instant app loading the AssetBundle from the " +
                                                     "remote server. Finally, select the \"{2}\" menu option to build " +
                                                     "the app in a manner suitable for publishing on Play Console.",
                "Build Settings", "Build and Run", "Build for Play Console"), descriptionTextStyle);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
        }

        private void OnGuiCreateBundleSelect()
        {
            var descriptionTextStyle = CreateDescriptionTextStyle();

            EditorGUILayout.LabelField("Create AssetBundle", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(UserInputGuiStyle);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Select scenes to be put into an AssetBundle and then build it.",
                descriptionTextStyle);

            EditorGUILayout.Space();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical(UserInputGuiStyle);
            EditorGUILayout.Space();

            _playInstantSceneTreeTreeView.OnGUI(GUILayoutUtility.GetRect(position.width,
                position.height - SceneViewDeltaFromTop));
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Add Open Scenes"))
            {
                _playInstantSceneTreeTreeView.AddOpenScenes();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.Space();


            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("AssetBundle File Path", GUILayout.MinWidth(FieldMinWidth));
            Config.AssetBundleFileName = EditorGUILayout.TextField(Config.AssetBundleFileName,
                GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Browse", GUILayout.Width(ShortButtonWidth)))
            {
                Config.AssetBundleFileName = EditorUtility.SaveFilePanel("Save AssetBundle", "", "", "");
                HandleDialogExit();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Build AssetBundle"))
            {
                try
                {
                    Config.SaveConfiguration(ToolBarSelectedButton.CreateBundle);
                    AssetBundleBuilder.BuildQuickDeployAssetBundle(GetEnabledSceneItemPaths());
                }
                catch (Exception ex)
                {
                    DialogHelper.DisplayMessage(AssetBundleBuildErrorTitle,
                        ex.Message);
                    throw;
                }

                HandleDialogExit();
            }

            EditorGUILayout.EndHorizontal();
        }

        private string[] GetEnabledSceneItemPaths()
        {
            var scenes = _playInstantSceneTreeTreeView.GetRows();
            var scenePaths = new List<string>();

            foreach (var scene in scenes)
            {
                if (((PlayInstantSceneTreeView.SceneItem) scene).Enabled)
                {
                    scenePaths.Add(scene.displayName);
                }
            }

            return scenePaths.ToArray();
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
            Config.CloudCredentialsFileName =
                EditorGUILayout.TextField(Config.CloudCredentialsFileName,
                    GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Browse", GUILayout.Width(ShortButtonWidth)))
            {
                Config.CloudCredentialsFileName =
                    EditorUtility.OpenFilePanel("Select cloud credentials file", "", "");
                HandleDialogExit();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("AssetBundle File Path", GUILayout.MinWidth(FieldMinWidth));
            Config.AssetBundleFileName = EditorGUILayout.TextField(Config.AssetBundleFileName,
                GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Browse", GUILayout.Width(ShortButtonWidth)))
            {
                Config.AssetBundleFileName = EditorUtility.OpenFilePanel("Select AssetBundle file", "", "");
                HandleDialogExit();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Cloud Storage Bucket Name", GUILayout.MinWidth(FieldMinWidth));
            Config.CloudStorageBucketName =
                EditorGUILayout.TextField(Config.CloudStorageBucketName, GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Cloud Storage Object Name", GUILayout.MinWidth(FieldMinWidth));
            Config.CloudStorageObjectName =
                EditorGUILayout.TextField(Config.CloudStorageObjectName, GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.Space();
            if (GUILayout.Button("Upload to Google Cloud Storage"))
            {
                try
                {
                    Config.SaveConfiguration(ToolBarSelectedButton.DeployBundle);
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
            Config.AssetBundleUrl =
                EditorGUILayout.TextField(Config.AssetBundleUrl, GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            var setAssetBundleText = QuickDeployConfig.EngineConfigExists()
                ? "Update AssetBundle URL"
                : "Set AssetBundle URL";

            if (GUILayout.Button(setAssetBundleText))
            {
                try
                {
                    Config.SaveConfiguration(ToolBarSelectedButton.LoadingScreen);
                }
                catch (Exception ex)
                {
                    DialogHelper.DisplayMessage(AssetBundleCheckerErrorTitle, ex.Message);

                    throw;
                }
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Check AssetBundle"))
            {
                var window = AssetBundleVerifierWindow.ShowWindow();

                try
                {
                    Config.SaveConfiguration(ToolBarSelectedButton.LoadingScreen);
                    window.StartAssetBundleDownload(Config.AssetBundleUrl);
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
                HandleDialogExit();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            if (LoadingScreenGenerator.LoadingScreenExists())
            {
                if (GUILayout.Button("Update Loading Scene"))
                {
                    try
                    {
                        Config.SaveConfiguration(ToolBarSelectedButton.LoadingScreen);
                        LoadingScreenGenerator.AddImageToScene(LoadingScreenGenerator.GetLoadingScreenCanvasObject(),
                            _loadingScreenImagePath);
                    }
                    catch (Exception ex)
                    {
                        DialogHelper.DisplayMessage(LoadingScreenUpdateErrorTitle, ex.Message);
                        throw;
                    }
                }
            }
            else
            {
                if (GUILayout.Button("Create Loading Scene"))
                {
                    try
                    {
                        Config.SaveConfiguration(ToolBarSelectedButton.LoadingScreen);
                        LoadingScreenGenerator.GenerateScene(Config.AssetBundleUrl,
                            _loadingScreenImagePath);
                    }
                    catch (Exception ex)
                    {
                        DialogHelper.DisplayMessage(LoadingScreenCreationErrorTitle, ex.Message);
                        throw;
                    }
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.EndVertical();
        }

        // Call this method after any of the SaveFilePanels and OpenFilePanels placed inbetween BeginHorizontal()s or
        // BeginVerticals()s. An error is thrown when a user switches contexts (into a different desktop), and the
        // window reloads. After completing an action, this error is thrown. This method is called to avoid this.
        //  Fix documentation: https://answers.unity.com/questions/1353442/editorutilitysavefilepane-and-beginhorizontal-caus.html
        private void HandleDialogExit()
        {
            GUIUtility.ExitGUI();
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