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
using GooglePlayInstant.Editor.GooglePlayServices;
using UnityEngine;

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// A CommandLineDialog that waits to run the specified command until after an AppDomain reset.
    ///
    /// Several seconds after a call to BuildPlayer finishes, Unity reloads all Editor scripts and resets the
    /// AppDomain, causing any running threads to be aborted. This EditorWindow waits to run the specified
    /// command until after the AppDomain is reset.
    /// </summary>
    public class PostBuildCommandLineDialog : CommandLineDialog
    {
        [SerializeField] public CommandLineParameters CommandLineParams;

        // Use two bool fields to detect when this script is reloaded and the AppDomain is reset.
        // See https://docs.unity3d.com/Manual/script-Serialization.html
        private static bool _nonserializedField;
        [SerializeField] private bool _serializedField;

        private void Awake()
        {
            CommandLineParams = new CommandLineParameters();
            _nonserializedField = false;
            _serializedField = false;
        }

        protected override void OnGUI()
        {
            // This block is entered twice: when the window is initially shown and later after the AppDomain is reset.
            if (!_nonserializedField)
            {
                _nonserializedField = true;
                if (_serializedField)
                {
                    Debug.Log("The AppDomain has been reset");
                    RunCommandAsync();
                }
                else
                {
                    Debug.Log("Waiting for the AppDomain reset...");
                    _serializedField = true;
                }
            }

            base.OnGUI();
        }

        private void RunCommandAsync()
        {
            RunAsync(
                CommandLineParams.FileName,
                CommandLineParams.Arguments,
                commandLineResult =>
                {
                    if (commandLineResult.exitCode == 0)
                    {
                        Close();
                    }
                    else
                    {
                        // After adding the button we need to scroll down a little more.
                        scrollPosition.y = Mathf.Infinity;
                        noText = "Close";
                        Repaint();
                    }
                },
                envVars: CommandLineParams.EnvironmentVariables);
        }

        /// <summary>
        /// Creates a dialog box which can display command line output.
        /// </summary>
        public static PostBuildCommandLineDialog CreateDialog(string title)
        {
            var window = (PostBuildCommandLineDialog) GetWindow(typeof(PostBuildCommandLineDialog), true, title);
            window.Initialize();
            return window;
        }
    }
}