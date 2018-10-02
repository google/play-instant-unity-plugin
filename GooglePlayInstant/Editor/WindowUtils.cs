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

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// Helper methods for working with <see cref="EditorWindow"/>.
    /// </summary>
    public static class WindowUtils
    {
        private const float ShortButtonWidth = 100.0f;

        public static void CreateRightAlignedButton(string text, Action action)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(text, GUILayout.Width(ShortButtonWidth)))
            {
                action();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}