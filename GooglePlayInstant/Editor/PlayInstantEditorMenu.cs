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
    public static class PlayInstantEditorMenu
    {
        [MenuItem("PlayInstant/Documentation/GitHub Project", false, 100)]
        private static void GitHubProject()
        {
            Application.OpenURL("https://github.com/google/play-instant-unity-plugin");
        }

        [MenuItem("PlayInstant/Documentation/Developer Site", false, 101)]
        private static void OpenDevDocumentation()
        {
            Application.OpenURL("https://g.co/InstantApps");
        }

        [MenuItem("PlayInstant/Documentation/Digital Asset Links", false, 102)]
        private static void VerifyAndroidAppLinks()
        {
            Application.OpenURL("https://developer.android.com/training/app-links/verify-site-associations#web-assoc");
        }

        [MenuItem("PlayInstant/Documentation/Open Source License", false, 103)]
        private static void OpenSourceLicense()
        {
            Application.OpenURL("https://www.apache.org/licenses/LICENSE-2.0");
        }

        [MenuItem("PlayInstant/Report a Bug", false, 110)]
        private static void OpenReportBug()
        {
            Application.OpenURL("https://github.com/google/play-instant-unity-plugin/issues");
        }

        [MenuItem("PlayInstant/Configure Instant or Installed...", false, 200)]
        private static void OpenEditorSettings()
        {
            PlayInstantSettingsWindow.ShowWindow();
        }

        [MenuItem("PlayInstant/Check Player Settings...", false, 201)]
        private static void CheckPlayerSettings()
        {
            PlayerAndBuildSettingsWindow.ShowWindow();
        }

        // Note: cannot use string.Format() in an attribute argument.
        [MenuItem("PlayInstant/Set up " + PlayInstantSdkInstaller.InstantAppsSdkName + "...", false, 202)]
        private static void SetUpPlayInstantSdk()
        {
            PlayInstantSdkInstaller.SetUp();
        }
    }
}
