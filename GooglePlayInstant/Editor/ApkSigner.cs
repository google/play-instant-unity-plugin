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
using Logger = UnityEngine.Logger;

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// Provides methods that call the Android SDK build tool "apksigner" to verify whether an APK complies with
    /// <see href="https://source.android.com/security/apksigning/v2">APK Signature Scheme V2</see> and to re-sign
    /// the APK if not. Instant apps require Signature Scheme V2 starting with Android O, however Unity versions
    /// prior to 2017.3 do not produce compliant APKs. Starting with Unity 2017.3 only Gradle-built APKs meet
    /// meet this requirement. Without this "adb install --ephemeral" on an Android O device will fail with
    /// "INSTALL_PARSE_FAILED_NO_CERTIFICATES: No APK Signature Scheme v2 signature in ephemeral package".
    /// </summary>
    public static class ApkSigner
    {
        private const string AndroidDebugKeystore = ".android/debug.keystore";

        /// <summary>
        /// Synchronously calls the apksigner tool to verify whether the specified APK uses APK Signature Scheme V2.
        /// </summary>
        /// <returns>true if the specified APK uses APK Signature Scheme V2, false otherwise</returns>
        public static bool Verify(string apkPath)
        {
            var apkSignerFileName = GetApkSignerFileName();
            var arguments = string.Format("verify {0}", apkPath);
            var result = CommandLine.Run(apkSignerFileName, arguments);
            if (result.exitCode == 0)
            {
                return true;
            }

            Debug.LogErrorFormat("\"{0} {1}\" failed with exit code {2}", apkSignerFileName, arguments,
                result.exitCode);
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
                Debug.LogFormat("No keystore specified. Signing using default Android debug.keystore.");
                if (Application.platform == RuntimePlatform.WindowsEditor)
                {
                    // TODO: test on Windows.
                    keystoreName = "TODO";
                }
                else
                {
                    var home = Environment.GetEnvironmentVariable("HOME");
                    keystoreName = string.IsNullOrEmpty(home)
                        ? AndroidDebugKeystore
                        : Path.Combine(home, AndroidDebugKeystore);
                }

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

            var apkSignerFileName = GetApkSignerFileName();
            var arguments = string.Format(
                "sign --ks {0} --ks-key-alias {1} --pass-encoding utf-8 {2}",
                keystoreName, keyaliasName, apkPath);

            var promptToPasswordDictionary = new Dictionary<string, string>
            {
                // Example keystore password prompt: "Keystore password for signer #1: "
                {"Keystore password for signer", keystorePass},
                // Example keyalias password prompt: "Key \"androiddebugkey\" password for signer #1: "
                {"password for signer", keyaliasPass}
            };
            var apkSignerResponder = new ApkSignerResponder(promptToPasswordDictionary);
            var result = CommandLine.Run(apkSignerFileName, arguments, ioHandler: apkSignerResponder.AggregateLine);
            if (result.exitCode == 0)
            {
                return true;
            }

            Debug.LogErrorFormat("\"{0} {1}\" failed with exit code {2}", apkSignerFileName, arguments,
                result.exitCode);
            return false;
        }

        private static string GetApkSignerFileName()
        {
            return Path.Combine(AndroidBuildTools.GetNewestBuildToolsPath(), "apksigner");
        }

        /// <summary>
        /// Checks apksigner's stdout for password prompts and outputs the associated password to apksigner's stdin.
        /// This is more secure than using apksigner's support for command line and file-based password input.
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
                foreach (var value in Encoding.UTF8.GetBytes(password + Environment.NewLine))
                {
                    stdin.BaseStream.WriteByte(value);
                }

                stdin.BaseStream.Flush();
            }
        }
    }
}