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
using GooglePlayInstant.Editor.AndroidManifest;
using UnityEditor;
using UnityEngine;

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// A window for managing settings related to building an instant app, e.g. the instant app's URL.
    /// </summary>
    public class BuildSettingsWindow : EditorWindow
    {
        public const string WindowTitle = "Play Instant Build Settings";
        private const int FieldWidth = 175;
        private static readonly string[] PlatformOptions = {"Installed", "Instant"};

        private readonly IAndroidManifestUpdater _androidManifestUpdater =
#if UNITY_2018_1_OR_NEWER
            new PostGenerateGradleProjectAndroidManifestUpdater();
#else
            new LegacyAndroidManifestUpdater();
#endif

        private bool _isInstant;
        private string _instantUrl;
        private string _scenesInBuild;
        private string _assetBundleManifestPath;

        /// <summary>
        /// Displays this window, creating it if necessary.
        /// </summary>
        public static void ShowWindow()
        {
            GetWindow(typeof(BuildSettingsWindow), true, WindowTitle);
        }

        private void Awake()
        {
            _isInstant = PlayInstantBuildConfiguration.IsInstantBuildType();
            _instantUrl = PlayInstantBuildConfiguration.InstantUrl;
            _scenesInBuild = string.Join(",", PlayInstantBuildConfiguration.ScenesInBuild);
            _assetBundleManifestPath = PlayInstantBuildConfiguration.AssetBundleManifestPath;
        }

        private void OnGUI()
        {
            var descriptionTextStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Italic,
                wordWrap = true
            };

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Android Build Type", EditorStyles.boldLabel, GUILayout.Width(FieldWidth));
            var index = EditorGUILayout.Popup(_isInstant ? 1 : 0, PlatformOptions);
            _isInstant = index == 1;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            if (_isInstant)
            {
                _instantUrl = GetLabelAndTextField("Instant Apps URL (Optional)", _instantUrl);

                var packageName = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android) ?? "package-name";
                EditorGUILayout.LabelField(string.Format(
                    "Instant apps are launched from web search, advertisements, etc via a URL. Specify the URL here " +
                    "and configure Digital Asset Links. Or, leave the URL blank and one will automatically be " +
                    "provided at https://instant.apps/{0}", packageName), descriptionTextStyle);
                EditorGUILayout.Space();
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Scenes in Build", EditorStyles.boldLabel);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(
                    "The scenes in the build are selected via Unity's \"Build Settings\" window. " +
                    "This can be overridden by specifying a comma separated scene list below.", descriptionTextStyle);
                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();
                var defaultScenes = string.IsNullOrEmpty(_scenesInBuild)
                    ? string.Join(", ", PlayInstantBuilder.GetEditorBuildEnabledScenes())
                    : "(overridden)";
                EditorGUILayout.LabelField(
                    string.Format("\"Build Settings\" Scenes: {0}", defaultScenes), EditorStyles.wordWrappedLabel);
                if (GUILayout.Button("Update", GUILayout.Width(100)))
                {
                    GetWindow(Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"), true);
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();

                _scenesInBuild = GetLabelAndTextField("Override Scenes (Optional)", _scenesInBuild);

                _assetBundleManifestPath =
                    GetLabelAndTextField("AssetBundle Manifest (Optional)", _assetBundleManifestPath);

                EditorGUILayout.LabelField(
                    "If you use AssetBundles, provide the path to your AssetBundle Manifest file to ensure that " +
                    "required types are not stripped during the build process.", descriptionTextStyle);
            }
            else
            {
                EditorGUILayout.LabelField(
                    "The \"Installed\" build type is used when creating a traditional installed APK. " +
                    "Select \"Instant\" to build a Google Play Instant APK.", descriptionTextStyle);
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            // Disable the Save button unless one of the fields has changed.
            GUI.enabled = IsAnyFieldChanged();

            if (GUILayout.Button("Save"))
            {
                if (_isInstant)
                {
                    SelectPlatformInstant();
                }
                else
                {
                    SelectPlatformInstalled();
                }
            }

            GUI.enabled = true;
        }

        private bool IsAnyFieldChanged()
        {
            if (_isInstant)
            {
                return !PlayInstantBuildConfiguration.IsInstantBuildType() ||
                       _instantUrl != PlayInstantBuildConfiguration.InstantUrl ||
                       _scenesInBuild != string.Join(",", PlayInstantBuildConfiguration.ScenesInBuild) ||
                       _assetBundleManifestPath != PlayInstantBuildConfiguration.AssetBundleManifestPath;
            }

            // If changing the build type to "Installed", then we don't care about the other fields
            return PlayInstantBuildConfiguration.IsInstantBuildType();
        }

        private static string GetLabelAndTextField(string label, string text)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(FieldWidth));
            var result = EditorGUILayout.TextField(text);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            return result;
        }

        private void SelectPlatformInstant()
        {
            Uri uri = null;
            _instantUrl = _instantUrl == null ? string.Empty : _instantUrl.Trim();
            if (_instantUrl.Length > 0)
            {
                try
                {
                    // TODO: allow port numbers? allow query parameters?
                    uri = new Uri(_instantUrl);
                }
                catch (Exception ex)
                {
                    DisplayUrlError(string.Format("The URL is invalid: {0}", ex.Message));
                    return;
                }

                if (uri.Scheme.ToLower() != "https")
                {
                    DisplayUrlError("The URL scheme should be \"https\"");
                    return;
                }

                if (string.IsNullOrEmpty(uri.Host))
                {
                    DisplayUrlError("If a URL is provided, the host must be specified");
                    return;
                }
            }

            var errorMessage = _androidManifestUpdater.SwitchToInstant(uri);
            if (errorMessage == null)
            {
                PlayInstantBuildConfiguration.SetInstantBuildType();
                PlayInstantBuildConfiguration.SaveConfiguration(
                    _instantUrl, _scenesInBuild.Split(','), _assetBundleManifestPath);
            }
            else
            {
                var message = string.Format("Error updating AndroidManifest.xml: {0}", errorMessage);
                Debug.LogError(message);
                EditorUtility.DisplayDialog("Error Saving", message, "OK");
            }
        }

        private void SelectPlatformInstalled()
        {
            PlayInstantBuildConfiguration.SetInstalledBuildType();
            _androidManifestUpdater.SwitchToInstalled();
        }

        private static void DisplayUrlError(string message)
        {
            Debug.LogError(message);
            EditorUtility.DisplayDialog("Invalid Default URL", message, "OK");
        }
    }
}