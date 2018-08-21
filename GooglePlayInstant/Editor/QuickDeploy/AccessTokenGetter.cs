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
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("GooglePlayInstant.Tests.Editor.QuickDeploy")]

namespace GooglePlayInstant.Editor.QuickDeploy
{
    /// <summary>
    /// Provides methods for using the OAuth2 flow to get user authorization and to retrieve an access token that is
    /// used send HTTP requests to Google Cloud Platform APIs.
    /// <see cref="https://developers.google.com/identity/protocols/OAuth2InstalledApp"/>
    /// </summary>
    public static class AccessTokenGetter
    {
        private const string OAuth2CodeGrantType = "authorization_code";
        private const string OAuth2RefreshTokenGrantType = "refresh_token";

        // Full control scope is required, since the application needs to read, write and change access
        // permissions of buckets and objects.
        private const string CloudStorageFullControlScope = "https://www.googleapis.com/auth/devstorage.full_control";
        private static KeyValuePair<string, string>? _authorizationResponse;

        private static Action<KeyValuePair<string, string>> _onOAuthResponseReceived;

        /// <summary>
        /// Get access token to use for API calls if available. Starts with invalid value until validated.
        /// </summary>
        public static GcpAccessToken AccessToken = new GcpAccessToken(null, null, 0);

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
        /// Retrieves and stores a new access token if the current token is invalid, and executes the post token action
        /// when the new valid access token is available.
        /// </summary>
        /// <param name="postTokenAction">Action to be executed when valid access token is avalable.</param>
        public static void ValidateAccessToken(Action postTokenAction)
        {
            // Access token is valid. No need to request a new one.
            if (AccessToken.IsValid())
            {
                postTokenAction();
                return;
            }

            // If there is no refresh token, go though authorization process.
            if (string.IsNullOrEmpty(GcpAccessToken.RefreshToken))
            {
                GetAuthCode(code => RequestFirstAccessToken(code, postTokenAction));
                return;
            }

            // If refresh token  is present, request a new access token using the refresh token.
            RefreshAccessToken(GcpAccessToken.RefreshToken, postTokenAction);
        }


        /// <summary>
        /// Instantiates the OAuth2 flow to retrieve authorization code for google cloud storage, and schedules
        /// invocation of the code handler on the received authorization code once it is available. Throws an exception
        /// once there is a failure to get the authorization code from the oauth2 flow.
        /// </summary>
        /// <param name="onAuthorizationCodeAction">An action to invoke on the authorization code instance when it is
        /// available.</param>
        private static void GetAuthCode(Action<AuthorizationCode> onAuthorizationCodeAction)
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
        /// Sends an HTTP request to OAuth2 token uri to get a new access token from the authorization code.
        /// Stores the values of the new access token and the refresh token, and invokes the post token action when
        /// the tokens have been  successfuly received and stored.
        /// </summary>
        /// <param name="authCode">Authorization code received from OAuth2 to be used to fetch access token.</param>
        /// <param name="postTokenAction">An action to invoke once the token has been received and stored.</param>
        private static void RequestFirstAccessToken(AuthorizationCode authCode, Action postTokenAction)
        {
            var grantDictionary = new Dictionary<string, string>
            {
                {"code", authCode.Code},
                {"redirect_uri", authCode.RedirectUri},
                {"grant_type", OAuth2CodeGrantType}
            };
            RequestToken(grantDictionary, postTokenAction);
        }

        /// <summary>
        /// Sends an HTTP request to OAuth2 token uri to get a new access token from the previously issued refresh token.
        /// Stores the value of the new access token, and invokes the post token action when the new access token has
        /// been successfully received and stored.
        /// </summary>
        private static void RefreshAccessToken(string refreshToken, Action postTokenAction)
        {
            var grantDictionary = new Dictionary<string, string>
            {
                {"refresh_token", refreshToken},
                {"grant_type", OAuth2RefreshTokenGrantType}
            };
            RequestToken(grantDictionary, postTokenAction);
        }

        /// <summary>
        /// Sends an HTTP request to OAuth2 token uri to retrieve, process and store needed tokens.
        /// </summary>
        /// <param name="grantDictionary">A dictionary containing OAuth2 grant type and grant values to be used when
        /// requesting access token. The dictionary will be mutated by adding more credentials values needed to
        /// send the token request.</param>
        /// <param name="postTokenAction"></param>
        private static void RequestToken(Dictionary<string, string> grantDictionary, Action postTokenAction)
        {
            var credentials = OAuth2Credentials.GetCredentials();
            grantDictionary.Add("client_id", credentials.client_id);
            grantDictionary.Add("client_secret", credentials.client_secret);
            WwwRequestInProgress.TrackProgress(
                HttpRequestHelper.SendHttpPostRequest(credentials.token_uri, grantDictionary, null),
                "Requesting access token",
                completeTokenRequest =>
                {
                    HandleTokenResponse(completeTokenRequest);
                    postTokenAction();
                });
        }

        /// <summary>
        /// Handles received response from a token request by parsing the response to update access and refresh token
        /// values if available, as well as throwing an error when there was a failure getting required tokens.
        /// </summary>
        /// <param name="completeTokenRequest">A www instance holding a token  request that has received a response.</param>
        private static void HandleTokenResponse(WWW completeTokenRequest)
        {
            var responseError = completeTokenRequest.error;
            var responseText = completeTokenRequest.text;
            if (!string.IsNullOrEmpty(responseError))
            {
                throw new Exception(string.Format(
                    "Sent request to get access token and received response with error: \"{0}\", and text: \"{1}\"",
                    responseError,
                    responseText));
            }

            var tokenResponse = JsonUtility.FromJson<GcpAccessTokenResponse>(responseText);
            if (string.IsNullOrEmpty(tokenResponse.access_token))
            {
                throw new Exception(string.Format("Couldn't retrieve access token from response text: \"{0}\"",
                    responseText));
            }

            AccessToken = new GcpAccessToken(tokenResponse.access_token, tokenResponse.refresh_token,
                tokenResponse.expires_in);
        }

        /// <summary>
        /// Represents authorization code received from OAuth2 Protocol when the user authorizes the application to
        /// access the cloud, and is used to get an access token used for making API requests.
        /// </summary>
        private class AuthorizationCode
        {
            public string Code;
            public string RedirectUri;
        }

        /// <summary>
        /// Represents the JSON body of the access token response returned by Google Cloud OAuth2 API.
        /// </summary>
#pragma warning disable CS0649
        [Serializable]
        private class GcpAccessTokenResponse
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

        /// <summary>
        /// Holds utility values and methods that enable to hold and retrieve the values of the current access and
        /// refresh tokens, as well as to track whether the current token has expired or not. 
        /// </summary>
        [Serializable]
        public class GcpAccessToken
        {
            /// <summary>
            ///  An offset to compensate for timing differences between the time when the token response was created on
            /// the server and the time when client application received the token response.
            /// </summary>
            private const int TokenExpirationOffsetInSeconds = 5;

            /// <summary>
            ///  A shared refresh token to use for further access token requests without repeating authorization flow.
            /// </summary>
            internal static string RefreshToken { get; private set; }

            /// <summary>
            ///  The value of the current access token if available.
            /// </summary>
            public readonly string Value;

            private readonly DateTime _expiresAt;

            /// <summary>
            /// Create a instance of GcpAccessToken with the given access token, refresh token and seconds until
            /// expiration. The new refresh token, if valid, replaces the current refresh token and is used for
            /// further requests for new access tokens as needed.
            /// </summary>
            internal GcpAccessToken(string accessToken, string refreshToken, int secondsToExpiration)
            {
                Value = accessToken;
                _expiresAt = DateTime.Now.AddSeconds(secondsToExpiration - TokenExpirationOffsetInSeconds);

                if (!string.IsNullOrEmpty(refreshToken))
                {
                    RefreshToken = refreshToken;
                }
            }

            /// <summary>
            ///  Determine whether there is an access token that is valid and ready to be used for a request now.
            /// </summary>
            public bool IsValid()
            {
                return !string.IsNullOrEmpty(Value) && DateTime.Now < _expiresAt;
            }
        }
    }
}