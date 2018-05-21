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

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GooglePlayInstant.Editor
{
    public class PlayerAndBuildSettingsWindow : EditorWindow
    {
        public static void ShowWindow()
        {
            GetWindow(typeof(PlayerAndBuildSettingsWindow), true, "Check Player Settings");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Required changes", EditorStyles.boldLabel);
            AddControls(PlayInstantSettingPolicy.GetRequiredPolicies());
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Recommended changes", EditorStyles.boldLabel);
            AddControls(PlayInstantSettingPolicy.GetRecommendedPolicies());
        }

        private void AddControls(IEnumerable<PlayInstantSettingPolicy> policies)
        {
            var descriptionStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Italic,
                wordWrap = true
            };

            foreach (var policy in policies)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(policy.Name, EditorStyles.wordWrappedLabel);
                if (policy.Description != null)
                {
                    EditorGUILayout.LabelField(policy.Description, descriptionStyle);
                }

                EditorGUILayout.EndVertical();

                if (!policy.IsCorrectState())
                {
                    if (GUILayout.Button("Update", GUILayout.Width(100)))
                    {
                        if (policy.ChangeState())
                        {
                            Debug.LogFormat("Updated setting: {0}", policy.Name);
                        }
                        else
                        {
                            Debug.LogErrorFormat("Failed to update setting: {0}", policy.Name);
                            EditorUtility.DisplayDialog("Error updating", "Failed to update setting", "OK");
                        }

                        Repaint();
                    }
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }
        }
    }
}