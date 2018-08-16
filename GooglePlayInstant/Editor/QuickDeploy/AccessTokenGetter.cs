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
using UnityEngine;

namespace GooglePlayInstant.Editor.QuickDeploy
{
    /// <summary>
    /// Provides methods for using the OAuth2 flow to get user authorization and to retrieve an access token that is
    /// used send HTTP requests to Google Cloud Platform APIs.
    /// </summary>
    public static class AccessTokenGetter
    {
        private const string OAuth2CodeGrantType = "authorization_code";

        // Full control scope is required, since the application needs to read, write and change access
        // permissions of buckets and objects.
        private const string CloudStorageFullControlScope = "https://www.googleapis.com/auth/devstorage.full_control";
        private static KeyValuePair<string, string>? _authorizationResponse;

        private static Action<KeyValuePair<string, string>> _onOAuthResponseReceived;

        /// <summary>
        /// Get Access token to use for API calls if available. Returns null if access token is not available.
        /// </summary>
        public static GcpAccessToken AccessToken { get; private set; }

        /// <summary>
        /// Check whether a new authorization code has been received and execute scheduled tasks accordingly.
        /// </summary>
        public static void Update()
        {
            // Handle scheduled tasks for when authorization code is received.
            if (_authorizationResponse.HasValue && _onOAuthResponseReceived != null)
            {
                _onOAuthResponseReceived(_authorizationResponse.Value);
                _authorizationResponse = null;
                _onOAuthResponseReceived = null;
            }
        }

        /// <summary>
        /// Get new access token if access token is expired or is not available, and execute the action when the access
        /// token is available.
        /// </summary>
        /// <param name="postTokenAction">Action to be executed when valid access token is avalable.</param>
        public static void UpdateAccessToken(Action postTokenAction)
        {
            // TODO: Implement reuse of refresh token to get a new access token when the current token is expired.
            if (AccessToken == null)
            {
                GetAuthCode(code => RequestAndStoreAccessToken(code, postTokenAction));
            }
            else
            {
                postTokenAction();
            }
        }

        /// <summary>
        /// Instantiate the OAuth2 flow to retrieve authorization code for google cloud storage, and schedule
        /// invocation of the code handler on the received authorization code once it is available or throw an exception
        /// once there is a failure to get the authorization code.
        /// </summary>
        /// <param name="onAuthorizationCodeAction">An action to invoke on the authorization code instance when it is
        /// available.</param>
        /// <exception cref="Exception">Exception thrown when required authorization code cannot be received
        /// from OAuth2 flow.</exception>
        public static void GetAuthCode(Action<AuthorizationCode> onAuthorizationCodeAction)
        {
            var server = new OAuth2Server(authorizationResponse => { _authorizationResponse = authorizationResponse; });
            server.Start();

            var redirectUri = server.CallbackEndpoint;

            _onOAuthResponseReceived = authorizationResponse =>
            {
                if (!string.Equals("code", authorizationResponse.Key))
                {
                    throw new Exception("Could not receive required permissions");
                }

                var authCode = new AuthorizationCode
                {
                    Code = authorizationResponse.Value,
                    RedirectUri = redirectUri
                };

                if (onAuthorizationCodeAction != null)
                {
                    onAuthorizationCodeAction(authCode);
                }
            };

            // Take the user to the authorization page to authorize the application.
            var credentials = OAuth2Credentials.GetCredentials();
            var authorizationUrl =
                string.Format("{0}?scope={1}&access_type=offline&redirect_uri={2}&response_type=code&client_id={3}",
                    credentials.auth_uri, CloudStorageFullControlScope, redirectUri, credentials.client_id);

            Application.OpenURL(authorizationUrl);
        }

        /// <summary>
        /// Sends an HTTP request to retrieve an access token from the token uri in developer's OAuth2 credentials file.
        /// Schedules an action to store the access token once the token is received from the server or to throw an
        /// exception once there is a failure to retrieve the token, and to invoke the post token action passed as an
        /// argument to this function once the token has been received and stored.
        /// </summary>
        /// <param name="authCode">Authorization code received from OAuth2 to be used to fetch access token.</param>
        /// <param name="postTokenAction">An action to invoke once the token has been received and stored.</param>
        /// <exception cref="Exception">Exception thrown when there is a failure to retrieve access token.</exception>
        private static void RequestAndStoreAccessToken(AuthorizationCode authCode, Action postTokenAction)
        {
            var credentials = OAuth2Credentials.GetCredentials();
            var formData = new Dictionary<string, string>
            {
                {"code", authCode.Code},
                {"client_id", credentials.client_id},
                {"client_secret", credentials.client_secret},
                {"redirect_uri", authCode.RedirectUri},
                {"grant_type", OAuth2CodeGrantType}
            };
            WwwRequestInProgress.TrackProgress(
                HttpRequestHelper.SendHttpPostRequest(credentials.token_uri, formData, null),
                "Requesting access token",
                completeTokenRequest =>
                {
                    var responseText = completeTokenRequest.text;
                    var token = JsonUtility.FromJson<GcpAccessToken>(responseText);
                    if (string.IsNullOrEmpty(token.access_token))
                    {
                        throw new Exception(string.Format(
                            "Attempted to request access token and received response with error {0} and text {1}",
                            responseText, completeTokenRequest.error));
                    }

                    AccessToken = token;
                    postTokenAction();
                });
        }

        /// <summary>
        /// Represents authorization code received from OAuth2 Protocol when the user authorizes the application to
        /// access the cloud, and is used to get an access token used for making API requests.
        /// </summary>
        public class AuthorizationCode
        {
            public string Code;
            public string RedirectUri;
        }

        /// <summary>
        /// Represents the JSON body of the access token issued by Google Cloud OAuth2 API.
        /// <see cref="https://developers.google.com/identity/protocols/OAuth2InstalledApp"/>
        /// </summary>
        [Serializable]
        public class GcpAccessToken
        {
            // Fields are named snake_case style to match the format of the JSON response returned by GCP OAuth2 API.

            /// <summary>
            /// Token for the application to use when making Cloud API requests.
            /// </summary>
            public string access_token;

            /// <summary>
            /// Token to use when the current access token has expired in order to get a new token.
            /// </summary>
            public string refresh_token;

            /// <summary>
            /// Seconds from the time the token was issued to when it will expire.
            /// </summary>
            public int expires_in;
        }
    }
}