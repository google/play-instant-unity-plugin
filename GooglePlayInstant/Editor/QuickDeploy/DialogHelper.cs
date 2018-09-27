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
using UnityEditor;

namespace GooglePlayInstant.Editor.QuickDeploy
{
    /// <summary>
    /// Utility class for displaying popup window messages.
    /// </summary>
    public static class DialogHelper
    {

        private const string OkButtonText = "OK";

        /// <summary>
        /// Displays a popup window that displays a message with a specified title.
        /// </summary>
        public static void DisplayMessage(string title, string message)
        {
            EditorUtility.DisplayDialog(title, message, OkButtonText);
        }

        /// <summary>
        /// Displays a save dialog pointing to the default path.
        /// If the path points to a file then that file will be used as the default file name.
        /// </summary>
        /// <returns>The user selected path</returns>
        public static string SaveFilePanel(string title, string defaultPath, string extension)
        {

            string fileName;
            try
            {
                fileName = Path.GetFileName(defaultPath);
            }
            catch (ArgumentException)
            {
                fileName = "";
            }
            
            string directory;
            try
            {
                directory = Path.GetDirectoryName(defaultPath);
            }
            catch (ArgumentException)
            {
                directory = "";
            }

            return EditorUtility.SaveFilePanel(title, directory, fileName, extension);
        }
    }
}