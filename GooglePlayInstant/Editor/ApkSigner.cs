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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using GooglePlayInstant.Editor.GooglePlayServices;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// Provides methods that call the Android SDK build tool "apksigner" to verify whether an APK complies with
    /// <a href="https://source.android.com/security/apksigning/v2">APK Signature Scheme V2</a> and to re-sign
    /// the APK if not. Instant apps require Signature Scheme V2 starting with Android O, however some Unity versions
    /// do not produce compliant APKs. Without this "adb install --ephemeral" on an Android O device will fail with
    /// "INSTALL_PARSE_FAILED_NO_CERTIFICATES: No APK Signature Scheme v2 signature in ephemeral package".
    /// </summary>
    public static class ApkSigner
    {
        private const string AndroidDebugKeystore = ".android/debug.keystore";

        /// <summary>
        /// Returns true if apksigner is available to call, false otherwise.
        /// </summary>
        public static bool IsAvailable()
        {
            return GetApkSignerJarPath() != null;
        }

        /// <summary>
        /// Synchronously calls the apksigner tool to verify whether the specified APK uses APK Signature Scheme V2.
        /// </summary>
        /// <returns>true if the specified APK uses APK Signature Scheme V2, false otherwise</returns>
        public static bool Verify(string apkPath)
        {
            var arguments = string.Format(
                "-jar {0} verify {1}",
                CommandLine.QuotePathIfNecessary(GetApkSignerJarPath()),
                CommandLine.QuotePathIfNecessary(apkPath));

            var result = CommandLine.Run(JavaUtilities.JavaBinaryPath, arguments);
            if (result.exitCode == 0)
            {
                Debug.Log("Verified APK Signature Scheme V2.");
                return true;
            }

            // Logging at info level since the most common failure (V2 signature missing) is normal.
            Debug.LogFormat("APK Signature Scheme V2 verification failed: {0}", result.message);
            return false;
        }

        /// <summary>
        /// Synchronously calls the apksigner tool to sign the specified APK using APK Signature Scheme V2.
        /// </summary>
        /// <returns>true if the specified APK was successfully signed, false otherwise</returns>
        public static bool Sign(string apkPath)
        {
            string keystoreName;
            string keystorePass;
            string keyaliasName;
            string keyaliasPass;
            if (string.IsNullOrEmpty(PlayerSettings.Android.keystoreName))
            {
                Debug.Log("No keystore specified. Signing using Android debug keystore.");
                var homePath =
                    Application.platform == RuntimePlatform.WindowsEditor
                        ? Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%")
                        : Environment.GetEnvironmentVariable("HOME");
                if (string.IsNullOrEmpty(homePath))
                {
                    Debug.LogError("Failed to locate home directory that contains Android debug keystore.");
                    return false;
                }

                keystoreName = Path.Combine(homePath, AndroidDebugKeystore);
                keystorePass = "android";
                keyaliasName = "androiddebugkey";
                keyaliasPass = "android";
            }
            else
            {
                keystoreName = PlayerSettings.Android.keystoreName;
                keystorePass = PlayerSettings.Android.keystorePass;
                keyaliasName = PlayerSettings.Android.keyaliasName;
                keyaliasPass = PlayerSettings.Android.keyaliasPass;
            }

            if (!File.Exists(keystoreName))
            {
                Debug.LogErrorFormat("Failed to locate keystore file: {0}", keystoreName);
                return false;
            }

            // This command will sign the APK file {3} using key {2} contained in keystore file {1}.
            // ApkSignerResponder will provide passwords using the default method of stdin, so no need
            // to specify "--ks-pass" or "--key-pass". ApkSignerResponder will encode the passwords
            // with UTF8, so we specify "--pass-encoding utf-8" here.
            var arguments = string.Format(
                "-jar {0} sign --ks {1} --ks-key-alias {2} --pass-encoding utf-8 {3}",
                CommandLine.QuotePathIfNecessary(GetApkSignerJarPath()),
                CommandLine.QuotePathIfNecessary(keystoreName),
                keyaliasName,
                CommandLine.QuotePathIfNecessary(apkPath));

            var promptToPasswordDictionary = new Dictionary<string, string>
            {
                // Example keystore password prompt: "Keystore password for signer #1: "
                {"Keystore password for signer", keystorePass},
                // Example keyalias password prompt: "Key \"androiddebugkey\" password for signer #1: "
                {"password for signer", keyaliasPass}
            };
            var responder = new ApkSignerResponder(promptToPasswordDictionary);
            var result = CommandLine.Run(JavaUtilities.JavaBinaryPath, arguments, ioHandler: responder.AggregateLine);
            if (result.exitCode == 0)
            {
                return true;
            }

            Debug.LogErrorFormat("APK re-signing failed: {0}", result.message);
            return false;
        }

        private static string GetApkSignerJarPath()
        {
            var newestBuildToolsVersion = AndroidBuildTools.GetNewestBuildToolsVersion();
            if (newestBuildToolsVersion == null)
            {
                return null;
            }

            var newestBuildToolsPath = Path.Combine(AndroidBuildTools.GetBuildToolsPath(), newestBuildToolsVersion);
            var apkSignerJarPath = Path.Combine(newestBuildToolsPath, "lib/apksigner.jar");
            if (File.Exists(apkSignerJarPath))
            {
                return apkSignerJarPath;
            }

            Debug.LogErrorFormat("Failed to locate apksigner.jar at path: {0}", apkSignerJarPath);
            return null;
        }

        /// <summary>
        /// Checks apksigner's stdout for password prompts and provides the associated password to apksigner's stdin.
        /// This is more secure than providing passwords on the command line (where passwords are visible to process
        /// listing tools like "ps") or using file-based password input (where passwords are written to disk).
        /// </summary>
        private class ApkSignerResponder : CommandLine.LineReader
        {
            private readonly Dictionary<string, string> _promptToPasswordDictionary;

            public ApkSignerResponder(Dictionary<string, string> promptToPasswordDictionary)
            {
                _promptToPasswordDictionary = promptToPasswordDictionary;
                LineHandler += CheckAndRespond;
            }

            private void CheckAndRespond(Process process, StreamWriter stdin, CommandLine.StreamData data)
            {
                if (process.HasExited)
                {
                    return;
                }

                // The password prompt text won't have a trailing newline, so read ahead on stdout to locate it.
                var stdoutData = GetBufferedData(0);
                var stdoutText = Aggregate(stdoutData).text;
                var password = _promptToPasswordDictionary
                    .Where(kvp => stdoutText.Contains(kvp.Key))
                    .Select(kvp => kvp.Value)
                    .FirstOrDefault();
                if (password == null)
                {
                    return;
                }

                Flush();
                // UTF8 to match "--pass-encoding utf-8" argument passed to apksigner.
                foreach (var value in Encoding.UTF8.GetBytes(password + Environment.NewLine))
                {
                    stdin.BaseStream.WriteByte(value);
                }

                stdin.BaseStream.Flush();
            }
        }
    }
}