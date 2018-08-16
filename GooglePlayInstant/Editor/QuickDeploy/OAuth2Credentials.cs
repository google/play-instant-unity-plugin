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
using UnityEngine;

namespace GooglePlayInstant.Editor.QuickDeploy
{
    /// <summary>
    /// Holds functionality for reading developer's OAuth2 Client Credentials.
    /// </summary>
    public static class OAuth2Credentials
    {
        /// <summary>
        /// Returns a Credentials instance containing credentials from the path specified in Quick Deploy
        /// configurations.
        /// <see cref="https://console.cloud.google.com/apis/credentials"/>
        /// </summary>
        /// <exception cref="Exception">Exception thrown if the file cannot be parsed as a valid OAuth2 client ID file.
        /// </exception>
        public static Credentials GetCredentials()
        {
            var credentialsFilePath = QuickDeployConfig.Config.cloudCredentialsFileName;
            var credentialsFile = JsonUtility.FromJson<CredentialsFile>(File.ReadAllText(credentialsFilePath));
            if (credentialsFile == null || credentialsFile.installed == null)
            {
                throw new Exception(string.Format(
                    "File at {0} is not a valid OAuth 2.0 credentials file for installed application. Please visit " +
                    "https://console.cloud.google.com/apis/credentials to create a valid OAuth 2.0 credentials file " +
                    "for your project.",
                    credentialsFilePath));
            }

            return credentialsFile.installed;
        }

        /// <summary>
        /// Class representation of the JSON contents of OAuth2 Credentials.
        /// </summary>
        [Serializable]
        public class Credentials
        {
            // Uses unconventional naming for public fields to conform to the format of the credentials JSON contents.
            public string client_id;
            public string client_secret;
            public string auth_uri;
            public string token_uri;
            public string project_id;
        }

        /// <summary>
        /// Class representation of the JSON file containing OAuth2 credentials.
        /// </summary>
        [Serializable]
        public class CredentialsFile
        {
            // Uses unconventional naming for public fields to conform to the format of the credentials file JSON contents.
            public Credentials installed;
        }
    }
}