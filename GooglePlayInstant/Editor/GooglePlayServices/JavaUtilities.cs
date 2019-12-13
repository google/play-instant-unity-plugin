// <copyright file="JdkChecker.cs" company="Google Inc.">
// Copyright (C) 2017 Google Inc. All Rights Reserved.
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>

namespace GooglePlayInstant.Editor.GooglePlayServices {
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text.RegularExpressions;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Utilities to determine Java tool installation and validate the Java installation for the
    /// project's build settings.
    /// </summary>
    public static class JavaUtilities {

        /// <summary>
        /// Exception thrown if a Java tool isn't found.
        /// </summary>
        public class ToolNotFoundException : ApplicationException {
            public ToolNotFoundException(string message) : base(message) {}
        }

        /// <summary>
        /// Environment variable used to specify the Java distribution directory.
        /// </summary>
        private const string JAVA_HOME = "JAVA_HOME";

        /// <summary>
        /// Minimum JDK version required to build with recently released Android libraries.
        /// </summary>
        private const float MINIMUM_JDK_VERSION_MAJOR_MINOR = 1.8f;

        /// <summary>
        /// Get the JDK path (JAVA_HOME) configured in the Unity editor.
        /// </summary>
        private static string EditorJavaHome {
            get
            {
#if UNITY_2019_3_OR_NEWER
                return UnityEditor.Android.AndroidExternalToolsSettings.jdkRootPath;
#else
                var jdkPath = GetEmbeddedJdkPath();
                return string.IsNullOrEmpty(jdkPath) ? EditorPrefs.GetString("JdkPath") : jdkPath;
#endif
            }
        }

        /// <summary>
        /// Returns the path to Unity's embedded version of OpenJDK, or null if not enabled.
        /// Unity 2018.3 added the option to use a version of OpenJDK that is installed with Unity.
        /// </summary>
        private static string GetEmbeddedJdkPath() {
            // Users switching between Unity versions may have JdkUseEmbedded present in their preferences even though
            // their current version of Unity does not support it. So we return null for older versions to avoid that
            // case.
#if !UNITY_2018_3_OR_NEWER
            return null;
#else
            // For Unity 2018.3 through 2019.2 construct the embedded JDK path as follows.
            if (EditorPrefs.GetInt("JdkUseEmbedded") != 1) {
                return null;
            }

            string pathRelativeToEditor;
            switch (Application.platform) {
                case RuntimePlatform.LinuxEditor:
                    pathRelativeToEditor = "Data/PlaybackEngines/AndroidPlayer/Tools/OpenJdk/Linux/";
                    break;
                case RuntimePlatform.OSXEditor:
                    pathRelativeToEditor = "PlaybackEngines/AndroidPlayer/Tools/OpenJdk/MacOS/";
                    break;
                case RuntimePlatform.WindowsEditor:
                    pathRelativeToEditor = "Data/PlaybackEngines/AndroidPlayer/Tools/OpenJdk/Windows/";
                    break;
                default:
                    throw new Exception(
                        "Cannot get embedded JDK path for unsupported platform: " + Application.platform);
            }

            var unityEditor = new FileInfo(EditorApplication.applicationPath);
            if (unityEditor.DirectoryName == null) {
                throw new Exception(
                    "Cannot get embedded JDK path. EditorApplication.applicationPath returned an unexpected value: " +
                    EditorApplication.applicationPath);
            }

            return Path.Combine(unityEditor.DirectoryName, pathRelativeToEditor);
#endif
        }

        /// <summary>
        /// Find the JDK path (JAVA_HOME) either configured in the Unity editor or via the JAVA_HOME
        /// environment variable.
        /// </summary>
        private static string JavaHome {
            get {
                var javaHome = EditorJavaHome;
                if (String.IsNullOrEmpty(javaHome)) {
                    javaHome = Environment.GetEnvironmentVariable(JAVA_HOME);
                }
                return javaHome;
            }
        }

        /// <summary>
        /// Get the path to the "jar" binary.
        /// </summary>
        /// <returns>Path to the "jar" binary if successful, throws ToolNotFoundException
        /// otherwise.</returns>
        internal static string JarBinaryPath {
            get { return FindJavaTool("jar"); }
        }

        /// <summary>
        /// Get the path to the "java" binary.
        /// </summary>
        /// <returns>Path to the "java" binary if successful, throws ToolNotFoundException
        /// otherwise.</returns>
        public static string JavaBinaryPath {
            get { return FindJavaTool("java"); }
        }

        static JavaUtilities() {
            // TODO(smiles): Register a check of the JDK version vs. the current build settings.
        }

        /// <summary>
        /// Construct a path to a binary in the Java distribution.
        /// </summary>
        /// <param name="javaTool">Name of the tool within the Java binary directory.</param>
        /// <returns>Path to the tool if it exists, null otherwise.</returns>
        private static string JavaHomeBinaryPath(string javaTool) {
            if (!String.IsNullOrEmpty(JavaHome)) {
                string toolPath = Path.Combine(
                   JavaHome, Path.Combine("bin", javaTool + CommandLine.GetExecutableExtension()));
                if (File.Exists(toolPath)) {
                    return toolPath;
                }
            }
            return null;
        }

        /// <summary>
        /// Find a Java tool.
        /// </summary>
        /// <param name="javaTool">Name of the tool to search for.</param>
        /// <returns>Path to the tool if it's found, throws a ToolNotFoundException
        /// otherwise.</returns>
        private static string FindJavaTool(string javaTool)
        {
            var javaHome = JavaHome;
            string toolPath = null;
            if (!String.IsNullOrEmpty(javaHome)) {
                toolPath = JavaHomeBinaryPath(javaTool);
                if (String.IsNullOrEmpty(toolPath)) {
                    EditorUtility.DisplayDialog(
                        "Android Resolver",
                        String.Format("{0} environment references a directory ({1}) that does " +
                                      "not contain {2} which is required to process Android " +
                                      "libraries.", JAVA_HOME, javaHome, toolPath),
                        "OK");
                    throw new ToolNotFoundException(
                        String.Format("{0} not found, {1} references incomplete Java distribution.",
                                      javaTool, javaHome));

                }
            } else {
                toolPath = CommandLine.FindExecutable(javaTool);
                if (!File.Exists(toolPath)) {
                    EditorUtility.DisplayDialog(
                        "Android Resolver",
                        String.Format("Unable to find {0} in the system path.  This tool is " +
                                      "required to process Android libraries.  Please configure " +
                                      "your JDK location under the " +
                                      "'Unity Preferences > External Tools' menu.",
                                      javaTool),
                        "OK");
                    throw new ToolNotFoundException(javaTool + " not found.");
                }
            }
            return toolPath;
        }

        /// <summary>
        /// Log Jdk version parsing failed warning.
        /// </summary>
        /// <param name="javaPath">Path to the java tool.</param>
        /// <param name="commandLineSummary">Summary of the executed command line.</param>
        private static void LogJdkVersionFailedWarning(string javaPath, string commandLineSummary) {
            PlayServicesResolver.Log(
                String.Format(
                    "Failed to get Java version when running {0}\n" +
                    "It is not be possible to verify your Java installation is new enough to " +
                    "compile with the latest Android SDK\n\n" +
                    "{1}", javaPath, commandLineSummary),
                level: LogLevel.Warning);
        }

        /// <summary>
        /// Determine whether the user's JDK is sufficient for the Android SDK and recently
        /// released libraries.
        /// </summary>
        internal static void CheckJdkForApiLevel() {
            // Get JAVA_HOME path from the editor settings.
            string javaPath = null;
            try {
                javaPath = JavaBinaryPath;
            } catch (ToolNotFoundException) {
                return;
            }
            var result = CommandLine.Run(javaPath, "-version", Directory.GetCurrentDirectory(),
                                         envVars: new Dictionary<string, string> {
                                             { JAVA_HOME, JavaHome }
                                         });
            if (result.exitCode != 0) {
                LogJdkVersionFailedWarning(javaPath, result.message);
                return;
            }
            float majorMinorVersion = 0;
            // The version string is can be reported via stderr or stdout so scrape the
            // concatenated message string.
            string pattern = "^(?<model>java||openjdk) version \"(?<major>\\d).(?<minor>\\d).(?<patch>\\d).*$";
            
            Match match = Regex.Match(result.message,pattern,RegexOptions.Multiline);
            if (match.Success) {
                float.TryParse(match.Groups["major"].Value + "." + match.Groups["minor"].Value, NumberStyles.Any,
                                        CultureInfo.InvariantCulture, out majorMinorVersion);
            }
            if (majorMinorVersion == 0) {
                LogJdkVersionFailedWarning(javaPath, result.message);
                return;
            }
            // If the user's installed JDK is too old, report an error.
            if (majorMinorVersion < MINIMUM_JDK_VERSION_MAJOR_MINOR) {
                PlayServicesResolver.Log(
                    String.Format("The configured JDK {0} is too old to build Android " +
                                  "applications with recent libraries.\n" +
                                  "Please install JDK version {1} or newer and configure Unity " +
                                  "to use the new JDK installation in the " +
                                  "'Unity Preferences > External Tools' menu.\n",
                                  majorMinorVersion, MINIMUM_JDK_VERSION_MAJOR_MINOR),
                    level: LogLevel.Error);
            }
        }
    }

}
