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

namespace GooglePlayInstant.Editor
{
    public class PlayInstantQuickDeployWindow : EditorWindow
    {
        private static readonly string[] ToolbarButtonNames =
        {
            "Create Bundle", "Deploy Bundle", "Verify Bundle",
            "Loading Screen", "Build"
        };

        private static int _toolbarSelectedButtonIndex = 0;

        private static string _loadingScreenImagePath;
        private static string _assetBundleUrl;

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

        public static void ShowWindow(ToolBarSelectedButton select)
        {
            GetWindow<PlayInstantQuickDeployWindow>("Quick Deploy");
            _toolbarSelectedButtonIndex = (int) select;
        }

        // TODO: replace stub strings with real values
        void OnGUI()
        {
            _toolbarSelectedButtonIndex = GUILayout.Toolbar(_toolbarSelectedButtonIndex, ToolbarButtonNames);
            switch ((ToolBarSelectedButton) _toolbarSelectedButtonIndex)
            {
                case ToolBarSelectedButton.CreateBundle:
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
        }

        private void OnGuiCreateBundleSelect()
        {
            EditorGUILayout.LabelField("AssetBundle Creation", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Use the Unity Asset Bundle Browser to select your game's main scene " +
                                       "and bundle it (and its dependencies) into an AssetBundle file.",
                EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(string.Format("AssetBundle Browser version: {0}", "not found"),
                EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();
            GUILayout.Button("Download AssetBundle Browser", GUILayout.Width(ButtonWidth));
            EditorGUILayout.Space();
            GUILayout.Button("Open AssetBundle Browser", GUILayout.Width(ButtonWidth));
        }

        private void OnGuiDeployBundleSelect()
        {
            EditorGUILayout.LabelField("AssetBundle Deployment", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Use the Google Cloud Storage to host the AssetBundle as a public " +
                                       "file. Or host the file on your own CDN.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Asset Bundle File Name", GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.TextField("c:\\mygame.assetbundle", GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Cloud Storage Bucket Name", GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.TextField("mycorp_awesome_game", GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Cloud Storage File Name", GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.TextField("mainscene", GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("PCloud Credentials", GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.TextField("c:\\path\\to\\credentials.json", GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();
            GUILayout.Button("Upload to Cloud Storage", GUILayout.Width(ButtonWidth));
            EditorGUILayout.Space();
            GUILayout.Button("Open Cloud Storage Console", GUILayout.Width(ButtonWidth));
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
            EditorGUILayout.TextField("http://storage.googleapis.com/mycorp_awesome_game/mainscene",
                GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical();
            GUILayout.Button("Verify AssetBundle", GUILayout.Width(ButtonWidth));
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
            if (GUILayout.Button("Upload Loading Image", GUILayout.Width(ButtonWidth)))
            {
                _loadingScreenImagePath =
                    EditorUtility.OpenFilePanel("Select Image", "", "png,jpg,jpeg,tif,tiff,gif,bmp");
            }

            EditorGUILayout.Space();

            var displayedPath = _loadingScreenImagePath ?? "no file specified";
            EditorGUILayout.LabelField(string.Format("Image file: {0}", displayedPath),
                GUILayout.MinWidth(FieldMinWidth));

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            if (GUILayout.Button("Create Loading Scene", GUILayout.Width(ButtonWidth)))
            {
                PlayInstantLoadingScreenGenerator.GenerateLoadingScreenScene(_loadingScreenImagePath,
                    _assetBundleUrl);
            }
        }

        private void OnGuiCreateBuildSelect()
        {
            EditorGUILayout.LabelField("Deployment", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Build the APK using the IL2CPP engine.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("APK File Name", GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.TextField("c:\\base.apk", GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            GUILayout.Button("Build Base APK", GUILayout.Width(ButtonWidth));
        }
    }
}