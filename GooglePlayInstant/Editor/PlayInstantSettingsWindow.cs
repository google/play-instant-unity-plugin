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
    public class PlayInstantSettingsWindow : EditorWindow
    {
        public const string WindowTitle = "Play Instant Settings";
        private static readonly string[] PlatformOptions = {"Installed", "Instant"};
        private const int FieldMinWidth = 100;

        private readonly IAndroidManifestUpdater _androidManifestUpdater =
#if UNITY_2018_1_OR_NEWER
            new PostGenerateGradleProjectAndroidManifestUpdater();
#else
            new LegacyAndroidManifestUpdater();
#endif

        private bool _isInstant;
        private string _instantUrl;

        private void Awake()
        {
            _isInstant = PlayInstantBuildConfiguration.IsPlayInstantScriptingSymbolDefined();
            _instantUrl = PlayInstantBuildConfiguration.GetInstantUrl();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Platform", GUILayout.MinWidth(FieldMinWidth));
            var index = EditorGUILayout.Popup(_isInstant ? 1 : 0, PlatformOptions, GUILayout.MinWidth(FieldMinWidth));
            _isInstant = index == 1;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            if (_isInstant)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Instant Apps URL (Optional)", GUILayout.MinWidth(FieldMinWidth));
                _instantUrl = EditorGUILayout.TextField(_instantUrl, GUILayout.MinWidth(FieldMinWidth));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();

                var packageName = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android) ?? "package.name";
                EditorGUILayout.LabelField(string.Format(
                    "Instant apps are launched from web search, advertisements, etc via a URL. Specify the URL here " +
                    "and configure Digital Asset Links, or leave the URL blank and one will automatically be " +
                    "provided at https://instant.apps/{0}", packageName), EditorStyles.wordWrappedLabel);
                EditorGUILayout.Space();

                EditorGUILayout.LabelField(
                    string.Format("Note: the symbol \"{0}\" will be defined for scripting with #if {0} / #endif.",
                        PlayInstantBuildConfiguration.PlayInstantScriptingDefineSymbol),
                    EditorStyles.wordWrappedLabel);
                EditorGUILayout.Space();
            }

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
                PlayInstantBuildConfiguration.DefinePlayInstantScriptingSymbol();
                PlayInstantBuildConfiguration.SetInstantUrl(_instantUrl);
                Close();
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
            PlayInstantBuildConfiguration.UndefinePlayInstantScriptingSymbol();
            _androidManifestUpdater.SwitchToInstalled();
            Close();
        }

        private static void DisplayUrlError(string message)
        {
            Debug.LogError(message);
            EditorUtility.DisplayDialog("Invalid Default URL", message, "OK");
        }

        public static void ShowWindow()
        {
            GetWindow(typeof(PlayInstantSettingsWindow), true, WindowTitle);
        }
    }
}